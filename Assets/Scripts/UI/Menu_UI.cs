using UnityEngine;
using UnityEngine.UI;

public class Menu_UI : MonoBehaviour
{

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
    public Button startGameButton;
    public Button settingsButton;
    public Button quitButton;
    public Button returnButton;
    [SerializeField] private int selectedSlotId;
    public int SelectedSlotId => selectedSlotId;

    private void Start()
    {
        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        DataChoosePanel.SetActive(false); 
        DataEnterPanel.SetActive(false);
    }


    public void OnStartGameClicked()
    {
        MainPanel.SetActive(false);
        DataChoosePanel.SetActive(true);
        //清空存档
        //根据当前状态初始化游戏数据
        //GameState.instance.InitializeGameState();
        //加载卧室场景
        //UnityEngine.SceneManagement.SceneManager.LoadScene("BedRoomScene");
    }

    //设置按钮点击事件
    public void OnSettingsClicked()
    {
        SettingsPanel.SetActive(true);
    }

    //退出按钮点击事件
    public void OnQuitClicked()
    {
        //处理退出按钮点击事件
        Application.Quit();
    }

    //从DataChoosePanel存档页面返回
    public void OnReturnButtonClicked()
    {
        OnCloseClicked();
        DataChoosePanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    //点击槽位
    public void OnDataChooseClicked(int slotId)
    {
        if (slotId < 1 || slotId > 4) return;
        selectedSlotId = slotId;
        DataEnterPanel.SetActive(true);
    }

    //加载存档开始游戏
    public void OnEnterDataClicked()
    {
        if (selectedSlotId < 1 || selectedSlotId > 4) return;
        if (SaveManager.instance.EnterSlot(selectedSlotId)) OnCloseClicked();
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
        DataEnterPanel.SetActive(false);
        selectedSlotId = 0;
    }
}
