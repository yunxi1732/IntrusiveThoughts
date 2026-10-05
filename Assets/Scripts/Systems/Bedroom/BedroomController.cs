using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//管理卧室的交互循环
//挂载在Bedroom场景中
//收集可交互遗物 - 退出卧室

public class BedroomController : MonoBehaviour
{
    //单例模式
    public static BedroomController instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
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
    public Button outButton;          //离开卧室按钮

    
    [Header("遗物信息展示")]
    public Image relicIcon;
    public TextMeshProUGUI relicDescription;

    //卧室状态
    [SerializeField] private RelicEntry relicEntryPrefab;   //房间遗物entry预制体
    private RelicEntry selectedRelic;
    private List<RelicEntry> relicEntries = new List<RelicEntry>();
    void Start()
    {
        RelicInfoPanel.SetActive(false);
        InitBedroom();
        collectButton?.onClick.AddListener(CollectRelic);     //绑定确认收集遗物按钮事件
        closeButton?.onClick.AddListener(CloseRelicInfo);     //绑定关闭遗物说明窗口按钮事件
        outButton?.onClick.AddListener(outButtonClicked);       //绑定离开卧室按钮事件
    }

    void CloseRelicInfo()
    {
        RelicInfoPanel.SetActive(false);
    }

    void CollectRelic()
    {
        RelicInfoPanel.SetActive(false);
        //在背包中加入指定遗物
        Inventory.instance.AddBubbleItem(selectedRelic.relicData);
        //删除entry
        Destroy(selectedRelic.gameObject);
        selectedRelic = null;
    }

    void outButtonClicked()
    {
        //关闭遗物信息展示
        CloseRelicInfo();
        //加载地图场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    public void InitBedroom()
    {
        //清空房间遗物
        foreach (RelicEntry entry in relicEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }
        relicEntries.Clear();

        //清空背包遗物
        Inventory.instance.ClearInventory();
        //根据游戏状态重新生成房间遗物entry
        //GenerateRelicEntry();

        //显示背包按钮
        Inventory.instance.EnableInventory();
        //显示日期
        DateController.instance.ShowDatePanel(true);
    }

    private void GenerateRelicEntry()
    {
        //根据游戏状态生成房间遗物entry
    }

    public void SelectRelicEntry(RelicEntry entry)
    {
        selectedRelic = entry;
        ShowRelicInfo(entry.transform.position);
    }

    public void ShowRelicInfo(Vector3 worldPosition)
    {
        RelicInfoPanel.SetActive(true);
        //MoveToWorldPosition(canvas, RelicInfoPanel.GetComponent<RectTransform>(), worldPosition);
        relicIcon.sprite = selectedRelic.relicData.icon;
        relicDescription.text = selectedRelic.relicData.desc;
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
