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

    void Start()
    {
        SettingsPanel.SetActive(false);
    }

    // 设置界面的存档按钮入口；槽位由进入游戏时的选择决定。
    public void OnSaveButtonClicked()
    {
        if (SaveManager.instance == null)
        {
            Debug.LogError("存档管理器尚未初始化。", this);
            return;
        }
        if (SaveManager.instance.CurrentSlotId == 0)
        {
            Debug.LogWarning("尚未选择游戏槽位，无法保存。", this);
            return;
        }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "BedRoomScene")
        {
            Debug.LogWarning("只能在卧室保存游戏。", this);
            return;
        }
        if (StoryManager.instance == null || StoryManager.instance.IsPlaying)
        {
            Debug.LogWarning("剧情尚未就绪或正在播放，无法保存。", this);
            return;
        }
        SaveManager.instance.SaveGame();
    }

    public void OnReturnButtonClicked()
    {
        SettingsPanel.SetActive(false);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainScene"
            && Menu_UI.instance != null)
            Menu_UI.instance.MainPanel.SetActive(true);
    }
}
