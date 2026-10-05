using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
        canvas = GetComponent<Canvas>();
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