using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public bool IsTransitioning => _currentRoutine != null;

    private void Awake()
    {
        // 单例（按需保留，可删除）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 独立的常驻 Overlay Canvas，不依赖被卸载场景的相机或父 Canvas。
        transform.SetParent(null, false);
        // Unity 组件必须使用 Unity 的空值比较，不能用 ?? 判断缺失组件。
        var overlay = GetComponent<Canvas>();
        if (overlay == null) overlay = gameObject.AddComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.overrideSorting = true;
        overlay.sortingOrder = short.MaxValue;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(gameObject);

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
        if (IsTransitioning) return;

        _currentRoutine = StartCoroutine(TransitionRoutine(onMidPoint, onComplete));
    }

    public void LoadScene(string sceneName)
    {
        if (IsTransitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"场景未加入构建列表：{sceneName}");
            return;
        }
        _currentRoutine = StartCoroutine(SceneTransitionRoutine(sceneName));
    }

    public void ReturnToMainMenu()
    {
        if (IsTransitioning) return;
        _currentRoutine = StartCoroutine(SceneTransitionRoutine("MainScene", true));
    }

    private IEnumerator SceneTransitionRoutine(string sceneName, bool resetSession = false)
    {
        _mask.raycastTarget = true;
        yield return FadeAlpha(0f, 1f, fadeInDuration);
        if (resetSession)
        {
            // 保留当前遮罩，销毁其他常驻根节点，让主菜单重新创建管理器和 UI。
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (root != gameObject) Destroy(root);
            yield return null;
        }
        var loading = SceneManager.LoadSceneAsync(sceneName);
        yield return loading;
        // 等待新场景 Start 填充结局和 UI 布局，保持全黑后再渐亮。
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return FadeAlpha(1f, 0f, fadeOutDuration);
        _mask.raycastTarget = false;
        _currentRoutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator TransitionRoutine(Action onMidPoint, Action onComplete)
    {
        // 转场期间屏蔽点击，防止误操作
        _mask.raycastTarget = true;

        // 1. 渐黑
        yield return FadeAlpha(0f, 1f, fadeInDuration);

        // 2. 全黑时执行转场逻辑
        onMidPoint?.Invoke();

        // 保持全黑一帧，等待 Destroy 和新生成 UI 的布局更新后再揭开遮罩。
        yield return null;
        Canvas.ForceUpdateCanvases();

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
