using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 挂在常驻 Canvas 的常开对象上；面板和按钮由 Inspector 配置，不自动绑定 OnClick。
public class SaveSlot_UI : MonoBehaviour
{
    [Serializable]
    public class SlotView
    {
        [Range(1, 4)] public int slotId = 1;
        public Button button;
        public TMP_Text summary;
    }

    public static SaveSlot_UI instance { get; private set; }
    [SerializeField] private GameObject slotPanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private List<SlotView> slots = new List<SlotView>();
    private enum Mode { Load, Save, CompletedSave }
    private Mode mode;
    private int selectedSlot;
    private List<string> endingIds;
    private Action onSaved;
    private Action onCancelled;
    private bool showingMessage;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(this); return; }
        instance = this;
        if (slotPanel != null) slotPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    public bool OpenLoad(Action cancelled = null) => Open(Mode.Load, null, null, cancelled);
    public bool OpenSave() => Open(Mode.Save, null, null, null);
    public bool OpenCompletedSave(List<string> ids, Action saved) => Open(Mode.CompletedSave, ids, saved, null);

    private bool Open(Mode requested, List<string> ids, Action saved, Action cancelled)
    {
        if (IsOpen || SaveManager.instance == null) return false;
        if (slotPanel == null || confirmPanel == null)
        {
            Debug.LogError("SaveSlot_UI 尚未绑定选槽位面板和确认面板。", this);
            return false;
        }
        if (requested != Mode.Load && !SaveManager.instance.CanSave(out string reason, requested == Mode.CompletedSave))
        {
            ShowTimedMessage(reason);
            return false;
        }
        mode = requested;
        endingIds = ids == null ? null : new List<string>(ids);
        onSaved = saved;
        onCancelled = cancelled;
        selectedSlot = 0;
        IsOpen = true;
        slotPanel.SetActive(true);
        confirmPanel.SetActive(false);
        Message("");
        RefreshSlots();
        return true;
    }

    public void RefreshSlots()
    {
        var usedIds = new HashSet<int>();
        foreach (var view in slots)
        {
            if (view == null) continue;
            if (view.slotId < 1 || view.slotId > 4 || !usedIds.Add(view.slotId))
            {
                if (view.button != null) view.button.interactable = false;
                if (view.summary != null) view.summary.text = "槽位编号配置错误";
                Debug.LogError($"SaveSlot_UI 的槽位编号必须为不重复的 1～4，当前值：{view.slotId}", this);
                continue;
            }
            bool valid = SaveManager.instance.TryReadSlot(view.slotId, out var data, out _);
            bool exists = SaveManager.instance.HasSave(view.slotId);
            if (view.button != null) view.button.interactable = mode != Mode.Load || valid;
            if (view.summary != null)
                view.summary.text = valid
                    ? $"Day {data.currentDate} | {data.locationName}" + (data.endingCompleted ? "已通关" : "")
                    : exists ? "存档损坏或不兼容" : "空槽位";
        }
    }

    public void OnSlotClicked(int slotId)
    {
        if (!IsOpen || showingMessage || slotId < 1 || slotId > 4) return;
        if (mode == Mode.Load && !SaveManager.instance.TryReadSlot(slotId, out _, out _)) return;
        selectedSlot = slotId;
        if (confirmText != null)
            confirmText.text = mode == Mode.Load ? $"是否读取该槽位的存档？"
                : SaveManager.instance.HasSave(slotId) ? $"是否保存到该槽位？原存档将被覆盖。"
                : $"是否保存到该槽位？";
        confirmPanel.SetActive(true);
        Message("");
    }

    public void OnConfirmClicked()
    {
        if (!IsOpen || showingMessage || selectedSlot == 0) return;
        bool success = mode == Mode.Load ? SaveManager.instance.EnterSlot(selectedSlot)
            : SaveManager.instance.SaveToSlot(selectedSlot, mode == Mode.CompletedSave, endingIds);
        if (!success)
        {
            ShowTimedMessage(SaveManager.instance.LastError ?? "保存失败，请重试。", mode == Mode.Load ? onCancelled : null);
            return;
        }
        Action saved = mode == Mode.Load ? null : onSaved;
        RefreshSlots();
        if (mode != Mode.Load)
        {
            ShowTimedMessage("存档成功", saved);
            return;
        }
        Close();
        saved?.Invoke();
    }

    private void ShowTimedMessage(string text, Action afterClose = null)
    {
        IsOpen = true;
        showingMessage = true;
        selectedSlot = 0;
        confirmPanel.SetActive(true);
        if (confirmText != null) confirmText.text = "";
        Message(text);
        StartCoroutine(CloseAfterMessage(afterClose));
    }

    private IEnumerator CloseAfterMessage(Action afterClose)
    {
        // MessageText 位于确认弹窗内，先保留弹窗让玩家看到成功反馈。
        yield return new WaitForSecondsRealtime(1.2f);
        Close();
        afterClose?.Invoke();
    }

    // 确认框的“否/返回”：保留槽位面板。
    public void OnCancelConfirmClicked()
    {
        if (!IsOpen || showingMessage) return;
        confirmPanel.SetActive(false);
        selectedSlot = 0;
        Message("");
    }

    // 槽位面板的“返回”：普通保存回设置，读取回菜单，通关保存回制作名单。
    public void OnBackClicked()
    {
        if (!IsOpen || showingMessage) return;
        Action cancelled = onCancelled;
        Close();
        cancelled?.Invoke();
    }

    private void Close()
    {
        confirmPanel.SetActive(false);
        slotPanel.SetActive(false);
        IsOpen = false;
        showingMessage = false;
        selectedSlot = 0;
        endingIds = null;
        onSaved = null;
        onCancelled = null;
    }

    public void ResetAfterDeletingSaves()
    {
        StopAllCoroutines();
        if (slotPanel != null && confirmPanel != null) Close();
        RefreshSlots();
    }

    private void Message(string message)
    {
        if (messageText != null) messageText.text = message;
        else if (!string.IsNullOrEmpty(message)) Debug.LogWarning(message, this);
    }
}
