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
    [SerializeField] private RelicEntry relicEntryPrefab;   //房间遗物entry预制体
    public RelicEntry selectedRelic;
    private List<RelicEntry> relicEntries = new List<RelicEntry>();
    void Start()
    {
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

    public void InitBedroom()
    {
        Bedroom_UI.instance.CloseRelicInfo();
        selectedRelic = null;
        bool createRecords = !GameState.instance.bedroomInitialized;
        bool initializedSuccessfully = true;
        relicEntries.Clear();
        var placements = new HashSet<string>();
        foreach (var entry in FindObjectsByType<RelicEntry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (entry.gameObject.scene != gameObject.scene) continue;
            if (!entry.InitializeRecord(createRecords))
            {
                if (createRecords) initializedSuccessfully = false;
                continue;
            }
            if (!placements.Add(entry.placementId))
            {
                Debug.LogError($"卧室遗物摆放编号重复：{entry.placementId}", entry);
                entry.gameObject.SetActive(false);
                initializedSuccessfully = false;
                continue;
            }
            relicEntries.Add(entry);
        }
        if (createRecords && initializedSuccessfully)
            GameState.instance.bedroomInitialized = true;
        //根据游戏状态重新生成房间遗物entry
        //GenerateRelicEntry();

        //显示背包按钮
        Inventory.instance.EnableInventory();
        //显示日期
        DateController.instance.ShowDatePanel(true);
    }

    private void GenerateRelicEntry()
    {
        //根据游戏状态生成房间遗物entry
    }

    public void SelectRelicEntry(RelicEntry entry)
    {
        if (entry.record == null || entry.record.collected) return;
        selectedRelic = entry;
        Bedroom_UI.instance.ShowRelicInfo(entry, entry.transform.position);
    }
}
