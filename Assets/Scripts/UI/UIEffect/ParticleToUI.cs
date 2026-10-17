using UnityEngine;

public class ParticleFollowUI : MonoBehaviour
{
    public ParticleSystem particle;
    public RectTransform targetUI;

    void Update()
    {
        // RectTransform.position 就是 UI 在世界空间中的位置
        particle.transform.position = targetUI.position;
    }
}