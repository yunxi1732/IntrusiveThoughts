using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//设置游戏
//管理游戏设置，音量，语言等
public class Inventory_UI : MonoBehaviour
{
    //单例模式
    public static Inventory_UI instance { get; private set; }

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
    public GameObject InventoryGroup;   //背包按钮+背包窗口
    public GameObject BubblePanel;   //气泡panel
    public GameObject InventoryPanel;   //背包窗口
    public GameObject ItemInfoPanel;   //遗物说明窗口
    public Button inventoryButton;  //气泡背包
    public Button closeInventoryButton;  //关闭背包按钮
    public List<RelicBubbleEntry> inventoryItems = new List<RelicBubbleEntry>(); //背包内物品列表
    [Header("Item Info")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public Image itemIcon;

    [Header("气泡详情列表")]
    public GameObject bubbleInfoPrefab;
    public RectTransform bubbleInfoPanel;
    public GameObject detailedPanel;
    public Image relicBG;
    public TextMeshProUGUI relicDesc;
    public ScrollRect relicDescScroll;

    private void OnEnable()
    {
        if (instance != this) return;
        EventManagerNP.StartListening(GameEvents.InventoryChanged, RefreshOpenInventory);
    }

    private void OnDisable()
    {
        if (instance != this) return;
        EventManagerNP.StopListening(GameEvents.InventoryChanged, RefreshOpenInventory);
    }

    void Start()
    {
        InventoryGroup.SetActive(false);
        InventoryPanel.SetActive(false);
        CloseDetailedPanel();
        ClearBubbleInfo();
        inventoryButton.gameObject.SetActive(true);
    }

    //点击背包按钮 - 打开背包
    public void OnOpenClicked()
    {
        InventoryPanel.SetActive(true);
        CloseDetailedPanel();
        RefreshBubbleInfo();
    }

    //点击关闭按钮 - 关闭背包
    public void OnCloseClicked()
    {
        CloseDetailedPanel();
        InventoryPanel.SetActive(false);
    }

    //启用背包功能，显示按钮并渲染气泡
    public void EnableInventory()
    {
        CloseDetailedPanel();
        InventoryGroup.SetActive(true);
        InventoryPanel.SetActive(false);
        // 显示按钮，并渲染气泡
        ShowInventoryButton();
    }

    //禁用背包功能
    public void DisableInventory()
    {
        CloseDetailedPanel();
        InventoryGroup.SetActive(false);
    }

    //显示背包按钮并渲染气泡
    public void ShowInventoryButton()
    {
        inventoryButton.gameObject.SetActive(true);
        BubblePanel.SetActive(true);
    }

    //隐藏气泡面板
    public void HideBubblePanel()
    {
        BubblePanel.SetActive(false);
    }

    public void RenderBubbleInfo(RelicData relic)
    {
        if (relic != null)
        {
            ShowDetailedPanel(relic);
            if (itemNameText != null)
            {
                itemNameText.text = relic.id;
            }
            if (itemDescText != null)
            {
                itemDescText.text = relic.desc;
            }
            if (itemIcon != null)
            {
                itemIcon.sprite = relic.icon;
                itemIcon.preserveAspect = true;
            }
        }
    }

    private void RefreshOpenInventory()
    {
        if (InventoryPanel != null && InventoryPanel.activeInHierarchy)
        {
            CloseDetailedPanel();
            RefreshBubbleInfo();
        }
    }

    private void ClearBubbleInfo()
    {
        if (bubbleInfoPanel == null) return;
        // 容器仅用于运行时条目，同时清理场景中预览用的 BubbleInfo。
        foreach (Transform child in bubbleInfoPanel)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public void RefreshBubbleInfo()
    {
        ClearBubbleInfo();
        if (bubbleInfoPanel == null || bubbleInfoPrefab == null || Inventory.instance == null) return;
        // 与 bubble1、bubble2、bubble3 使用同一个背包顺序；空槽不创建详情。
        var items = Inventory.instance.inventoryItems;
        for (int i = 0; i < items.Count; i++)
        {
            RelicData relic = items[i].relicData;
            var view = Instantiate(bubbleInfoPrefab, bubbleInfoPanel);
            view.name = "BubbleInfo" + (i + 1);
            var image = view.transform.Find("BubbleImage").GetComponent<Image>();
            image.sprite = relic.icon;
            image.preserveAspect = true;
            view.transform.Find("BubbleTag").GetComponent<TextMeshProUGUI>().text = relic.tag;
            view.transform.Find("DetailButton").GetComponent<Button>().onClick
                .AddListener(() => ShowDetailedPanel(relic));
        }
    }

    public void ShowDetailedPanel(RelicData relic)
    {
        if (relic == null || detailedPanel == null) return;
        detailedPanel.SetActive(true);
        detailedPanel.transform.SetAsLastSibling();
        if (relicBG != null)
        {
            relicBG.sprite = relic.icon;
            relicBG.preserveAspect = true;
            Color color = relicBG.color;
            color.a = 1f;
            relicBG.color = color;
        }
        if (relicDesc != null) relicDesc.text = relic.desc;
        if (relicDescScroll != null)
        {
            relicDescScroll.horizontal = false;
            relicDescScroll.vertical = true;
            relicDescScroll.scrollSensitivity = 0f;
            relicDescScroll.inertia = false;
            Canvas.ForceUpdateCanvases();
            if (relicDescScroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(relicDescScroll.content);
            relicDescScroll.StopMovement();
            relicDescScroll.verticalNormalizedPosition = 1f;
        }
    }

    // 在 Inspector 中绑定到 DetailCloseButton 的 OnClick。
    public void CloseDetailedPanel()
    {
        if (detailedPanel != null) detailedPanel.SetActive(false);
    }

}
