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
        if (instance == null || instance == this)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    //卧室状态
    public RelicEntry selectedRelic;
    private List<RelicEntry> relicEntries = new List<RelicEntry>();
    [Header("卧室 NPC 容器（绑定场景中的 NPCModelPanel）")]
    public RectTransform npcPanel;
    void Start()
    {
        StoryPlayer.instance.currentLocation = "出租屋";
        // 先按正确日期绑定遗物和面板，再播放剧情，避免覆盖剧情的 UI 动作。
        InitBedroom();

        if (DateController.instance.currentDate == 0)
        {
            StoryManager.instance.TryStart();
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
        Bedroom_UI.instance.RefreshDailyBackground(DateController.instance.currentDate);
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
        UpdateNPCs();
    }

    // 与其他地点使用同一份日程和预制体，人物出现不直接启动对白。
    public void UpdateNPCs()
    {
        if (npcPanel == null)
        {
            Debug.LogError("卧室 NPC 容器未绑定，请在 BedroomController 中绑定 NPCModelPanel。", this);
            return;
        }

        foreach (Transform child in npcPanel)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        var npcs = NPCManager.instance.GetNPCsAtLocation("出租屋");
        foreach (var npc in npcs)
        {
            var npcObject = Instantiate(NPCManager.instance.npcBodyPrefab, npcPanel);
            npcObject.GetComponent<NPCEntry>().Initialize(npc);
        }
        npcPanel.gameObject.SetActive(npcs.Count > 0);
    }

}
