using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    public int currentDate;   //0 为教程；教程结束事件将日期推进到第 1 天

    private void OnEnable()
    {
        EventManagerNP.StartListening(GameEvents.TutorialCompleted, OnTutorialCompleted);
    }

    private void OnDisable()
    {
        EventManagerNP.StopListening(GameEvents.TutorialCompleted, OnTutorialCompleted);
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
    }

    public void ShowDatePanel(bool show)
    {
        // 第 0 天只做教程；进入第 1 天后，由返回卧室的流程请求显示。
        datePanel?.SetActive(show && currentDate > 0);
    }

    public void PassDay()
    {
        // 第 0 天不能通过睡觉跳过正式游戏的卧室入口。
        if (currentDate == 0) return;
        currentDate++;
        UpdateDateText();
    }

    // 新游戏传入 0，读档传入存档日期；Start 不覆盖已恢复的数据。
    public void SetDate(int date)
    {
        if (date < 0) throw new System.ArgumentOutOfRangeException(nameof(date));
        currentDate = date;
        UpdateDateText();
        // 新游戏回到教程时，清除上一次游戏遗留的面板显示。
        if (currentDate == 0) ShowDatePanel(false);
    }

    public void UpdateDateText()
    {
        if (dateText != null)
        {
            // 正式第 1～7 天分别剩余 7～1 天，第 8 天结束。
            dateText.text = currentDate == 0 ? string.Empty : (8 - currentDate).ToString();
            if (currentDate > 7)
            {
                dateText.text = "游戏结束";
            }
        }
    }
}
