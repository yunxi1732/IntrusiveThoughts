using UnityEngine;
using UnityEngine.SceneManagement;
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