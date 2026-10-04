using UnityEngine;
using UnityEngine.UI;
using TMPro;

//管理卧室的交互循环
//点击遗物可以查看遗物信息
//点击背包可以查看背包物品

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
    public GameObject TheaterPanel;   //剧场主界面
    public GameObject DialoguePanel;   //对白窗口
    public Button ExitBut;     //退出剧场按钮
    public Button DialogueBut;     //确认对白
    public TextMeshProUGUI DialogueCharacter;   //对白角色名
    public TextMeshProUGUI DialogueText;   //对白文本

    public string interactiveRelic;  //可交互气泡

    void Start()
    {
        DialogueBut?.onClick.AddListener(NextDialogue);
        ExitBut?.onClick.AddListener(ExitTheater);
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

        //根据角色 + 地点初始化剧场
        TheaterPanel.SetActive(true);
        //设置背景图
        //生成角色并放置位置
        
        //关闭对话框
        DialoguePanel.SetActive(false);
    }

    void PlayStory()
    {
        //如果多个角色，需要选中目标再播放对白？
        //播放剧本逻辑
        StoryManager.instance.TryStart();
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

    void NextDialogue()
    {
        StoryManager.instance.NextDialogue();
    }

    void HandleDialogueEnd(DialogueSequence sequence)
    {
        DialoguePanel.SetActive(false);
    }

    void ExitTheater()
    {
        //结束剧情交互
        StoryManager.instance.ResetStory();
        //关闭背包按钮
        Inventory.instance.DisableInventory();
        TheaterPanel.SetActive(false);
    }


}
