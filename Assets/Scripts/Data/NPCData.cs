using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class NPCData
{
    public string id;
    public string desc;
    public string iconString;
    public string modelString;
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
