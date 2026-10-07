using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理大地图功能，包括地点选择、角色显示以及结束当天的操作

public class MapController : MonoBehaviour
{
    //单例模式
    public static MapController instance { get; private set; }

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
    
    [Header("交互场景展示")]
    private LocationEntry selectedLocationEntry;
    public List<LocationEntry> locationEntries = new List<LocationEntry>();   //对应场景内所有可交互点

    void Start()
    {
        InitMap();
    }


    public void InitMap()
    {
        //关闭背包功能
        Inventory.instance.DisableInventory();
        foreach (var entry in locationEntries) entry.ShowNPCs();
    }

    public void SelectLocationEntry(LocationEntry entry)
    {
        //处理选中地图位置的逻辑
        selectedLocationEntry = entry;
        //加载对应场景和npc
        TheaterController.instance.InitTheater(entry);
    }

}
