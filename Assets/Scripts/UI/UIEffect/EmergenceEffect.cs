using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EmergenceEffect : MonoBehaviour
{
    [Header("震动参数")]
    public float duration = 0.3f;      // 总时长
    public float magnitude = 8f;       // 震动幅度（像素）

    [Header("背景闪烁（可选）")]
    public bool enableBackgroundFlash = false;

    public RectTransform rectTransform;
    public Image backgroundImage;
    private Vector2 originalPos;

    void Awake()
    {
        originalPos = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// 触发涌现特效
    /// </summary>
    public void PlayEmergenceShake()
    {
        StopAllCoroutines();
        if (rectTransform == null) return;
        rectTransform.anchoredPosition = originalPos;
        StartCoroutine(ShakeCoroutine());
        if (enableBackgroundFlash) StartCoroutine(BackGroundCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 震动幅度随时间衰减，越来越小
            float damper = 1f - (elapsed / duration);
            
            float offsetX = Random.Range(-1f, 1f) * magnitude * damper;
            float offsetY = Random.Range(-1f, 1f) * magnitude * damper;

            rectTransform.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;
    }
    private IEnumerator BackGroundCoroutine()
    {
        //backgroundImage闪烁效果
        if (backgroundImage == null) yield break;
        float elapsed = 0f;
        backgroundImage.gameObject.SetActive(true);
        backgroundImage.sprite = null;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 闪烁效果：alpha在0.5和1之间变化
            float alpha = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 2 / duration);
            Color color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
            yield return null;
        }
        backgroundImage.gameObject.SetActive(false);
        // 恢复原始alpha
        Color finalColor = backgroundImage.color;
        finalColor.a = 1f;
        backgroundImage.color = finalColor;
    }
}
