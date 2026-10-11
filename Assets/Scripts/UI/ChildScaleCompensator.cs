using UnityEngine;

public class ChildScaleCompensator : MonoBehaviour
{
    private RectTransform self;
    private RectTransform parent;

    void Awake()
    {
        self = GetComponent<RectTransform>();
        parent = transform.parent as RectTransform;
    }

    void LateUpdate()
    {
        // 抵消父级的缩放，让自身保持原始大小
        Vector3 parentScale = parent.localScale;
        self.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f
        );
    }
}