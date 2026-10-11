using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


//管理卧室的交互循环
//点击遗物可以查看遗物信息
//点击背包可以查看背包物品

//public class RelicEntry : MonoBehaviour, IPointerClickHandler
public class RelicEntry : MonoBehaviour
{
    [Tooltip("对应 CSV-Relic.csv 中的唯一 id。")]
    public string relicId;
    [System.NonSerialized]
    public RelicData relicData;
    public BedroomRelicData record;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(relicId);

    public bool InitializeRecord(bool createIfMissing)
    {
        if (GameState.instance == null || RelicManager.instance == null) return false;
        record = GameState.instance.bedroomRelics.Find(item => item.relicId == relicId);
        if (record == null)
        {
            // 恢复时不能重新随机或生成新 ID。存档未记录的位置保持隐藏。
            if (!createIfMissing)
            {
                gameObject.SetActive(false);
                return false;
            }

            RelicData data = RelicManager.instance.GetRelicDataByName(relicId);
            if (data == null)
            {
                Debug.LogError($"遗物配置不存在：{name} 的 relicId 为 {relicId}", this);
                return false;
            }
            record = new BedroomRelicData
            {
                instanceId = System.Guid.NewGuid().ToString("N"),
                relicId = data.id,
            };
            GameState.instance.bedroomRelics.Add(record);
        }

        Debug.Log($"找到卧室记录对应的遗物：{relicId}", this);
        
        relicData = RelicManager.instance.GetRelicDataByName(relicId);
        if (relicData == null)
            Debug.LogError($"卧室记录引用了不存在的遗物：{record.relicId}", this);
        bool visible = relicData != null
            && !record.collected
            && RelicManager.instance.IsEligible(relicData);
        gameObject.SetActive(visible);
        return visible;
    }
    // public void OnPointerClick(PointerEventData eventData)
    // {
    //     Debug.Log($"遗物被点击：{relicId}", this);
    //     // 当鼠标点击遗物时，显示遗物信息
    //     BedroomController.instance?.SelectRelicEntry(this);
    // }
}
