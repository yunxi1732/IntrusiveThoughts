using UnityEngine.EventSystems;
using UnityEngine.UI;

// 保留原生拖动方向：向上拖动浏览下方条目；禁用滚轮。
public class MemoryScrollView : ScrollRect
{
    public override void OnScroll(PointerEventData eventData)
    {
        // 完全忽略滚轮，不触发 ScrollRect 的滚动或惯性状态。
    }
}
