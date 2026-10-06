using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

//遗物信息管理
//挂载在GameRoot场景中，不销毁

public class RelicManager : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static RelicManager instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //[Header("UI Panels")]
    public List<RelicData> FullRelicList = new List<RelicData>();   //全部遗物信息
    void Initialize()
    {
        // 初始化遗物管理器
        //初始化全量遗物信息：将csv转化为json，并从json文件中读取全量遗物信息
        TextAsset csvFile = Resources.Load<TextAsset>("CSV/Relic");
        if (csvFile != null)
        {
            string csv = csvFile.text;
            string json = CsvToJson.Convert(csv);
            ReadRelicJson(json);
        }
        else
        {
            Debug.LogError("CSV file not found in Resources folder!");
        }
    }

    public void ReadRelicJson(string jsonData)
    {
        //从json文件中读取全量遗物信息
        string wrappedJson = "{\"relics\":" + jsonData + "}";
        //Debug.Log("JSON Data: " + jsonData); // 打印 JSON 数据

        // 解析 JSON 数据
        RelicCsvRows gameData = JsonUtility.FromJson<RelicCsvRows>(wrappedJson);
        Debug.Log(wrappedJson);

        foreach (RelicCsvRow row in gameData.relics) {
            int priority = 0;
            if (!string.IsNullOrWhiteSpace(row.priority) && !int.TryParse(row.priority, out priority))
            {
                Debug.LogError($"遗物 {row.id} 的 priority 必须是整数：{row.priority}");
                continue;
            }
            var data = new RelicData
            {
                id = row.id,
                desc = row.desc,
                iconString = row.iconString,
                condition = row.condition,
                priority = priority,
            };
            Debug.Log("Adding relic: " + data.id);
            //初始化icon
            data.icon = Resources.Load<Sprite>(data.iconString);
            FullRelicList.Add(data);
        }
    }

    public RelicData GetRandomRelicData()
    {
        if (FullRelicList.Count == 0)
        {
            Debug.LogWarning("FullRelicList is empty!");
            return null;
        }
        int randomIndex = Random.Range(0, FullRelicList.Count);
        return FullRelicList[randomIndex];
    }

    public RelicData GetRelicDataByName(string relicName)
    {
        return FullRelicList.Find(r => r.id == relicName);
    }
}
