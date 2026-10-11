using UnityEngine;

// CSV 中的遗物配置；场景中的 RelicEntry 通过 id 绑定具体遗物。
[System.Serializable]
public class RelicData
{
    public string id;
    public string desc;
    public string iconString;
    public string condition;
    // 资源加载后的缓存，不写入进度存档。
    [System.NonSerialized]
    public Sprite icon;

}

// CsvToJson 将数字也输出为字符串，因此 CSV 行和运行时配置分开。
[System.Serializable]
public class RelicCsvRow
{
    public string id;
    public string desc;
    public string iconString;
    public string condition;
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
