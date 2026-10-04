using UnityEngine;
using UnityEngine.UI;

//设置游戏
//管理游戏设置，音量，语言等
public class Settings : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static Settings instance { get; private set; }

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
    public Button returnButton;

    void Start()
    {
        SettingsPanel.SetActive(false);
        returnButton.onClick.AddListener(OnReturnButtonClicked);
    }

    void OnReturnButtonClicked()
    {
        SettingsPanel.SetActive(false);
        MainMenu.instance.MainPanel.SetActive(true);
    }
}
