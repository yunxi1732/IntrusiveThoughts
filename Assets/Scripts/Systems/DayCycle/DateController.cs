using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

//时间管理系统 todo
//挂载在GameRoot场景中，不销毁

public class DateController : MonoBehaviour
{
    //单例模式
    public static DateController instance { get; private set; }

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
    public GameObject datePanel;   //显示当前日期的面板
    public TextMeshProUGUI dateText;   //显示当前日期的文本
    public Image dateImage;   //日期改用 Resources/UI/day1～day7 图片
    public int currentDate;   //0 为教程；教程结束事件将日期推进到第 1 天
    private readonly Sprite[] dailyDateSprites = new Sprite[7];
    private Sprite endingDateSprite;

    private void OnEnable()
    {
        EventManagerNP.StartListening(GameEvents.TutorialCompleted, OnTutorialCompleted);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        OnActiveSceneChanged(default, SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        EventManagerNP.StopListening(GameEvents.TutorialCompleted, OnTutorialCompleted);
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        if (current.name == "EndScene" || current.name == "MainScene")
            ShowDatePanel(current.name == "EndScene");
        else
            UpdateDateText();
    }

    private void OnTutorialCompleted()
    {
        // 先记录教程完成，再进入第一天，地图立即使用第一天的 NPC 日程。
        StoryManager.instance.SetStoryState("story.tutorial.3", 1);
        if (currentDate == 0) SetDate(1);
    }

    void Start()
    {
        UpdateDateText();
        ShowDatePanel(false);
        EventManager1P<int>.TriggerEvent(GameEvents.DayChanged, currentDate);
    }

    public void ShowDatePanel(bool show)
    {
        // 第 0 天只做教程；进入第 1 天后，由返回卧室的流程请求显示。
        UpdateDateText();
        string scene = SceneManager.GetActiveScene().name;
        datePanel?.SetActive(scene != "MainScene"
            && (scene == "EndScene" || (show && currentDate >= 1 && currentDate <= 7)));
    }

    public void PassDay()
    {
        // 第 0 天不能通过睡觉跳过正式游戏的卧室入口。
        if (currentDate == 0) return;
        SetDate(currentDate + 1);
    }

    // 新游戏传入 0，读档传入存档日期；Start 不覆盖已恢复的数据。
    public void SetDate(int date)
    {
        if (date < 0) throw new System.ArgumentOutOfRangeException(nameof(date));
        currentDate = date;
        UpdateDateText();
        // 新游戏回到教程时，清除上一次游戏遗留的面板显示。
        if (currentDate == 0) ShowDatePanel(false);
        EventManager1P<int>.TriggerEvent(GameEvents.DayChanged, currentDate);
    }

    public void UpdateDateText()
    {
        // 保留方法与旧引用，兼容现有调用；正式日期只显示美术图片。
        if (dateText != null) dateText.gameObject.SetActive(false);
        if (dateImage == null && datePanel != null)
            dateImage = datePanel.GetComponent<Image>();
        if (dateImage == null) return;
        if (SceneManager.GetActiveScene().name == "EndScene")
        {
            if (endingDateSprite == null) endingDateSprite = Resources.Load<Sprite>("UI/day_end");
            dateImage.sprite = endingDateSprite;
            dateImage.enabled = endingDateSprite != null;
            if (endingDateSprite == null) Debug.LogWarning("日期图片缺失：Resources/UI/day_end", this);
            return;
        }
        if (currentDate < 1 || currentDate > 7)
        {
            dateImage.sprite = null;
            dateImage.enabled = false;
            datePanel?.SetActive(false);
            return;
        }

        int index = currentDate - 1;
        if (dailyDateSprites[index] == null)
            dailyDateSprites[index] = Resources.Load<Sprite>($"UI/day{currentDate}");
        dateImage.sprite = dailyDateSprites[index];
        dateImage.enabled = dateImage.sprite != null;
        if (dateImage.sprite == null)
            Debug.LogWarning($"日期图片缺失：Resources/UI/day{currentDate}", this);
    }
}
