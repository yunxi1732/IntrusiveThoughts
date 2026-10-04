using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


//管理卧室的交互循环
//点击遗物可以查看遗物信息
//点击背包可以查看背包物品

public class RelicEntry : MonoBehaviour, IPointerClickHandler
{
    public RelicData relicData;

    void Start()
    {
        if (relicData.id == null || relicData.id == "")
        {
            relicData = RelicManager.instance.GetRandomRelicData();
        } else
        {
            relicData = RelicManager.instance.GetRelicDataByName(relicData.id);
        }
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
