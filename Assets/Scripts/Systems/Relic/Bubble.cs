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
        // 图标新增 Mask 父节点，兼容旧的直属 Icon 层级。
        var iconTransform = transform.Find("Mask/Icon");
        if (iconTransform == null) iconTransform = transform.Find("Icon");
        icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (icon == null) Debug.LogError($"气泡 {name} 缺少 Mask/Icon 或 Icon 图片组件。", this);
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
            icon.preserveAspect = true;
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
            string relicName = StoryPlayer.instance.interactiveRelic;
            string target = StoryPlayer.instance.currentCharacter;
            if (!string.IsNullOrEmpty(relicName) && StoryPlayer.instance.IsMatchRelic(relicData.id))
            {
                Debug.Log("遗物使用成功: " + relicData.id + ", 期望: " + relicName);
                //开始涌现剧情
                // 缓存 ID，刷新气泡槽位后本组件可能绑定到另一件物品。
                string instanceId = bubbleData.sourceInstanceId;
                if (StoryManager.instance.TryStart(target, relicData))
                {
                    AudioManager.instance.PlayEmergenceSFX();
                    StoryPlayer.instance.emergenceEffect.PlayParticles();
                    Inventory.instance.RemoveRelic(instanceId);
                }
            } else
            {
                AudioManager.instance.PlaySFX(4);
                //获取使用对象
                StoryManager.instance.TryStart(target, relicData, "relicfail");
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
