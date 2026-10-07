using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Bubble : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RelicData relicData;
    public BubbleData bubbleData;
    private RectTransform rect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private Image icon;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        //获取名称为icon的子物体
        icon = transform.Find("Icon")?.GetComponent<Image>();
        // 记录原始位置
        originalPosition = rect.anchoredPosition;
        originalParent = rect.parent;
    }

    public void init(RelicData data, BubbleData record)
    {
        relicData = data;
        bubbleData = record;
        if (icon != null && relicData != null)
        {
            icon.sprite = relicData.icon;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        droppedSuccessfully = false;
        // // 记录原始位置
        // originalPosition = rect.anchoredPosition;
        // originalParent = rect.parent;

        // 拖拽时让射线穿透自身，才能检测到下方的 DropTarget
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 移动端要用 eventData.delta 除以 canvas.scaleFactor
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // 回原位
        ReturnToOrigin();
        // 如果成功接收，则消耗该物品并触发剧情分支
        Debug.Log("Dropped successfully: " + droppedSuccessfully);
        if (droppedSuccessfully)
        {
            //检查遗物是否匹配
            string relicName = TheaterController.instance.interactiveRelic;
            if (!string.IsNullOrEmpty(relicName) && TheaterController.instance.interactiveRelic == relicData.id)
            {
                Debug.Log("遗物使用成功: " + relicData.id + ", 期望: " + relicName);
                //开始涌现剧情
                // 缓存 ID，刷新气泡槽位后本组件可能绑定到另一件物品。
                string instanceId = bubbleData.sourceInstanceId;
                if (StoryManager.instance.TryStart(null, relicData))
                    Inventory.instance.RemoveRelic(instanceId);
                //gameObject.SetActive(false);
            } else
            {
                Debug.Log("遗物使用失败: " + relicData.id + ", 期望: " + relicName);
            }
        }
    }

    private bool droppedSuccessfully = false;

    public void MarkDropped() => droppedSuccessfully = true;

    public void ReturnToOrigin()
    {
        rect.SetParent(originalParent);
        rect.anchoredPosition = originalPosition;
    }

}
