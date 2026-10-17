using UnityEngine;
using UnityEngine.UI;

//设置游戏
//管理游戏设置，音量，语言等
public class Settings_UI : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static Settings_UI instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Panels")]
    public GameObject SettingsPanel;
    [SerializeField] private GameObject quitConfirmPanel;
    [SerializeField] private TMPro.TMP_Text quitConfirmText;

    void Start()
    {
        SettingsPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
    }

    // 保存始终先选槽位，未保存过的新游戏也可使用。
    public void OnSaveButtonClicked()
    {
        if (SaveManager.instance != null) SaveManager.instance.SaveGame();
    }

    public void OnQuitButtonClicked()
    {
        if (ScreenTransition.Instance != null && ScreenTransition.Instance.IsTransitioning) return;
        if (quitConfirmPanel == null) { Debug.LogError("请绑定退出确认面板。", this); return; }
        if (quitConfirmText != null) quitConfirmText.text = "是否确认退出游戏？未保存的游戏进度将会丢失。";
        quitConfirmPanel.SetActive(true);
    }

    public void OnCancelQuitClicked()
    {
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
    }

    public void OnConfirmQuitClicked()
    {
        if (quitConfirmPanel == null || !quitConfirmPanel.activeInHierarchy) return;
        if (SaveManager.instance != null) SaveManager.instance.SavePreferences();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnReturnButtonClicked()
    {
        if (SaveManager.instance != null) SaveManager.instance.SavePreferences();
        SettingsPanel.SetActive(false);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene"
            && Menu_UI.instance != null)
            Menu_UI.instance.MainPanel.SetActive(true);
    }
}
