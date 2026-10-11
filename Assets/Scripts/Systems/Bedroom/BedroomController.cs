using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理卧室的交互循环
//挂载在Bedroom场景中
//收集可交互遗物 - 退出卧室

public class BedroomController : MonoBehaviour
{
    //单例模式
    public static BedroomController instance { get; private set; }

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

    //卧室状态
    public RelicEntry selectedRelic;
    private List<RelicEntry> relicEntries = new List<RelicEntry>();
    void Start()
    {
        //如果是第0天，播放开场剧情
        if (DateController.instance.currentDate == 0)
        {
            StoryManager.instance.TryStart();
        }

        InitBedroom();
    }

    public void CollectRelic()
    {
        if (selectedRelic == null || selectedRelic.record == null || selectedRelic.record.collected) return;
        var record = selectedRelic.record;
        if (!Inventory.instance.AddBubbleItem(selectedRelic.relicData, record.instanceId)) return;
        record.collected = true;
        Bedroom_UI.instance.CloseRelicInfo();
        selectedRelic.gameObject.SetActive(false);
        selectedRelic = null;
    }

    public void SelectRelicEntry(RelicEntry entry)
    {
        if (entry.record == null || entry.record.collected) return;
        selectedRelic = entry;
        Bedroom_UI.instance.ShowRelicInfo(entry, entry.transform.position);
    }

    public void InitBedroom()
    {
        Bedroom_UI.instance.CloseRelicInfo();
        bool createRecords = !GameState.instance.bedroomInitialized;
        bool initializedSuccessfully = true;
        selectedRelic = null;
        relicEntries.Clear();
        foreach (var entry in FindObjectsByType<RelicEntry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // if (!entry.InitializeRecord(createRecords))
            // {
            //     if (createRecords) initializedSuccessfully = false;
            //     continue;
            // }
            entry.InitializeRecord(createRecords);
            relicEntries.Add(entry);
        }

        Debug.Log("createRecords: " + createRecords + ", initializedSuccessfully: " + initializedSuccessfully);
        if (createRecords && initializedSuccessfully)
        {
            GameState.instance.bedroomInitialized = true;
        }

        //显示背包按钮
        Inventory.instance.EnableInventory();
        //显示日期
        DateController.instance.ShowDatePanel(true);
    }

}
