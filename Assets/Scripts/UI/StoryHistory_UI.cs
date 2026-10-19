using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 使用场景中已制作的 HistoryPanel / Scroll View，只在打开时填充文本。
public class StoryHistory_UI : MonoBehaviour
{
    [SerializeField] private Button historyButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private ScrollRect historyScroll;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject historyLinePrefab;
    [SerializeField] private float rowSpacing = 32f;
    private readonly List<DialogueLine> lines = new List<DialogueLine>();
    private readonly List<HistoryRow> rows = new List<HistoryRow>();
    private CanvasGroup buttonVisibility;
    private bool dialogueWasVisible;
    private bool unlocked;
    private StoryManager story;
    private Coroutine openingPosition;
    public bool IsOpen => historyPanel != null && historyPanel.activeSelf;

    private sealed class HistoryRow
    {
        public RectTransform rect;
        public TextMeshProUGUI name;
        public TextMeshProUGUI content;
        public float height;
        public FontStyles nameStyle;
        public FontStyles contentStyle;
    }

    private void Awake()
    {
        historyPanel.SetActive(false);
        buttonVisibility = historyButton.GetComponent<CanvasGroup>();
        if (buttonVisibility == null) buttonVisibility = historyButton.gameObject.AddComponent<CanvasGroup>();
        if (buttonText == null) buttonText = historyButton.GetComponentInChildren<TMP_Text>(true);
        SetButtonText(false);
        // 对白有独立 Canvas，单改 sibling 顺序无法保证历史按钮处于最上层。
        var parentCanvas = historyButton.GetComponentInParent<Canvas>();
        var buttonCanvas = historyButton.GetComponent<Canvas>();
        if (buttonCanvas == null) buttonCanvas = historyButton.gameObject.AddComponent<Canvas>();
        int order = 0;
        if (parentCanvas != null)
        {
            foreach (var canvas in parentCanvas.rootCanvas.GetComponentsInChildren<Canvas>(true))
                order = Mathf.Max(order, canvas.sortingOrder);
            buttonCanvas.sortingLayerID = parentCanvas.sortingLayerID;
        }
        buttonCanvas.overrideSorting = true;
        buttonCanvas.sortingOrder = order + 1;
        if (historyButton.GetComponent<GraphicRaycaster>() == null)
            historyButton.gameObject.AddComponent<GraphicRaycaster>();
        ApplyButtonVisibility();
    }

    private void OnEnable()
    {
        EventManagerNP.StartListening(GameEvents.Start, EnableHistory);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SaveManager.instance != null && SaveManager.instance.CurrentSlotId > 0) EnableHistory();
    }

    private void Start()
    {
        story = StoryManager.instance;
        story.OnLine += OnLine;
        story.OnDialogueEnd += OnSequenceEnd;
        // 读档进入房间不会再次触发新游戏的 Start 事件。
        unlocked |= !story.IsOpening && GameState.instance != null && GameState.instance.bedroomInitialized;
        unlocked |= SaveManager.instance != null && SaveManager.instance.CurrentSlotId > 0;
        ApplyButtonVisibility();
    }

    private void OnDisable()
    {
        EventManagerNP.StopListening(GameEvents.Start, EnableHistory);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (story != null)
        {
            story.OnLine -= OnLine;
            story.OnDialogueEnd -= OnSequenceEnd;
        }
        Close();
    }

    private void EnableHistory()
    {
        unlocked = true;
        ApplyButtonVisibility();
    }

    private void ApplyButtonVisibility()
    {
        if (buttonVisibility == null) return;
        buttonVisibility.alpha = unlocked ? 1f : 0f;
        buttonVisibility.interactable = unlocked;
        buttonVisibility.blocksRaycasts = unlocked;
    }

    public void Toggle()
    {
        if (IsOpen) { Close(); return; }
        if (!unlocked || story == null || !story.IsPlaying) return;
        if (historyLinePrefab == null)
        {
            Debug.LogError("请绑定 HistoryLine 预制件。", this);
            return;
        }
        story.GetCurrentHistory(lines);
        if (lines.Count == 0) return;
        dialogueWasVisible = dialoguePanel.activeSelf;
        dialoguePanel.SetActive(false);
        historyPanel.SetActive(true);
        SetButtonText(true);
        historyPanel.transform.SetAsLastSibling();
        // 保留原 HistoryBut 作为开关，不新建窗口或关闭按钮。
        historyButton.transform.SetAsLastSibling();
        historyScroll.StopMovement();
        Canvas.ForceUpdateCanvases();
        Populate();
        historyScroll.verticalNormalizedPosition = 0f;
        CenterFirstLine();
        openingPosition = StartCoroutine(CenterAfterLayout());
    }

    public void Close()
    {
        CancelOpeningPosition();
        SetButtonText(false);
        if (!IsOpen) return;
        historyScroll.StopMovement();
        historyPanel.SetActive(false);
        if (dialoguePanel != null && story != null && story.IsPlaying)
            dialoguePanel.SetActive(dialogueWasVisible);
    }

    private void OnLine(DialogueLine _) => HideForDialogueChange();
    private void OnSequenceEnd(DialogueSequence _) => HideForDialogueChange();

    private void HideForDialogueChange()
    {
        CancelOpeningPosition();
        SetButtonText(false);
        // 新句的显示状态由 StoryPlayer 决定，不能用历史窗口的旧状态覆盖。
        if (!IsOpen) return;
        historyScroll.StopMovement();
        historyPanel.SetActive(false);
    }

    private void SetButtonText(bool open)
    {
        if (buttonText != null) buttonText.text = open ? "返回" : "历史";
    }

    private Vector2 ScreenCenterInViewport()
    {
        var viewport = historyScroll.viewport;
        var canvas = viewport.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), camera, out var center)
            ? center : viewport.rect.center;
    }

    private void CenterFirstLine()
    {
        Canvas.ForceUpdateCanvases();
        var viewport = historyScroll.viewport;
        var first = rows[0].rect;
        float rowCenter = viewport.InverseTransformPoint(first.TransformPoint(first.rect.center)).y;
        var position = historyScroll.content.anchoredPosition;
        position.y += ScreenCenterInViewport().y - rowCenter;
        historyScroll.content.anchoredPosition = position;
        historyScroll.StopMovement();
    }

    private IEnumerator CenterAfterLayout()
    {
        // 打开当帧和下一帧布局结束后各定位一次，避免 ScrollRect 重建覆盖首句位置。
        yield return null;
        if (IsOpen && rows.Count > 0) CenterFirstLine();
        openingPosition = null;
    }

    private void CancelOpeningPosition()
    {
        if (openingPosition == null) return;
        StopCoroutine(openingPosition);
        openingPosition = null;
    }

    private void Populate()
    {
        float viewHeight = historyScroll.viewport.rect.height;
        float totalHeight = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            if (i == rows.Count) rows.Add(CreateRow());
            var row = rows[i];
            row.rect.gameObject.SetActive(true);
            row.name.text = lines[i].character;
            row.content.text = lines[i].content;
            bool current = ReferenceEquals(lines[i], story.CurrentLine);
            // 保留预制件样式，只有当前句按历史功能要求额外加粗。
            row.name.fontStyle = current ? row.nameStyle | FontStyles.Bold : row.nameStyle;
            row.content.fontStyle = current ? row.contentStyle | FontStyles.Bold : row.contentStyle;
            // 使用预制件的宽度、48 号字体和行间距，按 TMP 实际换行计数。
            var textRect = row.content.rectTransform;
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(55f, row.content.GetPreferredValues().y));
            row.content.ForceMeshUpdate();
            int lineCount = Mathf.Max(1, row.content.textInfo.lineCount);
            row.height = 55f + (lineCount - 1) * 51f;
            row.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, row.height);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, row.height);
            totalHeight += row.height + (i == 0 ? 0f : rowSpacing);
        }
        for (int i = lines.Count; i < rows.Count; i++) rows[i].rect.gameObject.SetActive(false);

        // 从最新句往上排列较早记录；向下拖动内容即可查看上方的历史。
        float bottomPadding = Mathf.Max(0f, ScreenCenterInViewport().y
            - historyScroll.viewport.rect.yMin - rows[0].height * 0.5f);
        float contentHeight = Mathf.Max(viewHeight, viewHeight * 0.5f + totalHeight + bottomPadding);
        var content = historyScroll.content;
        content.sizeDelta = new Vector2(0f, contentHeight);
        float bottom = bottomPadding;
        for (int i = 0; i < lines.Count; i++)
        {
            var row = rows[i];
            var position = row.rect.anchoredPosition;
            position.y = -(contentHeight - bottom - row.height)
                - (1f - row.rect.pivot.y) * row.height;
            row.rect.anchoredPosition = position;
            bottom += row.height + rowSpacing;
        }
        Canvas.ForceUpdateCanvases();
    }

    private HistoryRow CreateRow()
    {
        var view = Instantiate(historyLinePrefab, historyScroll.content, false);
        var rect = view.GetComponent<RectTransform>();
        var nameText = view.transform.Find("Character").GetComponent<TextMeshProUGUI>();
        var contentText = view.transform.Find("Content").GetComponent<TextMeshProUGUI>();
        return new HistoryRow
        {
            rect = rect, name = nameText, content = contentText,
            height = rect.rect.height,
            nameStyle = nameText.fontStyle, contentStyle = contentText.fontStyle
        };
    }
}
