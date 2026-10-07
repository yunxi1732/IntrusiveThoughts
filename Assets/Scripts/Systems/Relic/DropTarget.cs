using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class DropTarget : MonoBehaviour, IDropHandler
{
    public System.Action<Bubble> OnItemDropped;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("OnDrop called");
        // 拿到被拖拽的物体
        Bubble item = eventData.pointerDrag?.GetComponent<Bubble>();
        if (item == null) return;

        // 标记成功，阻止它回原位
        item.MarkDropped();
        Debug.Log("Item dropped: " + item.name);

        // 触发你的事件
        OnItemDropped?.Invoke(item);
    }
}
