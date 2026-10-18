using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//NPC管理器，挂载在GameRoot中，不销毁
//负责管理游戏中的所有NPC，包括其信息、日程安排以及在不同地点的出现情况

public class NPCManager : MonoBehaviour
{
    public const string PlayerId = "余予";
    //单例模式
    public static NPCManager instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public List<NPCData> FullNPCList = new List<NPCData>();  //全量NPC信息列表
    public List<NPCSchedule> Schedules = new List<NPCSchedule>();  //NPC出现规则
    public GameObject npcIconPrefab;    //大地图中的npc图标预制体
    public GameObject npcBodyPrefab;    //场景中的npc预制体

    [Header("Player Info")]
    public Sprite playerIcon;

    private string chenxingDefaultIconString;
    private bool chenxingSpecialIcon;

    private void OnEnable()
    {
        if (instance != this) return;
        EventManager1P<bool>.StartListening(GameEvents.ChenxingIconChanged, ApplyChenxingIcon);
        GameStateAccess.Changed += CheckChenxingIcon;
        EventManager1P<int>.StartListening(GameEvents.DayChanged, OnDateChanged);
    }

    private void OnDisable()
    {
        if (instance != this) return;
        EventManager1P<bool>.StopListening(GameEvents.ChenxingIconChanged, ApplyChenxingIcon);
        GameStateAccess.Changed -= CheckChenxingIcon;
        EventManager1P<int>.StopListening(GameEvents.DayChanged, OnDateChanged);
    }

    private void OnDateChanged(int day) => CheckChenxingIcon();

    // 也覆盖读档、新游戏及直接恢复剧情状态的路径。
    private void LateUpdate() => CheckChenxingIcon();

    private void CheckChenxingIcon()
    {
        if (instance != this || DateController.instance == null || StoryManager.instance == null) return;
        bool special = DateController.instance.currentDate >= 5
            && StoryManager.instance.GetStoryState("story.seq_day4_chenxing7") == 1;
        if (special != chenxingSpecialIcon)
            EventManager1P<bool>.TriggerEvent(GameEvents.ChenxingIconChanged, special);
    }

    private void ApplyChenxingIcon(bool special)
    {
        var npc = GetNPCDataByName("辰星");
        if (npc == null || string.IsNullOrEmpty(chenxingDefaultIconString)) return;
        string names = special ? "char_chenxing2_xiaorenteshu" : chenxingDefaultIconString;
        var sprites = LoadSprites(names, npc.id, "iconString");
        var icon = sprites.Find(sprite => sprite != null);
        if (icon == null) return;
        npc.iconString = names;
        npc.icons = sprites;
        npc.icon = icon;
        chenxingSpecialIcon = special;
        foreach (var entry in FindObjectsByType<NPCEntry>(FindObjectsSortMode.None))
            if (entry.npcData == npc) entry.RefreshIcon();
        foreach (var location in FindObjectsByType<LocationEntry>(FindObjectsSortMode.None))
            if (location.npcIconPanel != null) location.ShowNPCs();
    }

    private NPCData RequireNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
            throw new System.ArgumentException("NPC ID 不能为空。", nameof(npcId));
        var npc = GetNPCDataByName(npcId);
        if (npc == null)
            throw new System.InvalidOperationException($"NPC 尚未加载或 ID 不存在：{npcId}");
        return npc;
    }

    public int GetNpcState(string npcId, string attribute)
    {
        return RequireNpc(npcId).GetValue(attribute);
    }

    public void SetNpcState(string npcId, string attribute, int value)
    {
        if (string.IsNullOrWhiteSpace(attribute))
            throw new System.ArgumentException("NPC 属性名不能为空。", nameof(attribute));
        RequireNpc(npcId).SetValue(attribute, value);
    }

    // 情绪累加/扣减，最低为 0。CSV 的 +=/-= 解析仍需另行接入。
    public void AddNpcState(string npcId, string attribute, int amount)
    {
        long result = (long)GetNpcState(npcId, attribute) + amount;
        SetNpcState(npcId, attribute, (int)System.Math.Max(0L, System.Math.Min(int.MaxValue, result)));
    }

    public bool HasTag(string npcId, string tag)
    {
        return RequireNpc(npcId).tags.Contains(tag);
    }

    public void AddTag(string npcId, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new System.ArgumentException("NPC 标签不能为空。", nameof(tag));
        var npc = RequireNpc(npcId);
        if (!npc.tags.Contains(tag)) npc.tags.Add(tag);
    }

    void Initialize()
    {
        // 初始化NPC管理器
        //初始化全量NPC信息：将csv转化为json，并从json文件中读取全量NPC信息
        ParseNPCCSV();
        // 主角与其他人物共用属性和存档；CSV 尚未配置主角时补建记录。
        if (GetNPCDataByName(PlayerId) == null)
            FullNPCList.Add(new NPCData { id = PlayerId });
        ParseNPCScheduleCSV();
    }

    public void ParseNPCCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CSV/CSV-NPC");
        if (csvFile == null)
        {
            Debug.LogError("CSV file not found in Resources folder!");
            return;
        }

        string csv = csvFile.text;
        string jsonData = CsvToJson.Convert(csv);
        //从json文件中读取全量NPC信息
        string wrappedJson = "{\"npcs\":" + jsonData + "}";
        Debug.Log("JSON Data: " + jsonData); // 打印 JSON 数据

        // 解析 JSON 数据
        NPCJsons gameData = JsonUtility.FromJson<NPCJsons>(wrappedJson);
        Debug.Log(wrappedJson);

        foreach(NPCData data in gameData.npcs) {
            if (data.id == "辰星")
            {
                chenxingDefaultIconString = data.iconString;
                chenxingSpecialIcon = false;
            }
            Debug.Log("Adding NPC: " + data.id);
            data.icons = LoadSprites(data.iconString, data.id, "iconString");
            data.models = LoadSprites(data.modelString, data.id, "modelString");
            // 保留现有单图接口，地图和地点默认使用第一张可用头像。
            data.icon = data.icons.Find(sprite => sprite != null);
            data.model = data.models.Find(sprite => sprite != null);
            // CSV 不含运行时字段；显式初始化，避免集合为空引用。
            data.attributes = new List<StateValueData>();
            data.tags = new List<string>();
            var existing = GetNPCDataByName(data.id);
            if (existing != null)
            {
                existing.desc = data.desc;
                existing.iconString = data.iconString;
                existing.modelString = data.modelString;
                existing.icon = data.icon;
                existing.model = data.model;
                existing.icons = data.icons;
                existing.models = data.models;
            }
            else
                FullNPCList.Add(data);
        }
    }

    private static List<Sprite> LoadSprites(string names, string npcId, string field)
    {
        var sprites = new List<Sprite>();
        if (string.IsNullOrWhiteSpace(names)) return sprites;
        foreach (string part in names.Split(';'))
        {
            string resourceName = part.Trim();
            var sprite = resourceName.Length == 0 ? null : Resources.Load<Sprite>("Sprites/" + resourceName);
            sprites.Add(sprite);
            if (sprite == null)
                Debug.LogWarning($"NPC {npcId} 的 {field} 资源无法加载为 Sprite：Sprites/{resourceName}");
        }
        return sprites;
    }

    public void ParseNPCScheduleCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CSV/CSV-NPCSchedule");
        if (csvFile == null)
        {
            Debug.LogError("CSV file not found in Resources folder!");
            return;
        }

        string csv = csvFile.text;
        string jsonData = CsvToJson.Convert(csv);
        string wrappedJson = "{\"schedules\":" + jsonData + "}";
        Debug.Log("JSON Data: " + jsonData); // 打印 JSON 数据

        // 解析 JSON 数据
        NPCScheduleJsons gameData = JsonUtility.FromJson<NPCScheduleJsons>(wrappedJson);
        Debug.Log(wrappedJson);

        Schedules.Clear();
        foreach (NPCScheduleData data in gameData.schedules)
        {
            if (string.IsNullOrEmpty(data.npcid)) continue;
            int.TryParse(data.priority, out int priority);
            Schedules.Add(new NPCSchedule
            {
                npcid = data.npcid,
                location = data.location,
                priority = priority,
                conditions = DialogueParse.ParseConditions(data.conditions, data.npcid),
            });
        }
    }

    //返回NPC当前应出现的地点，没有满足的规则返回 null
    public string GetNPCLocation(string npcId)
    {
        NPCSchedule best = null;
        foreach (var s in Schedules)
        {
            if (s.npcid != npcId) continue;
            if (!s.conditions.TrueForAll(c => c.Check(GameStateAccess.Get(c.key)))) continue;
            if (best == null || s.priority > best.priority) best = s;
        }
        Debug.Log("Best location for NPC " + npcId + ": " + (best?.location ?? "None"));
        string location = best?.location;
        if (location != null && StoryManager.instance != null && StoryManager.instance.IsNpcAbsent(npcId, location))
            return null;
        return location;
    }

    public List<NPCData> GetNPCsAtLocation(string location)
    {
        var result = new List<NPCData>();
        foreach (var npc in FullNPCList)
            if (GetNPCLocation(npc.id) == location) result.Add(npc);
        return result;
    }

    public NPCData GetNPCDataByName(string id)
    {
        foreach (var npc in FullNPCList)
        {
            if (npc.id == id)
            {
                return npc;
            }
        }
        return null;
    }


}
