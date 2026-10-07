using UnityEngine;
using UnityEngine.UI;
using TMPro;

//设置游戏
//管理游戏设置，音量，语言等
public class Bedroom_UI : MonoBehaviour
{
    //单例模式
    public static Bedroom_UI instance { get; private set; }

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
    public Canvas canvas;  // UI 所在的 Canvas
    public GameObject RelicInfoPanel;   //遗物说明窗口
    public Button collectButton;     //确认收集遗物按钮
    public Button closeButton;       //关闭遗物说明窗口按钮
    public Button outdoorButton;          //离开卧室按钮

    [Header("遗物信息展示")]
    public Image relicIcon;
    public TextMeshProUGUI relicDescription;

    void Start()
    {
        RelicInfoPanel.SetActive(false);
    }

    public void CloseRelicInfo()
    {
        RelicInfoPanel.SetActive(false);
    }

    public void OnCollectRelicClicked()
    {
        BedroomController.instance.CollectRelic();
    }

    public void OnOutdoorButtonClicked()
    {
        //关闭遗物信息展示
        RelicInfoPanel.SetActive(false);
        //加载地图场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    public void ShowRelicInfo(RelicEntry entry, Vector3 worldPosition)
    {
        RelicInfoPanel.SetActive(true);
        //MoveToWorldPosition(canvas, RelicInfoPanel.GetComponent<RectTransform>(), worldPosition);
        relicIcon.sprite = entry.relicData.icon;
        relicDescription.text = entry.relicData.description;
    }

    public void MoveToWorldPosition(Canvas canvas, RectTransform uiElement, Vector3 worldPos)
    {
        // 1. 世界坐标 → 屏幕坐标
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 2. 屏幕坐标 → Canvas 本地坐标
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.worldCamera,   // Camera 模式下必须传 worldCamera
            out localPos
        );

        // 3. 设置 UI 位置
        uiElement.anchoredPosition = localPos;
    }

}
