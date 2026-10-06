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
        if (selectedRelic == null || selectedRelic.record == null || selectedRelic.record.collected) return;
        var record = selectedRelic.record;
        if (!Inventory.instance.AddBubbleItem(selectedRelic.relicData, record.instanceId)) return;
        record.collected = true;
        RelicInfoPanel.SetActive(false);
        selectedRelic.gameObject.SetActive(false);
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
        CloseRelicInfo();
        selectedRelic = null;
        bool createRecords = !GameState.instance.bedroomInitialized;
        bool initializedSuccessfully = true;
        relicEntries.Clear();
        var placements = new HashSet<string>();
        foreach (var entry in Object.FindObjectsByType<RelicEntry>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (entry.gameObject.scene != gameObject.scene) continue;
            if (!entry.InitializeRecord(createRecords))
            {
                if (createRecords) initializedSuccessfully = false;
                continue;
            }
            if (!placements.Add(entry.placementId))
            {
                Debug.LogError($"卧室遗物摆放编号重复：{entry.placementId}", entry);
                entry.gameObject.SetActive(false);
                initializedSuccessfully = false;
                continue;
            }
            relicEntries.Add(entry);
        }
        if (createRecords && initializedSuccessfully)
            GameState.instance.bedroomInitialized = true;
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
        if (entry.record == null || entry.record.collected) return;
        selectedRelic = entry;
        ShowRelicInfo(entry.transform.position);
    }

    public void ShowRelicInfo(Vector3 worldPosition)
    {
        RelicInfoPanel.SetActive(true);
        //MoveToWorldPosition(canvas, RelicInfoPanel.GetComponent<RectTransform>(), worldPosition);
        relicIcon.sprite = selectedRelic.relicData.icon;
        relicDescription.text = selectedRelic.relicData.description;
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
