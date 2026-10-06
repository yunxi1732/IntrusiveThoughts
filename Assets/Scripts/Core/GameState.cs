using UnityEngine;
using System.Collections.Generic;


//保存游戏运行时状态
public class GameState : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static GameState instance { get; private set; }
    public bool bedroomInitialized;
    public PlayerData player = new PlayerData();
    public List<BedroomRelicData> bedroomRelics = new List<BedroomRelicData>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeGameState()
    {
        // 仅供空槽开始新游戏调用；读档和返回卧室不得调用。
        Inventory.instance.InitInventory();
        player = new PlayerData();
        DateController.instance.SetDate(0);
        bedroomInitialized = false;
        bedroomRelics.Clear();
    }

    // 供后续存档系统调用：只恢复卧室、背包与日期部分。
    // 在管理器和资源初始化后、卧室绑定物件前执行。
    public void RestoreBedroomProgress(GameData data)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));
        if (!data.bedroomInitialized)
            throw new System.ArgumentException("已有存档必须包含初始化完成的卧室记录。", nameof(data));
        if (data.currentDate < 0)
            throw new System.ArgumentException("存档日期不能小于 0。", nameof(data));
        var records = data.bedroomRelics ?? new List<BedroomRelicData>();
        var restoredRecords = records.ConvertAll(item => new BedroomRelicData
        {
            instanceId = item.instanceId,
            relicId = item.relicId,
            placementId = item.placementId,
            collected = item.collected,
        });
        Inventory.instance.RestoreBubbleRecords(data.inventoryBubbles ?? new List<BubbleData>());
        bedroomRelics = restoredRecords;
        bedroomInitialized = true;
        DateController.instance.SetDate(data.currentDate);
    }
}
