using System;
using System.Collections.Generic;
using UnityEngine;

//挂在按钮上：所有条件满足才显示（或可点击），状态变化时自动刷新；不配条件则始终开放
[RequireComponent(typeof(CanvasGroup))]
public class UIGate : MonoBehaviour
{
    [Tooltip("全部满足才开放，条件写法同对话 conditions")]
    public List<DialogueCondition> conditions = new List<DialogueCondition>();

    private CanvasGroup group;
    private bool subscribedStory;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        GameStateAccess.Changed += Refresh;
        Refresh();
    }

    private void Start()
    {
        //StoryManager 可能晚于本组件的 OnEnable 创建
        if (!subscribedStory && StoryManager.instance != null)
        {
            StoryManager.instance.OnDialogueEnd += OnDialogueEnd;
            subscribedStory = true;
        }
        Refresh();
    }

    private void OnDisable()
    {
        GameStateAccess.Changed -= Refresh;
    }

    private void OnDestroy()
    {
        if (subscribedStory && StoryManager.instance != null)
            StoryManager.instance.OnDialogueEnd -= OnDialogueEnd;
    }

    private void OnDialogueEnd(DialogueSequence _) => Refresh();

    public bool IsOpen()
    {
        foreach (var c in conditions)
        {
            try
            {
                if (!c.Check(GameStateAccess.Get(c.key))) return false;
            }
            catch (Exception e)
            {
                //配置错误时开放，避免玩家被卡死
                Debug.LogError($"UIGate({name}) 条件无效: {e.Message}", this);
                return true;
            }
        }
        return true;
    }

    public void Refresh()
    {
        if (group == null) return;
        bool open = IsOpen();
        group.interactable = open;
        group.blocksRaycasts = open;
        group.alpha = open ? 1f : 0f;
    }

    //遗物数量等不经过 GameStateAccess.Set 的变化，由对应系统调用
    public static void RefreshAll()
    {
        foreach (var gate in FindObjectsByType<UIGate>(FindObjectsSortMode.None)) gate.Refresh();
    }
}
