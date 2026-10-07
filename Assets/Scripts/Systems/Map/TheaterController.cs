using UnityEngine;
using UnityEngine.UI;
using TMPro;

//剧场模块，管理剧场内的角色显示，对白显示与交互

public class TheaterController : MonoBehaviour
{
    //单例模式
    public static TheaterController instance { get; private set; }

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

    public string interactiveRelic;  //可交互气泡
    public LocationEntry currentLocation;

    void Start()
    {
        StoryManager.instance.OnLine += HandleDialogueLine;
        StoryManager.instance.OnDialogueEnd += HandleDialogueEnd;
    }

    private void OnDestroy()
    {
        if (StoryManager.instance != null)
        {
            StoryManager.instance.OnLine -= HandleDialogueLine;
            StoryManager.instance.OnDialogueEnd -= HandleDialogueEnd;
        }
    }

    public void InitTheater(LocationEntry location)
    {
        //显示背包按钮，并显示可用气泡
        Inventory.instance.EnableInventory();
        Inventory.instance.ShowBubblePanel();


        currentLocation = location;
        //根据角色 + 地点初始化剧场
        Map_UI.instance.ShowTheaterPanel();
        //设置背景图

        //生成角色并放置位置
        GameObject npcPrefab = NPCManager.instance.npcBodyPrefab;
        foreach (Transform child in npcmodelPanel.transform) Destroy(child.gameObject);
        foreach (var npc in location.npcList)
        {
            GameObject npcObj = Instantiate(npcPrefab, npcmodelPanel.transform);
            //初始化npcObj
            npcObj.GetComponent<NPCEntry>().Initialize(npc);
        }

        
        //关闭对话框
        DialoguePanel.SetActive(false);
    }

    void HandleDialogueLine(DialogueLine line)
    {
        Debug.Log("触发对话语句: " + line);
        if (DialoguePanel == null)
        {
            Debug.LogError("DialoguePanel is not assigned.");
            return;
        }
        DialoguePanel.SetActive(true);
        //处理对白逻辑
        DialogueCharacter.text = line.character;
        DialogueText.text = line.content;
        //处理交互状态
        interactiveRelic = line.relic;
    }

    public void OnDialogueClicked()
    {
        StoryManager.instance.NextDialogue();
    }

    void HandleDialogueEnd(DialogueSequence sequence)
    {
        DialoguePanel.SetActive(false);
    }

}
