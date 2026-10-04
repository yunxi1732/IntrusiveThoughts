using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理卧室的交互循环
//点击遗物可以查看遗物信息
//点击背包可以查看背包物品

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
    public int currentDate;   //当前日期

    void Start()
    {
        currentDate = 0;
        UpdateDateText();
        ShowDatePanel(false);
    }

    public void ShowDatePanel(bool show)
    {
        datePanel?.SetActive(show);
    }

    public void PassDay()
    {
        currentDate++;
        UpdateDateText();
    }

    public void UpdateDateText()
    {
        if (dateText != null)
        {
            dateText.text = (7 - currentDate).ToString();
            if (currentDate >= 7)
            {
                dateText.text = "游戏结束";
            }
        }
    }
}
