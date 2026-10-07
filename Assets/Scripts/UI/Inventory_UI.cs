using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//设置游戏
//管理游戏设置，音量，语言等
public class Inventory_UI : MonoBehaviour
{
    //单例模式
    public static Inventory_UI instance { get; private set; }

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
    public GameObject InventoryGroup;   //背包按钮+背包窗口
    public GameObject BubblePanel;   //气泡panel
    public GameObject InventoryPanel;   //背包窗口
    public GameObject ItemInfoPanel;   //遗物说明窗口
    public Button inventoryButton;  //气泡背包
    public Button closeInventoryButton;  //关闭背包按钮
    public List<RelicBubbleEntry> inventoryItems = new List<RelicBubbleEntry>(); //背包内物品列表
    [Header("Item Info")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public Image itemIcon;

    void Start()
    {
        InventoryGroup.SetActive(false);
        InventoryPanel.SetActive(false);
        inventoryButton.gameObject.SetActive(true);
    }

    //点击背包按钮 - 打开背包
    public void OnOpenClicked()
    {
        InventoryPanel.SetActive(true);
        HideInventoryButton();
    }

    //点击关闭按钮 - 关闭背包
    public void OnCloseClicked()
    {
        InventoryPanel.SetActive(false);
        ShowInventoryButton();
    }

    //启用背包功能，显示按钮并渲染气泡
    public void EnableInventory()
    {
        InventoryGroup.SetActive(true);
        InventoryPanel.SetActive(false);
        // 显示按钮，并渲染气泡
        ShowInventoryButton();
    }

    //禁用背包功能
    public void DisableInventory()
    {
        InventoryGroup.SetActive(false);
    }

    //显示背包按钮并渲染气泡
    public void ShowInventoryButton()
    {
        inventoryButton.gameObject.SetActive(true);
        BubblePanel.SetActive(true);
    }

    //隐藏背包按钮并隐藏气泡
    public void HideInventoryButton()
    {
        inventoryButton.gameObject.SetActive(false);
        HideBubblePanel();
    }

    //隐藏气泡面板
    public void HideBubblePanel()
    {
        BubblePanel.SetActive(false);
    }

    public void RenderBubbleInfo(RelicData relic)
    {
        if (relic != null)
        {
            if (itemNameText != null)
            {
                itemNameText.text = relic.id;
            }
            if (itemDescText != null)
            {
                itemDescText.text = relic.description;
            }
            if (itemIcon != null)
            {
                itemIcon.sprite = relic.icon;
            }
        }
    }

}
