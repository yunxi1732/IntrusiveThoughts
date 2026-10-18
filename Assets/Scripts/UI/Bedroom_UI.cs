using UnityEngine;
using UnityEngine.UI;
using TMPro;

//设置游戏
//管理游戏设置，音量，语言等
public class Bedroom_UI : MonoBehaviour
{
    //单例模式
    public static Bedroom_UI instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Panels")]
    public Canvas canvas;  // UI 所在的 Canvas
    public GameObject BedroomPanel;   //卧室主面板
    public GameObject LivingRoomPanel;
    public GameObject RelicInfoPanel;   //遗物说明窗口
    public GameObject SleepPanel;      //结束当天的确认窗口
    public Button collectButton;     //确认收集遗物按钮
    public Button closeButton;       //关闭遗物说明窗口按钮
    public Button outdoorButton;          //离开卧室按钮

    [Header("遗物信息展示")]
    public Image relicIcon;
    public TextMeshProUGUI relicDescription;
    [SerializeField] private ScrollRect relicDescriptionScroll;

    private Image bedroomBackground;
    private Image livingRoomBackground;
    private readonly Sprite[] dailyBackgrounds = new Sprite[7];
    private readonly Sprite[] dailyLivingRoomBackgrounds = new Sprite[7];

    private void OnEnable()
    {
        if (instance != this) return;
        EventManager1P<int>.StartListening(GameEvents.DayChanged, RefreshDailyBackground);
        RefreshDailyBackground(DateController.instance != null ? DateController.instance.currentDate : 0);
    }

    private void OnDisable()
    {
        if (instance != this) return;
        EventManager1P<int>.StopListening(GameEvents.DayChanged, RefreshDailyBackground);
    }

    public void RefreshDailyBackground(int day)
    {
        if (bedroomBackground == null && BedroomPanel != null)
            bedroomBackground = BedroomPanel.GetComponent<Image>();
        if (livingRoomBackground == null && LivingRoomPanel != null)
            livingRoomBackground = LivingRoomPanel.GetComponent<Image>();

        // 教程沿用第一天；结束后保留最后一天的房间。
        int index = Mathf.Clamp(day, 1, 7) - 1;
        ApplyDailyBackground(bedroomBackground, dailyBackgrounds, "bg_bedroom", index);
        ApplyDailyBackground(livingRoomBackground, dailyLivingRoomBackgrounds, "bg_livingroom", index);
    }

    private void ApplyDailyBackground(Image background, Sprite[] sprites, string prefix, int index)
    {
        if (background == null) return;
        if (sprites[index] == null)
            sprites[index] = Resources.Load<Sprite>($"UI/{prefix}{index + 1}");
        if (sprites[index] == null)
        {
            Debug.LogWarning($"房间背景缺失：Resources/UI/{prefix}{index + 1}", this);
            return;
        }
        background.sprite = sprites[index];
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
    }

    public void OnGoToLivingRoomClicked() => SwitchRoom(false);
    public void OnReturnToBedroomClicked() => SwitchRoom(true);

    private void SwitchRoom(bool bedroom)
    {
        if (StoryManager.instance != null && StoryManager.instance.IsPlaying) return;
        ShowRoom(bedroom);
    }

    public void ShowRoom(bool bedroom)
    {
        CloseRelicInfo();
        if (BedroomController.instance != null) BedroomController.instance.selectedRelic = null;
        BedroomPanel.SetActive(bedroom);
        LivingRoomPanel.SetActive(!bedroom);
    }

    void Start()
    {
        if (SleepPanel != null) SleepPanel.SetActive(false);
        RefreshDailyBackground(DateController.instance != null ? DateController.instance.currentDate : 0);
    }

    public void CloseRelicInfo()
    {
        RelicInfoPanel.SetActive(false);
    }

    public void OnCollectRelicClicked()
    {
        BedroomController.instance.CollectRelic();
    }

    public void OnOutdoorButtonClicked()
    {
        //如果对话在进行中，防止打断对话
        if (StoryManager.instance.IsPlaying)
        {
            //播放交互失败音效
            return;
        }

        //如果教程1没有完成，无法出门
        if (StoryManager.instance.GetState("story.tutorial.1") != 1)
        {
            //触发警告对话
            StoryManager.instance.TryStart(null, null, "tutorial1");
            return;
        }

        //关闭遗物信息展示
        RelicInfoPanel.SetActive(false);
        //加载地图场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    // NextDayButton：先询问，不直接推进日期。
    public void OnEndDayClicked()
    {
        if (!CanSleep()) return;
        if (SleepPanel == null) { Debug.LogError("请绑定 Bedroom_UI 的 SleepPanel。", this); return; }
        SleepPanel.SetActive(true);
    }

    public void OnCancelSleepClicked()
    {
        if (SleepPanel != null) SleepPanel.SetActive(false);
    }

    private bool CanSleep()
    {
        if (ScreenTransition.Instance.IsTransitioning || StoryManager.instance.IsProcessingLeave) return false;

        //如果对话在进行中，防止打断对话
        if (StoryManager.instance.IsPlaying)
        {
            //播放交互失败音效
            return false;
        }
        //如果教程2没有完成，无法睡觉
        if (StoryManager.instance.GetState("story.tutorial.3") != 1)
        {
            //触发警告对话
            StoryManager.instance.TryStart(null, null, "tutorial2");
            return false;
        }
        return true;
    }

    // SleepPanel 确认：复查交互状态后执行原有睡觉/结局转场。
    public void OnConfirmSleepClicked()
    {
        if (SleepPanel == null || !SleepPanel.activeInHierarchy) return;
        SleepPanel.SetActive(false);
        if (!CanSleep()) return;

        if (DateController.instance.currentDate == 7)
        {
            ScreenTransition.Instance.LoadScene("EndScene");
            return;
        }
        // 全黑时推进日期并刷新房间，让场景变化完全被遮罩挡住。
        ScreenTransition.Instance.Play(onMidPoint: () =>
            {
                DateController.instance.PassDay();
                BedroomController.instance.InitBedroom();
            }, onComplete: () => StoryPlayer.instance.RequestAutoCheck());
    }

    public void ShowRelicInfo(RelicEntry entry, Vector3 worldPosition)
    {
        RelicInfoPanel.SetActive(true);
        //MoveToWorldPosition(canvas, RelicInfoPanel.GetComponent<RectTransform>(), worldPosition);
        relicIcon.sprite = entry.relicData.icon;
        relicIcon.preserveAspect = true;
        relicDescription.text = entry.relicData.desc;
        if (relicDescriptionScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(relicDescription.rectTransform);
            relicDescriptionScroll.StopMovement();
            relicDescriptionScroll.verticalNormalizedPosition = 1f;
        }
    }

    public void MoveToWorldPosition(Canvas canvas, RectTransform uiElement, Vector3 worldPos)
    {
        // 1. 世界坐标 → 屏幕坐标
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 2. 屏幕坐标 → Canvas 本地坐标
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.worldCamera,   // Camera 模式下必须传 worldCamera
            out localPos
        );

        // 3. 设置 UI 位置
        uiElement.anchoredPosition = localPos;
    }

}
