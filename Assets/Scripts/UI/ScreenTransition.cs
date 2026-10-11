using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 转场遮罩控制器
/// 流程：透明 -> 渐黑 -> (全黑时执行转场逻辑) -> 渐渐透明
/// </summary>
[RequireComponent(typeof(Image))]
public class ScreenTransition : MonoBehaviour
{
    public static ScreenTransition Instance { get; private set; }

    [Header("渐变时长（秒）")]
    [SerializeField] private float fadeInDuration = 0.5f;   // 渐黑耗时
    [SerializeField] private float fadeOutDuration = 0.5f;  // 渐透明耗时

    [Header("遮罩颜色")]
    [SerializeField] private Color fadeColor = Color.black;

    private Image _mask;
    private Coroutine _currentRoutine;

    private void Awake()
    {
        // 单例（按需保留，可删除）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _mask = GetComponent<Image>();
        _mask.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        _mask.raycastTarget = false; // 默认不挡点击，转场时才打开
    }

    /// <summary>
    /// 执行转场
    /// </summary>
    /// <param name="onMidPoint">全黑时执行的回调（转场逻辑）</param>
    /// <param name="onComplete">完全恢复透明后的回调（可空）</param>
    public void Play(Action onMidPoint, Action onComplete = null)
    {
        if (_currentRoutine != null)
            StopCoroutine(_currentRoutine);

        _currentRoutine = StartCoroutine(TransitionRoutine(onMidPoint, onComplete));
    }

    private IEnumerator TransitionRoutine(Action onMidPoint, Action onComplete)
    {
        // 转场期间屏蔽点击，防止误操作
        _mask.raycastTarget = true;

        // 1. 渐黑
        yield return FadeAlpha(0f, 1f, fadeInDuration);

        // 2. 全黑时执行转场逻辑
        onMidPoint?.Invoke();

        // 如需等待异步逻辑，可在这里 yield return new WaitForSeconds(...)
        // 或者把 onMidPoint 改成 Func<IEnumerator> 支持协程回调

        // 3. 渐渐透明
        yield return FadeAlpha(1f, 0f, fadeOutDuration);

        _mask.raycastTarget = false;
        _currentRoutine = null;

        onComplete?.Invoke();
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // 用 unscaled，避免被 Time.timeScale 影响
            float t = Mathf.Clamp01(time / duration);
            // 可选：缓动
            // t = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        var c = _mask.color;
        c = fadeColor;
        c.a = a;
        _mask.color = c;
    }
}