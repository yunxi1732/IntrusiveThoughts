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