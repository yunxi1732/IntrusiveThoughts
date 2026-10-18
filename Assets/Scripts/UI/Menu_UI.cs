using UnityEngine;
using UnityEngine.UI;

public class Menu_UI : MonoBehaviour
{
    public void PlaySFX(int index)
    {
        AudioManager.instance.PlaySFX(index);
    }

    //单例模式
    public static Menu_UI instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Panels")]
    public GameObject MainPanel;
    public GameObject DataChoosePanel;
    public GameObject DataEnterPanel;
    public GameObject SettingsPanel;
    [Header("开始新游戏 / 继续游戏选择")]
    public GameObject GameModePanel;
    public Button startGameButton;
    public Button settingsButton;
    public Button quitButton;
    [SerializeField] private int selectedSlotId;
    public int SelectedSlotId => selectedSlotId;

    private void Start()
    {
        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        if (DataChoosePanel != null) DataChoosePanel.SetActive(false);
        if (DataEnterPanel != null) DataEnterPanel.SetActive(false);
        if (GameModePanel != null) GameModePanel.SetActive(false);
    }


    public void OnStartGameClicked()
    {
        if (!SaveManager.instance.HasAnyValidSave()) { OnNewGameClicked(); return; }
        ShowGameMode();
    }

    public void OnNewGameClicked()
    {
        SaveManager.instance.StartNewGame();
    }

    public void OnContinueGameClicked()
    {
        if (SaveSlot_UI.instance == null) { Debug.LogError("请先配置常驻 SaveSlot_UI。", this); return; }
        if (!SaveSlot_UI.instance.OpenLoad(ShowGameMode)) return;
        MainPanel.SetActive(false);
        if (GameModePanel != null) GameModePanel.SetActive(false);
    }

    private void ShowGameMode()
    {
        if (GameModePanel == null) { Debug.LogError("请绑定开始新游戏/继续游戏选择面板 GameModePanel。", this); return; }
        MainPanel.SetActive(false);
        GameModePanel.SetActive(true);
    }

    //设置按钮点击事件
    public void OnSettingsClicked()
    {
        SettingsPanel.SetActive(true);
    }

    //退出按钮点击事件
    public void OnQuitClicked()
    {
        // 编辑器中停止运行；打包后退出游戏进程。
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //从DataChoosePanel存档页面返回
    public void OnReturnButtonClicked()
    {
        // 兼容尚未更换的旧绑定；游戏中的槽位返回只关闭弹窗。
        if (SaveSlot_UI.instance != null && SaveSlot_UI.instance.IsOpen)
        {
            SaveSlot_UI.instance.OnBackClicked();
            return;
        }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainScene") return;
        OnCloseClicked();
        if (DataChoosePanel != null) DataChoosePanel.SetActive(false);
        if (GameModePanel != null) GameModePanel.SetActive(false);
        if (MainPanel != null) MainPanel.SetActive(true);
    }

    //点击槽位
    public void OnDataChooseClicked(int slotId)
    {
        selectedSlotId = slotId;
        if (SaveSlot_UI.instance != null) SaveSlot_UI.instance.OnSlotClicked(slotId);
    }

    //加载存档开始游戏
    public void OnEnterDataClicked()
    {
        if (SaveSlot_UI.instance != null) SaveSlot_UI.instance.OnConfirmClicked();
    }

    //清除存档按钮点击事件
    public void OnClearDataClicked()
    {
        if (selectedSlotId < 1 || selectedSlotId > 4) return;
        if (SaveManager.instance.ClearSave(selectedSlotId)) OnCloseClicked();
    }

    //关闭DataEnterPanel读档确认页面
    public void OnCloseClicked()
    {
        if (DataEnterPanel != null) DataEnterPanel.SetActive(false);
        if (SaveSlot_UI.instance != null && SaveSlot_UI.instance.IsOpen) SaveSlot_UI.instance.OnCancelConfirmClicked();
        selectedSlotId = 0;
    }
}
