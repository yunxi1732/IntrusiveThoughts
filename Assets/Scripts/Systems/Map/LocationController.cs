using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

//场景模块，管理场景内的角色显示，对白显示与交互

public class LocationController : MonoBehaviour
{
    //单例模式
    public static LocationController instance { get; private set; }

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
    public GameObject npcmodelPanel;   //角色模型面板
    public GameObject DialoguePanel;   //对白窗口
    public TextMeshProUGUI DialogueCharacter;   //对白角色名
    public TextMeshProUGUI DialogueText;   //对白文本
    public Image StoryImage;   //剧情图组件

    public string currentLocation;

    void Start()
    {
        InitLocation();
    }

    public void InitLocation()
    {
        //显示背包按钮，并显示可用气泡
        Inventory.instance.EnableInventory();
        Inventory.instance.ShowBubblePanel();


        currentLocation = StoryPlayer.instance.currentLocation;
        EventManager1P<string>.TriggerEvent(GameEvents.AudioLocationChanged, currentLocation);
        //获取该场景下npc
        Location_UI.instance.ShowBackGround();
        //设置背景图

        //生成角色并放置位置
        GameObject npcPrefab = NPCManager.instance.npcBodyPrefab;
        //清空残留角色
        foreach (Transform child in npcmodelPanel.transform) Destroy(child.gameObject);
        //生成场景内角色
        foreach (var npc in NPCManager.instance.GetNPCsAtLocation(currentLocation))
        {
            GameObject npcObj = Instantiate(npcPrefab, npcmodelPanel.transform);
            //初始化npcObj
            npcObj.GetComponent<NPCEntry>().Initialize(npc);
        }

        
        //关闭对话框
        DialoguePanel.SetActive(false);
        StoryImage.gameObject.SetActive(false);
    }

    public void UpdateNPCs()
    {
        if (string.IsNullOrEmpty(currentLocation)) return; 
        //重新获取该场景下的npc
        List<NPCData> currentNpcs = NPCManager.instance.GetNPCsAtLocation(currentLocation);

        //生成currentNpcs中新增的npc
        foreach (var npc in currentNpcs)
        {
            bool exists = false;
            foreach (Transform child in npcmodelPanel.transform)
            {
                NPCEntry npcEntry = child.GetComponent<NPCEntry>();
                if (npcEntry != null && npcEntry.npcData.id == npc.id)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                GameObject npcObj = Instantiate(NPCManager.instance.npcBodyPrefab, npcmodelPanel.transform);
                npcObj.GetComponent<NPCEntry>().Initialize(npc);
            }
        }

        //销毁npcmodelPanel中不在currentNpcs中的npc
        foreach (Transform child in npcmodelPanel.transform)
        {
            NPCEntry npcEntry = child.GetComponent<NPCEntry>();
            if (npcEntry != null && !currentNpcs.Contains(npcEntry.npcData))
            {
                Destroy(child.gameObject);
            }
        }
    }

    


}
