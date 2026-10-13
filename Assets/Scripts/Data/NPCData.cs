using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class NPCData
{
    public string id;
    public string desc;
    public string iconString;    //多个头像资源名，使用分号分隔
    public string modelString;   //多个立绘资源名，使用分号分隔
    // 运行时情绪和属性，key 如 happy、regret、emotion.anxiety。
    public List<StateValueData> attributes = new List<StateValueData>();
    public List<string> tags = new List<string>();

    public int GetValue(string attribute)
    {
        var entry = attributes.Find(item => item.key == attribute);
        return entry == null ? 0 : entry.value;
    }

    public void SetValue(string attribute, int value)
    {
        var entry = attributes.Find(item => item.key == attribute);
        if (entry == null)
            attributes.Add(new StateValueData { key = attribute, value = value });
        else
            entry.value = value;
    }

    // 加载后的资源缓存，不写入进度存档。
    [System.NonSerialized]
    public Sprite icon;
    [System.NonSerialized]
    public Sprite model;
    // 顺序与 CSV 一致；缺失资源保留 null，避免后面的表情序号错位。
    [System.NonSerialized]
    public List<Sprite> icons = new List<Sprite>();
    [System.NonSerialized]
    public List<Sprite> models = new List<Sprite>();

    public Sprite GetModel(string tag)
    {
        // 台词 tag 为从 1 开始的立绘序号；留空使用默认立绘。
        if (string.IsNullOrWhiteSpace(tag)) return model;
        if (int.TryParse(tag.Trim(), out int number) && number >= 1 && number <= models.Count)
            return models[number - 1] != null ? models[number - 1] : model;
        Debug.LogWarning($"NPC {id} 的立绘序号无效：{tag}，使用默认立绘。");
        return model;
    }
}

[System.Serializable]
public class NPCJsons
{
    public NPCData[] npcs;
}


//用csv-json解析
[System.Serializable]
public class NPCScheduleData
{
    public string npcid;
    public string location;
    public string priority;
    public string conditions;
}

//解析后的出现规则：条件满足时 NPC 出现在 location，多条满足取 priority 最大
public class NPCSchedule
{
    public string npcid;
    public string location;
    public int priority;
    public List<DialogueCondition> conditions;
}

[System.Serializable]
public class NPCScheduleJsons
{
    public NPCScheduleData[] schedules;
}
