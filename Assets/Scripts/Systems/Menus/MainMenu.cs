using UnityEngine;
using UnityEngine.UI;

//主菜单
//管理游戏主菜单界面
public class MainMenu : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static MainMenu instance { get; private set; }

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
    public GameObject SettingsPanel;
    public Button startGameButton;
    public Button settingsButton;
    public Button quitButton;

    void Start()
    {
        //初始化菜单界面
        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        DataChoosePanel.SetActive(false);

        //注册按钮事件
        startGameButton.onClick.AddListener(OnStartGameClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    //处理新游戏按钮点击事件
    private void OnStartGameClicked()
    {
        MainPanel.SetActive(false);
        DataChoosePanel.SetActive(true);
        //清空存档
        //根据当前状态初始化游戏数据
        //GameState.instance.InitializeGameState();
        //加载卧室场景
        //UnityEngine.SceneManagement.SceneManager.LoadScene("BedRoomScene");
    }

    //处理设置按钮点击事件
    private void OnSettingsClicked()
    {
        //显示设置面板
        MainPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }

    //处理退出按钮点击事件
    private void OnQuitClicked()
    {
        //处理退出按钮点击事件
        Application.Quit();
    }

    public void OnReturnButtonClicked()
    {
        DataChoosePanel.SetActive(false);
        MainPanel.SetActive(true);
    }
}
