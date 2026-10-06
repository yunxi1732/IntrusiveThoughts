using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//NPC管理器，挂载在GameRoot中，不销毁
//负责管理游戏中的所有NPC，包括其信息、日程安排以及在不同地点的出现情况

public class NPCManager : MonoBehaviour
{
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
        ParseNPCScheduleCSV();
    }

    public void ParseNPCCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CSV/NPC");
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
            Debug.Log("Adding NPC: " + data.id);
            //初始化icon
            data.icon = Resources.Load<Sprite>("Sprites/" + data.iconString);
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
            }
            else
                FullNPCList.Add(data);
        }
    }

    public void ParseNPCScheduleCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CSV/NPCSchedule");
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
        return best?.location;
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
