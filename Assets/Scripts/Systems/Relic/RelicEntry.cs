using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


//管理卧室的交互循环
//点击遗物可以查看遗物信息
//点击背包可以查看背包物品

public class RelicEntry : MonoBehaviour, IPointerClickHandler
{
    public RelicData relicData;
    [Tooltip("场景内唯一摆放编号；未填写时使用层级路径。")]
    public string placementId;
    public BedroomRelicData record;

    public bool InitializeRecord(bool createIfMissing)
    {
        if (GameState.instance == null || RelicManager.instance == null) return false;
        if (string.IsNullOrWhiteSpace(placementId))
        {
            placementId = "";
            for (Transform node = transform; node != null; node = node.parent)
                placementId = node.name + "[" + node.GetSiblingIndex() + "]/" + placementId;
        }
        record = GameState.instance.bedroomRelics.Find(item => item.placementId == placementId);
        if (record == null)
        {
            // 恢复时不能重新随机或生成新 ID。存档未记录的位置保持隐藏。
            if (!createIfMissing)
            {
                gameObject.SetActive(false);
                return false;
            }
            var data = relicData == null || string.IsNullOrEmpty(relicData.id)
                ? RelicManager.instance.GetRandomRelicData()
                : RelicManager.instance.GetRelicDataByName(relicData.id);
            if (data == null)
            {
                Debug.LogError($"遗物配置不存在：{name}", this);
                return false;
            }
            record = new BedroomRelicData
            {
                instanceId = System.Guid.NewGuid().ToString("N"),
                relicId = data.id,
                placementId = placementId,
            };
            GameState.instance.bedroomRelics.Add(record);
        }
        relicData = RelicManager.instance.GetRelicDataByName(record.relicId);
        if (relicData == null)
            Debug.LogError($"卧室记录引用了不存在的遗物：{record.relicId}", this);
        gameObject.SetActive(relicData != null && !record.collected);
        return relicData != null;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Relic clicked: " + gameObject.name);
        // 当鼠标点击遗物时，显示遗物信息
        BedroomController bedroomController = BedroomController.instance;
        if (bedroomController != null)
        {
            bedroomController.SelectRelicEntry(this);
        }
    }

    // public void OnMouseDown()
    // {
    //     Debug.Log("Relic mouse down: " + gameObject.name);
    // }
}
