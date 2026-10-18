using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

//管理常驻Canvas，确保在场景切换时保持不被销毁，并自动绑定当前场景的相机
public class StayCanvas : MonoBehaviour
{
    public static StayCanvas instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);      // 防止重复实例
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public Canvas canvas;

    [Header("鼠标光标（切片内左上角起算的像素热点）")]
    public Vector2 cursorHotspot = new Vector2(0f, 125f);
    [Min(8)] public int cursorWidth = 48;
    private Texture2D normalCursor;
    private Texture2D pressedCursor;
    private Vector2 scaledCursorHotspot;
    private bool cursorPressed;

    private Texture2D CreateCursor(string resource, string spriteName)
    {
        Sprite sprite = System.Array.Find(Resources.LoadAll<Sprite>(resource), item => item.name == spriteName);
        if (sprite == null)
        {
            Debug.LogWarning($"找不到光标切片：{resource}/{spriteName}", this);
            return null;
        }
        Rect rect = sprite.rect;
        int width = Mathf.Clamp(cursorWidth, 8, 256);
        int height = Mathf.Max(1, Mathf.RoundToInt(width * rect.height / rect.width));
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = spriteName + " Cursor";
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, sprite.texture.GetPixelBilinear(
                    (rect.x + (x + 0.5f) * rect.width / width) / sprite.texture.width,
                    (rect.y + (y + 0.5f) * rect.height / height) / sprite.texture.height));
        texture.Apply(false, false);
        // 原始切片为 385 × 480，热点随显示尺寸一起缩放。
        scaledCursorHotspot = new Vector2(
            Mathf.Clamp(cursorHotspot.x / 385f * width, 0f, width - 1f),
            Mathf.Clamp(cursorHotspot.y / 480f * height, 0f, height - 1f));
        return texture;
    }

    private void ApplyCursor(bool pressed)
    {
        cursorPressed = pressed;
        Texture2D texture = pressed && pressedCursor != null ? pressedCursor : normalCursor;
        Cursor.SetCursor(texture, texture != null ? scaledCursorHotspot : Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        bool pressed = Mouse.current.leftButton.isPressed;
        if (cursorPressed != pressed) ApplyCursor(pressed);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && normalCursor != null)
            ApplyCursor(Mouse.current != null && Mouse.current.leftButton.isPressed);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            instance = null;
        }
        if (normalCursor != null) Destroy(normalCursor);
        if (pressedCursor != null) Destroy(pressedCursor);
    }

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        normalCursor = CreateCursor("UI/mouse0", "mouse0_0");
        pressedCursor = CreateCursor("UI/mouse1", "mouse1_0");
        ApplyCursor(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 找到新场景里的相机
        Camera cam = Camera.main;
        if (cam == null)
            cam = Object.FindFirstObjectByType<Camera>();

        canvas.worldCamera = cam;
    }
}
