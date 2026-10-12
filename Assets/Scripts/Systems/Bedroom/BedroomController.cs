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
        StoryPlayer.instance.currentLocation = "出租屋";
        DateController.instance.BeginFirstDayOnBedroomEntry();
        // 先按正确日期绑定遗物和面板，再播放剧情，避免覆盖剧情的 UI 动作。
        InitBedroom();

        if (DateController.instance.currentDate == 0)
        {
            StoryManager.instance.TryStart();
        }
        else if (DateController.instance.currentDate == 1)
        {
            // 用原 CSV 的地点、NPC、条件和 once 规则触发正式第一天开场。
            StoryManager.instance.TryStart("麻雀");
        }
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
        // 教程期间隐藏日期；教程结束后返回卧室才显示七天倒计时。
        DateController.instance.ShowDatePanel(DateController.instance.currentDate > 0);
    }

}
