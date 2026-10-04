using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理卧室的交互循环
//点击遗物可以查看遗物信息
//点击背包可以查看背包物品

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

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // 初始化NPC管理器
        //初始化全量NPC信息：将csv转化为json，并从json文件中读取全量NPC信息
        ParseNPCCSV();
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
