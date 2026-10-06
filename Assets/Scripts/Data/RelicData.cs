using UnityEngine;
using UnityEngine.Serialization;

// CSV 中的遗物配置；具体物件身份由 BedroomRelicData 保存。
[System.Serializable]
public class RelicData
{
    public string id;
    [FormerlySerializedAs("description")] public string desc;
    public string iconString;
    public string condition;
    public int priority;
    // 资源加载后的缓存，不写入进度存档。
    [System.NonSerialized]
    public Sprite icon;

    // 保留现有 UI 的访问方式，CSV 字段统一使用 desc。
    public string description { get => desc; set => desc = value; }
}

// CsvToJson 将数字也输出为字符串，因此 CSV 行和运行时配置分开。
[System.Serializable]
public class RelicCsvRow
{
    public string id;
    public string desc;
    public string iconString;
    public string condition;
    public string priority;
}

[System.Serializable]
public class RelicCsvRows
{
    public RelicCsvRow[] relics;
}

[System.Serializable]
public class RelicJsons
{
    public RelicData[] relics;
}
