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
    public int currentDate;   //当前日期

    void Start()
    {
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

    // 新游戏传入 0，读档传入存档日期；Start 不覆盖已恢复的数据。
    public void SetDate(int date)
    {
        if (date < 0) throw new System.ArgumentOutOfRangeException(nameof(date));
        currentDate = date;
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
