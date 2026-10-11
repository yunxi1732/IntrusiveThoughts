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
    public List<LocationEntry> locationEntries = new List<LocationEntry>();   //对应场景内所有可交互点

    void Start()
    {
        InitMap();
    }

    public void InitMap()
    {
        //关闭背包功能
        Inventory.instance.DisableInventory();

        //刷新NPC显示
        foreach (var entry in locationEntries) entry.ShowNPCs();
    }

}
