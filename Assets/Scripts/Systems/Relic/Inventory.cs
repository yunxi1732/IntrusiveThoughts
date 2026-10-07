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
    public GameObject BubblePanel;   //气泡panel
    public GameObject InventoryContent; //背包内容区域
    public List<RelicBubbleEntry> inventoryItems = new List<RelicBubbleEntry>(); //背包内物品列表
    public List<Bubble> bubbleEntries = new List<Bubble>(); //背包内物品列表



    void Start()
    {
    }

    public void InitInventory()
    {
        ClearInventory();

        if (inventoryItems.Count > 0)
        {
            Inventory_UI.instance.RenderBubbleInfo(inventoryItems[0].relicData);
        }
    }

    //启用背包功能 - 显示按钮/关闭背包面包/渲染气泡
    public void EnableInventory()
    {
        Inventory_UI.instance.EnableInventory();
        // 进入卧室时背包可能已从存档恢复，立即显示并绑定已有气泡。
        ShowBubblePanel();
    }
    //禁用背包功能 - 隐藏按钮/关闭背包面板/隐藏气泡面板
    public void DisableInventory()
    {
        Inventory_UI.instance.DisableInventory();
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
                itemButton.onClick.AddListener(() => Inventory_UI.instance.RenderBubbleInfo(relic));
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
}
