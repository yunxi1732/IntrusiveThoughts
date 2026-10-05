using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[System.Serializable]
public class NPCData
{
    public string id;
    public string desc;
    public string iconString;
    public string state;
    public int stateValue;
    public Sprite icon;
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
    public System.Collections.Generic.List<DialogueCondition> conditions;
}

[System.Serializable]
public class NPCScheduleJsons
{
    public NPCScheduleData[] schedules;
}