using UnityEngine;

[System.Serializable]
public class RelicData
{
    public string id;
    public string desc;
    public string iconString;
    public Sprite icon;
}

[System.Serializable]
public class RelicJsons
{
    public RelicData[] relics;
}