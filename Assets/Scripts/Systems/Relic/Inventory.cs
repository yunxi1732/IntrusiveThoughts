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
    public BubbleData bubbleData;
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
        ClearInventory();

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
        // 进入卧室时背包可能已从存档恢复，立即显示并绑定已有气泡。
        ShowBubblePanel();
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
                bubbleEntries[i].init(inventoryItems[i].relicData, inventoryItems[i].bubbleData);
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

    public bool AddBubbleItem(RelicData relic, string instanceId)
    {
        if (relic == null || string.IsNullOrWhiteSpace(instanceId)) return false;
        if (inventoryItems.Exists(entry => entry.bubbleData.sourceInstanceId == instanceId)) return false;
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

            inventoryItems.Add(new RelicBubbleEntry
            {
                item = item,
                relicData = relic,
                bubbleData = new BubbleData { sourceInstanceId = instanceId, relicId = relic.id },
            });

            //刷新bubble显示
            ShowBubblePanel();
        }
        return true;
    }

    public bool RemoveRelic(string instanceId)
    {
        int index = inventoryItems.FindIndex(entry => entry.bubbleData.sourceInstanceId == instanceId);
        if (index < 0) return false;
        Destroy(inventoryItems[index].item);
        inventoryItems.RemoveAt(index);
        //刷新bubble显示
        ShowBubblePanel();
        return true;
    }

    // 存档快照不包含 UI 引用，也不共享可变的数据对象。
    public List<BubbleData> GetBubbleRecords()
    {
        return inventoryItems.ConvertAll(entry => new BubbleData
        {
            sourceInstanceId = entry.bubbleData.sourceInstanceId,
            relicId = entry.bubbleData.relicId,
        });
    }

    public void RestoreBubbleRecords(List<BubbleData> records)
    {
        // 先校验再清空，避免无效存档导致当前背包丢失。
        var ids = new HashSet<string>();
        var relics = new List<RelicData>();
        foreach (var record in records)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.sourceInstanceId)
                || !ids.Add(record.sourceInstanceId))
                throw new System.ArgumentException("背包存档含空记录或重复实例 ID。", nameof(records));
            var relic = RelicManager.instance.GetRelicDataByName(record.relicId);
            if (relic == null)
                throw new System.ArgumentException($"背包存档引用了不存在的遗物：{record.relicId}", nameof(records));
            relics.Add(relic);
        }
        ClearInventory();
        for (int i = 0; i < records.Count; i++)
            AddBubbleItem(relics[i], records[i].sourceInstanceId);
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
                itemDescText.text = relic.description;
            }
            if (itemIcon != null)
            {
                itemIcon.sprite = relic.icon;
            }
        }
    }
}
