using UnityEngine;
using UnityEngine.UI;

public class Menu_UI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject MainPanel;
    public GameObject DataChoosePanel;
    public GameObject SettingsPanel;
    public Button startGameButton;
    public Button settingsButton;
    public Button quitButton;
    public Button returnButton;


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

    public void OnReturnButtonClicked()
    {
        DataChoosePanel.SetActive(false);
        MainPanel.SetActive(true);
    }
}
