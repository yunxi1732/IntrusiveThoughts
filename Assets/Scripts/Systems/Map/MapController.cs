using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理大地图功能，包括地点选择、角色显示以及结束当天的操作

public class MapController : MonoBehaviour
{
    public void PlaySFX(int index)
    {
        AudioManager.instance.PlaySFX(index);
    }

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
    private bool marksDirty;

    private void OnEnable()
    {
        if (instance != this) return;
        GameStateAccess.Changed += RequestMarkRefresh;
        EventManagerNP.StartListening(GameEvents.InventoryChanged, RequestMarkRefresh);
        EventManager1P<int>.StartListening(GameEvents.DayChanged, OnDayChanged);
        EventManager1P<bool>.StartListening(GameEvents.DialoguePlayingChanged, OnDialoguePlayingChanged);
        marksDirty = true;
    }

    private void OnDisable()
    {
        if (instance != this) return;
        GameStateAccess.Changed -= RequestMarkRefresh;
        EventManagerNP.StopListening(GameEvents.InventoryChanged, RequestMarkRefresh);
        EventManager1P<int>.StopListening(GameEvents.DayChanged, OnDayChanged);
        EventManager1P<bool>.StopListening(GameEvents.DialoguePlayingChanged, OnDialoguePlayingChanged);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void RequestMarkRefresh() => marksDirty = true;
    private void OnDayChanged(int day) => RequestMarkRefresh();
    private void OnDialoguePlayingChanged(bool playing)
    {
        if (!playing) RequestMarkRefresh();
    }

    private void LateUpdate()
    {
        if (!marksDirty) return;
        marksDirty = false;
        foreach (var entry in locationEntries)
            if (entry != null) entry.RefreshDialogueMarks();
    }

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
