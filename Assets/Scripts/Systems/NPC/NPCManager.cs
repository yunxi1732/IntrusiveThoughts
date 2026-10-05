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

    void Start()
    {
        Initialize();
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
            if (!s.conditions.TrueForAll(c => c.Check(StoryManager.instance.GetState(c.key)))) continue;
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
