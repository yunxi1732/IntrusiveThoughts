using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理卧室的交互循环
//挂载在Bedroom场景中
//收集可交互遗物 - 退出卧室

public class BedroomController : MonoBehaviour
{
    public void PlaySFX(int index)
    {
        AudioManager.instance.PlaySFX(index);
    }

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

    private void OnEnable() => EventManagerNP.StartListening(GameEvents.Start, OnGameStart);
    private void OnDisable() => EventManagerNP.StopListening(GameEvents.Start, OnGameStart);

    private void OnGameStart()
    {
        // 新游戏状态已设为 day0；初始化完成后才开始场景音乐。
        Bedroom_UI.instance.ShowRoom(true);
        InitBedroom();
        if (AudioManager.instance != null) AudioManager.instance.PlayBGM("bgm_bedroom1");
    }

    private System.Collections.IEnumerator PlayOpening()
    {
        // 等待场景 UI 的 Start 完成，避免覆盖 CG 显示。
        yield return null;
        if (!StoryManager.instance.StartSeqenceByID("seq_tut_1"))
            Debug.LogError("找不到开场剧情 seq_tut_1。", this);
    }

    //卧室状态
    public RelicEntry selectedRelic;
    private List<RelicEntry> relicEntries = new List<RelicEntry>();
    [Header("卧室 NPC 容器（绑定场景中的 NPCModelPanel）")]
    public RectTransform npcPanel;
    void Start()
    {
        StoryPlayer.instance.currentLocation = "出租屋";
        if (StoryManager.instance.IsOpening)
        {
            Bedroom_UI.instance.BedroomPanel.SetActive(false);
            Bedroom_UI.instance.LivingRoomPanel.SetActive(false);
            Inventory.instance.DisableInventory();
            StartCoroutine(PlayOpening());
            return;
        }
        // 教程从卧室开始；正式游戏进家先到客厅。
        bool? restoredView = SaveManager.instance != null ? SaveManager.instance.ConsumeRestoredBedroomView() : null;
        Bedroom_UI.instance.ShowRoom(restoredView ?? DateController.instance.currentDate == 0);
        // 先按正确日期绑定遗物和面板，再播放剧情，避免覆盖剧情的 UI 动作。
        InitBedroom();

        if (DateController.instance.currentDate == 0)
        {
            StoryManager.instance.TryStart();
        }
        StoryPlayer.instance.RequestAutoCheck();
    }

    public void CollectRelic()
    {
        if (selectedRelic == null || selectedRelic.record == null || selectedRelic.record.collected) return;
        var record = selectedRelic.record;
        selectedRelic.SynchronizeIcon();
        if (!Inventory.instance.AddBubbleItem(selectedRelic.relicData, record.instanceId)) return;
        record.collected = true;
        Bedroom_UI.instance.CloseRelicInfo();
        selectedRelic.gameObject.SetActive(false);
        selectedRelic = null;
    }

    public void SelectRelicEntry(RelicEntry entry)
    {
        if (entry.record == null || entry.record.collected) return;
        entry.SynchronizeIcon();
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
        // 包括隐藏房间里的遗物，不扫描场景中遗留的其他遗物对象。
        var homeUI = Bedroom_UI.instance;
        var homeRelics = new List<RelicEntry>();
        homeRelics.AddRange(homeUI.BedroomPanel.GetComponentsInChildren<RelicEntry>(true));
        homeRelics.AddRange(homeUI.LivingRoomPanel.GetComponentsInChildren<RelicEntry>(true));
        foreach (var entry in homeRelics)
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

    // 对话结束更新条件后，只刷新遗物，不重置房间、背包或 NPC。
    public void UpdateRelics()
    {
        var homeUI = Bedroom_UI.instance;
        var entries = new List<RelicEntry>();
        entries.AddRange(homeUI.BedroomPanel.GetComponentsInChildren<RelicEntry>(true));
        entries.AddRange(homeUI.LivingRoomPanel.GetComponentsInChildren<RelicEntry>(true));
        relicEntries.Clear();
        foreach (var entry in entries)
        {
            // 复用既有收集记录，已收集遗物不会重新出现，也不补建存档记录。
            entry.InitializeRecord(false);
            relicEntries.Add(entry);
        }
        if (selectedRelic != null && !selectedRelic.gameObject.activeInHierarchy)
        {
            selectedRelic = null;
            homeUI.CloseRelicInfo();
        }
    }

    // 与其他地点使用同一份日程和预制体，人物出现不直接启动对白。
    public void UpdateNPCs()
    {
        if (StoryManager.instance.IsProcessingLeave) return;
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
