using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// EndScene 的显示控制；NextButton 的 OnClick 由 Inspector 绑定。
public class EndingController : MonoBehaviour
{
    [SerializeField] private Image npcImage;
    [SerializeField] private TMP_Text endingText;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private ScrollRect scrollRect;
    private List<EndingData> endings;
    private int currentPriority = -1;
    private readonly List<string> achievedEndingIds = new List<string>();
    private bool configurationFailed;

    private void Start()
    {
        try
        {
            var csv = Resources.Load<TextAsset>("CSV/CSV-End");
            if (csv == null) throw new InvalidOperationException("找不到 CSV/CSV-End。");
            endings = EndingRules.Load(csv.text);
            if (Inventory.instance != null) Inventory.instance.DisableInventory();
            ShowNext();
        }
        catch (Exception error)
        {
            Debug.LogError($"结局初始化失败：{error}", this);
            title.text = "结局配置错误";
            endingText.text = error.Message;
            nextButtonText.text = "退出";
            currentPriority = 4;
            configurationFailed = true;
        }
    }

    public void OnNextButtonClicked()
    {
        if (ScreenTransition.Instance != null && ScreenTransition.Instance.IsTransitioning) return;
        if (currentPriority >= 4)
        {
            if (!configurationFailed && (SaveManager.instance == null || !SaveManager.instance.CompleteEnding(achievedEndingIds)))
            {
                nextButtonText.text = "保存失败，重试";
                return;
            }
            ScreenTransition.Instance.ReturnToMainMenu();
            return;
        }
        try { ShowNext(); }
        catch (Exception error) { Debug.LogError($"结局判定失败：{error}", this); }
    }

    private void ShowNext()
    {
        for (int priority = currentPriority + 1; priority <= 4; priority++)
        {
            var ending = EndingRules.Select(endings, priority, GameStateAccess.Get);
            if (ending == null) continue;
            currentPriority = priority;
            title.text = ending.结局标题;
            endingText.text = (ending.txt ?? "").Replace("\\n", "\n");
            SetSprite(npcImage, ending.image, "Sprites");
            // 制作名单不是可达成的剧情结局。
            if (priority < 4 && !achievedEndingIds.Contains(ending.结局ID)) achievedEndingIds.Add(ending.结局ID);
            nextButtonText.text = priority == 4 ? "退出" : "下一个";
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
            return;
        }
        throw new InvalidOperationException("没有可显示的结局，请检查 priority 4 的条件。");
    }

    private static void SetSprite(Image image, string resource, string defaultFolder)
    {
        image.sprite = null;
        image.enabled = false;
        if (string.IsNullOrWhiteSpace(resource)) return;
        string path = resource.Trim().Replace('\\', '/');
        if (path.StartsWith("Assets/Resources/", StringComparison.Ordinal)) path = path.Substring(17);
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 4);
        var sprite = Resources.Load<Sprite>(path.Contains("/") ? path : defaultFolder + "/" + path);
        if (sprite == null) throw new InvalidOperationException($"结局图片不存在：{resource}");
        image.sprite = sprite;
        image.enabled = true;
    }
}
