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
    public GameObject NPCPanel;          //NPC面板
    public GameObject SleepPanel;      //结束当天的确认窗口
    public TextMeshProUGUI sleepText;
    public Button collectButton;     //确认收集遗物按钮
    public Button closeButton;       //关闭遗物说明窗口按钮
    public Button outdoorButton;          //离开卧室按钮
    [SerializeField] private GameObject nextDayButton;
    public Image DisplayNpcButton;          //显示NPC面板按钮

    [Header("遗物信息展示")]
    public Image relicIcon;
    public TextMeshProUGUI relicName;
    public TextMeshProUGUI relicDescription;
    [SerializeField] private ScrollRect relicDescriptionScroll;

    private bool isNPCDisplayed = true;
    private CanvasGroup npcVisibility;
    private Image bedroomBackground;
    private Image livingRoomBackground;
    private readonly Sprite[] dailyBackgrounds = new Sprite[7];
    private readonly Sprite[] dailyLivingRoomBackgrounds = new Sprite[7];
    public Sprite NPCOn;
    public Sprite NPCOff;

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
        SetCollectionUIVisible(true);
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
        InitNPCPanel();
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

    public void ShowSetBut()
    {
        SetSettingsVisible(isNPCDisplayed);
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
        if (SleepPanel == null || sleepText == null) { Debug.LogError("请绑定 Bedroom_UI 的 SleepPanel 或 sleepText。", this); return; }

        //检测特殊条件，决定使用何种睡觉提示文本
        if (DateController.instance.currentDate == 3 && StoryManager.instance.GetState("npc.麻雀.linestart") == 0)
        {
            //触发警告对话
            sleepText.text = "麻雀好像还在楼道做什么……\n我有种直觉，如果就这么睡去，<color=red>有些话就再也没机会说了</color>。";
        } else
        {
            sleepText.text = "确定睡觉吗？";
        }
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
        //如果麻雀教程没有完成，无法睡觉
        if (StoryManager.instance.GetState("story.seq_day1_start") != 1)
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
        if (relicName != null) relicName.text = entry.relicData.id;
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

    void InitNPCPanel()
    {
        isNPCDisplayed = true;
        ApplyNPCVisibility();
    }

    private void LateUpdate()
    {
        // 只检查子物体，按钮显隐不参与剧情的 NPC 刷新。
        bool hasNPC = false;
        if (NPCPanel != null)
        {
            foreach (Transform child in NPCPanel.transform)
            {
                // 刷新时旧 NPC 已停用，但 Destroy 要到帧末才移除它们。
                if (child.gameObject.activeSelf && child.GetComponent<NPCEntry>() != null)
                {
                    hasNPC = true;
                    break;
                }
            }
        }
        if (DisplayNpcButton != null)
            DisplayNpcButton.gameObject.SetActive(hasNPC);
    }

    public void SwitchNPCPanel()
    {
        if (NPCPanel == null) return;
        isNPCDisplayed = !isNPCDisplayed;
        ApplyNPCVisibility();
    }

    private void ApplyNPCVisibility()
    {
        if (NPCPanel != null)
        {
            if (npcVisibility == null)
            {
                npcVisibility = NPCPanel.GetComponent<CanvasGroup>();
                if (npcVisibility == null) npcVisibility = NPCPanel.AddComponent<CanvasGroup>();
            }
            npcVisibility.alpha = isNPCDisplayed ? 1f : 0f;
            npcVisibility.interactable = isNPCDisplayed;
            npcVisibility.blocksRaycasts = isNPCDisplayed;
        }
        SetCollectionUIVisible(isNPCDisplayed);
        UpdateDisplayNpcButton();
    }

    private void SetCollectionUIVisible(bool visible)
    {
        SetUIVisible(nextDayButton, visible);
        if (outdoorButton != null) SetUIVisible(outdoorButton.gameObject, visible);
        if (DateController.instance != null) SetUIVisible(DateController.instance.datePanel, visible);
        SetSettingsVisible(visible);
    }

    private void SetSettingsVisible(bool visible)
    {
        if (StayCanvas.instance == null) return;
        var settings = StayCanvas.instance.GetComponentInChildren<SettingButton_UI>(true);
        if (settings == null) return;
        bool showSettings = visible && (RelicInfoPanel == null || !RelicInfoPanel.activeSelf);
        SetUIVisible(settings.gameObject, showSettings);
    }

    private static void SetUIVisible(GameObject target, bool visible)
    {
        if (target == null) return;
        var group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    void UpdateDisplayNpcButton()
    {
        if (DisplayNpcButton == null) return;

        if (isNPCDisplayed)
        {
            DisplayNpcButton.sprite = NPCOn;
        }
        else
        {
            DisplayNpcButton.sprite = NPCOff;
        }
    }

}
