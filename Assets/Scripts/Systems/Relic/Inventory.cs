using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

//背包系统
//点击背包按钮打开背包
//点背包内物品可以查看信息

public class RelicBubbleEntry
{
    public GameObject item;
    public int index;
    public RelicData relicData;
}

public class Inventory : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static Inventory instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject inventoryItemPrefab;  //背包项预制体
    [Header("UI Panels")]
    public GameObject InventoryGroup;   //背包按钮+背包窗口
    public GameObject BubblePanel;   //气泡panel
    public GameObject InventoryPanel;   //背包窗口
    public GameObject InventoryContent; //背包内容区域
    public GameObject ItemInfoPanel;   //遗物说明窗口
    public Button inventoryButton;  //气泡背包
    public Button closeInventoryButton;  //关闭背包按钮
    public List<RelicBubbleEntry> inventoryItems = new List<RelicBubbleEntry>(); //背包内物品列表
    public List<Bubble> bubbleEntries = new List<Bubble>(); //背包内物品列表
    [Header("Item Info")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public Image itemIcon;



    void Start()
    {
        InventoryGroup.SetActive(false);
        InventoryPanel.SetActive(false);
        inventoryButton.gameObject.SetActive(true);

        inventoryButton?.onClick.AddListener(ShowInventory);
        closeInventoryButton?.onClick.AddListener(CloseInventory);
    }

    public void InitInventory()
    {
        //清空背包内物品
        foreach (var item in inventoryItems)
        {
            if (item != null)
            {
                Destroy(item.item);
            }
        }
        inventoryItems.Clear();

        // 初始化背包内物品
        // foreach (var relic in RelicManager.instance.FullRelicList)
        // {
        //     AddBubbleItem(relic);
        // }

        if (inventoryItems.Count > 0)
        {
            RenderBubbleInfo(inventoryItems[0].relicData);
        }
    }

    public void EnableInventory()
    {
        InventoryGroup.SetActive(true);
        inventoryButton.gameObject.SetActive(true);
        InventoryPanel.SetActive(false);
        BubblePanel.SetActive(false);
    }
    public void DisableInventory()
    {
        InventoryGroup.SetActive(false);
        inventoryButton.gameObject.SetActive(false);
        InventoryPanel.SetActive(false);
        BubblePanel.SetActive(false);
    }

    public void ShowInventoryButton()
    {
        inventoryButton.gameObject.SetActive(true);
        ShowBubblePanel();
    }

    public void HideInventoryButton()
    {
        inventoryButton.gameObject.SetActive(false);
        HideBubblePanel();
    }
    public void ShowBubblePanel()
    {
        BubblePanel.SetActive(true);
        //根据背包内的遗物渲染泡泡
        for (int i = 0; i < bubbleEntries.Count; i++)
        {
            if (i < inventoryItems.Count && inventoryItems[i] != null)
            {
                bubbleEntries[i].gameObject.SetActive(true);
                bubbleEntries[i].init(inventoryItems[i].relicData);
            } else
            {
                bubbleEntries[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideBubblePanel()
    {
        BubblePanel.SetActive(false);
    }

    public void ShowInventory()
    {
        InventoryPanel.SetActive(true);
        HideInventoryButton();
    }

    public void CloseInventory()
    {
        InventoryPanel.SetActive(false);
        ShowInventoryButton();
    }

    public void AddBubbleItem(RelicData relic)
    {
        if (relic != null)
        {
            //创建背包项预制体
            GameObject item = Instantiate(inventoryItemPrefab, InventoryContent.transform);
            //找到名字为Icon的子物体渲染图标
            GameObject iconObject = item.transform.Find("Icon")?.gameObject;
            Image icon = iconObject?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = relic.icon;
            }
            //注册点击事件
            Button itemButton = item.GetComponent<Button>();
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(() => RenderBubbleInfo(relic));
            }

            inventoryItems.Add(new RelicBubbleEntry { item = item, relicData = relic });

            //刷新bubble显示
            ShowBubblePanel();
        }
    }

    public void RemoveRelic(RelicData relicData)
    {
        inventoryItems.RemoveAll(entry => {
            if (entry.relicData.id == relicData.id)
            {
                Destroy(entry.item);
                return true;
            }
            return false;
        });
        //刷新bubble显示
        ShowBubblePanel();
    }

    public void ClearInventory()
    {
        //清除所有背包遗物
        foreach (var entry in inventoryItems)
        {
            Destroy(entry.item);
        }
        inventoryItems.Clear();

        //刷新bubble显示
        ShowBubblePanel();
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
                itemDescText.text = relic.desc;
            }
            if (itemIcon != null)
            {
                itemIcon.sprite = relic.icon;
            }
        }
    }
}
