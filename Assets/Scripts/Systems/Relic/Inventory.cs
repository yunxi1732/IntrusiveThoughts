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
        // 读档先创建背包项、卧室随后绑定美术；重新显示时同步已有列表图标。
        foreach (var entry in inventoryItems)
        {
            var itemIcon = entry.item != null ? entry.item.transform.Find("Icon")?.GetComponent<Image>() : null;
            if (itemIcon == null) continue;
            itemIcon.sprite = entry.relicData.icon;
            itemIcon.preserveAspect = true;
        }
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
        if (inventoryItems.Count >= bubbleEntries.Count)
        {
            Debug.LogWarning("背包遗物栏已满，无法收集更多遗物。");
            return false;
        }
        if (relic != null)
        {
            //创建背包项预制体
            // 新背包在打开时创建 BubbleInfo；仅兼容仍绑定了旧列表的界面。
            GameObject item = inventoryItemPrefab != null && InventoryContent != null
                ? Instantiate(inventoryItemPrefab, InventoryContent.transform) : null;
            //找到名字为Icon的子物体渲染图标
            GameObject iconObject = item != null ? item.transform.Find("Icon")?.gameObject : null;
            Image icon = iconObject?.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = relic.icon;
                icon.preserveAspect = true;
            }
            //注册点击事件
            Button itemButton = item != null ? item.GetComponent<Button>() : null;
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

            //触发收集遗物的相关剧情
            Debug.Log("bedroomInitialized: " + GameState.instance.bedroomInitialized + ", inventoryItems.Count: " + inventoryItems.Count + ", bubbleEntries.Count: " + bubbleEntries.Count);
            if (GameState.instance.bedroomInitialized && inventoryItems.Count > 1)
            {
                StoryManager.instance.SetState("story.tutorial.1", 1);
            }

            //刷新bubble显示
            ShowBubblePanel();
            EventManagerNP.TriggerEvent(GameEvents.InventoryChanged);
        }
        return true;
    }

    public bool RemoveRelic(string instanceId)
    {
        int index = inventoryItems.FindIndex(entry => entry.bubbleData.sourceInstanceId == instanceId);
        if (index < 0) return false;
        Destroy(inventoryItems[index].item);
        inventoryItems.RemoveAt(index);
        EventManagerNP.TriggerEvent(GameEvents.InventoryChanged);
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

    public bool HasRelic(string relicId)
    {
        return inventoryItems.Exists(entry => entry.relicData.id == relicId);
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
        if (records.Count > bubbleEntries.Count)
            throw new System.ArgumentException("背包存档中的遗物数量超过可用栏位。", nameof(records));

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
        EventManagerNP.TriggerEvent(GameEvents.InventoryChanged);

        //刷新bubble显示
        ShowBubblePanel();
    }
}
