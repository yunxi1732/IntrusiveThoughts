using UnityEngine;
using UnityEngine.UI;

public class Menu_UI : MonoBehaviour
{
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
        OnCloseClicked();
        DataChoosePanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    public void OnDataChooseClicked(int slotId)
    {
        if (slotId < 1 || slotId > 4) return;
        selectedSlotId = slotId;
        DataEnterPanel.SetActive(true);
    }

    public void OnEnterDataClicked()
    {
        if (selectedSlotId < 1 || selectedSlotId > 4) return;
        if (SaveManager.instance.EnterSlot(selectedSlotId)) OnCloseClicked();
    }

    public void OnClearDataClicked()
    {
        if (selectedSlotId < 1 || selectedSlotId > 4) return;
        if (SaveManager.instance.ClearSave(selectedSlotId)) OnCloseClicked();
    }

    public void OnCloseClicked()
    {
        DataEnterPanel.SetActive(false);
        selectedSlotId = 0;
    }
}
