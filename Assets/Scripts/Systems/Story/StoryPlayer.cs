using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

//对话播放系统

public class StoryPlayer : MonoBehaviour
{
    public static StoryPlayer instance { get; private set; }

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
    public GameObject StoryPanel;   //剧情面板
    public GameObject skipButton;
    public Image StoryImage;   //剧情图组件
    public Image CharacterImg;   //角色模型面板
    public Image CharacterShadowImg;   //角色阴影面板
    public GameObject DialoguePanel;   //对白窗口
    public TextMeshProUGUI DialogueCharacter;   //对白角色名
    public TextMeshProUGUI DialogueText;   //对白文本

    public string currentLocation;
    public string interactiveRelic;  //可交互气泡
    public string currentCharacter;  //当前交互对象
    public bool canInteract = true;  //是否可以交互跳过
    private bool autoCheckPending;
    [Header("场景小人离开动画")]
    [Min(0f)] public float npcLeaveDuration = 0.4f;

    private void OnEnable()
    {
        EventManager1P<bool>.StartListening(GameEvents.DialoguePlayingChanged, SetSkipButtonVisible);
        SetSkipButtonVisible(StoryManager.instance != null && StoryManager.instance.IsPlaying);
    }

    private void OnDisable()
    {
        EventManager1P<bool>.StopListening(GameEvents.DialoguePlayingChanged, SetSkipButtonVisible);
    }

    private void SetSkipButtonVisible(bool playing)
    {
        if (skipButton != null) skipButton.SetActive(playing);
    }

    void Start()
    {
        StoryPanel.SetActive(false);
        //关闭对话框
        DialoguePanel.SetActive(false);
        StoryImage.gameObject.SetActive(false);
        StoryManager.instance.OnLine += HandleDialogueLine;
        StoryManager.instance.OnDialogueEnd += HandleDialogueEnd;
        GameStateAccess.Changed += OnStateChange;
        SetSkipButtonVisible(StoryManager.instance.IsPlaying);
    }

    private void OnDestroy()
    {
        if (StoryManager.instance != null)
        {
            StoryManager.instance.OnLine -= HandleDialogueLine;
            StoryManager.instance.OnDialogueEnd -= HandleDialogueEnd;
        }

        GameStateAccess.Changed -= OnStateChange;
    }

    void OnStateChange()
    {
        RequestAutoCheck();
    }

    // 合并同一帧的多次状态变化，等场景初始化、对话和转场完成后检查。
    public void RequestAutoCheck() => autoCheckPending = true;

    private void LateUpdate()
    {
        if (!autoCheckPending || StoryManager.instance == null) return;
        if (SaveSlot_UI.instance != null && SaveSlot_UI.instance.IsOpen) return;
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "BedRoomScene" && scene != "LocationScene") return;
        if (StoryManager.instance.IsPlaying || StoryManager.instance.IsProcessingLeave) return;
        if (ScreenTransition.Instance != null && ScreenTransition.Instance.IsTransitioning) return;
        autoCheckPending = false;
        StoryManager.instance.TryStart(null, null, "auto");
    }

    void HandleDialogueLine(DialogueLine line)
    {
        StoryPanel.SetActive(true);
        if (line == null)
            return;


        //如果line的readTime不为空，需要设置等待时长，时间结束后才可以交互跳过
        if (line.waitTime > 0)
        {
            canInteract = false;
            //设置等待时长，时间结束后才可以交互跳过
            StartCoroutine(WaitForReadTime(line.waitTime));
        }

        Debug.Log("Handling dialogue line: " + line + ", content: " + line.content + ", image: " + line.image);
        //设置背景图
        if (!string.IsNullOrEmpty(line.image))
        {
            //播放背景图
            Debug.Log("Loading background image: " + line.image);
            StoryImage.sprite = Resources.Load<Sprite>("Sprites/" + line.image);
            StoryImage.gameObject.SetActive(true);
        } else
        {
            StoryImage.gameObject.SetActive(false);
        }

        //设置对话内容
        if (!string.IsNullOrEmpty(line.content))
        {
            //显示对白文本
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
            CharacterImg.sprite = line.GetCharacterSprite();
            CharacterShadowImg.sprite = line.GetCharacterSprite();
        } else 
        {
            DialoguePanel?.SetActive(false);
        }

        //处理特殊动作
        DoAction(line.action);

        //处理交互状态
        interactiveRelic = line.relics;
        currentCharacter = line.character;
    }

    public bool IsMatchRelic(string relicId)
    {
        //interactiveRelic是分号隔开的relic列表，查找interactiveRelic是否包含relicId
        if (string.IsNullOrEmpty(interactiveRelic)) return false;
        if (interactiveRelic == "any") return true;

        var relics = interactiveRelic.Split(';', '；');
        foreach (var relic in relics)
        {
            if (relic.Trim() == relicId) return true;
        }
        return false;
    }
    


    //执行附带的特殊动作，例如UI动画，教程引导等
    //line/sequence结束时，或者遗物被拾取时，都可以执行动作
    public void DoAction(string actions)
    {
        if (string.IsNullOrEmpty(actions)) return;
        foreach (var part in actions.Split(';'))
        {
            string action = part.Trim();
            if (action.Length > 0) DoSingleAction(action);
        }
    }

    private void DoSingleAction(string action)
    {
        //在这里处理特殊动作的逻辑，例如播放动画、触发事件等
        Debug.Log("Processing action: " + action);
        switch (action)
        {
            case "closeInventory":
                //对话时隐藏背包
                Inventory.instance.DisableInventory();
                break;
            case "showInventory":
                //显示背包
                Inventory.instance.EnableInventory();
                break;
            case "updaterelic":
                if (SceneManager.GetActiveScene().name == "BedRoomScene" && BedroomController.instance != null)
                    BedroomController.instance.UpdateRelics();
                break;
            case "updatenpc":
                //刷新npc，遍历获取本场景中的npc，如果新增则添加，否则删除
                Debug.Log("update npc in theater action: " + action);
                // Unity 已销毁对象不能用 ?. 判空；只刷新当前场景的控制器。
                if (SceneManager.GetActiveScene().name == "LocationScene" && LocationController.instance != null)
                    LocationController.instance.UpdateNPCs();
                else if (SceneManager.GetActiveScene().name == "BedRoomScene" && BedroomController.instance != null)
                    BedroomController.instance.UpdateNPCs();
                break;
            //在这里添加特殊动作的处理逻辑，例如播放动画、触发事件等
            default:
                //播放特殊对话
                if (action.StartsWith("play.", StringComparison.Ordinal))
                {
                    //播放特殊对话的逻辑
                    string value = action.Substring("play.".Length);
                    Debug.Log("Playing special dialogue: " + value);
                } else {
                    Debug.LogWarning("Unhandled play action: " + action);
                }
                break;
        }
    }

    IEnumerator WaitForReadTime(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        canInteract = true;
    }

    public void OnDialogueClicked()
    {
        if (canInteract && !StoryManager.instance.IsProcessingLeave)
        {
            StoryManager.instance.NextDialogue();
        }
    }

    // 只淡出场景小人及其子节点（包括阴影），不修改对白或半身立绘。
    public IEnumerator FadeNpcBodies(HashSet<string> ids)
    {
        var bodies = FindNpcBodies(ids);
        var groups = new List<CanvasGroup>();
        var alphas = new List<float>();
        foreach (var body in bodies)
        {
            var group = body.BodyCanvasGroup;
            groups.Add(group);
            alphas.Add(group.alpha);
        }
        float duration = Mathf.Max(0f, npcLeaveDuration);
        float elapsed = 0f;
        while (elapsed < duration && groups.Count > 0)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < groups.Count; i++)
                if (groups[i] != null) groups[i].alpha = Mathf.Lerp(alphas[i], 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        foreach (var body in bodies)
            if (body != null)
            {
                body.BodyCanvasGroup.alpha = 0f;
                body.gameObject.SetActive(false);
            }
    }

    public void HideNpcBodies(HashSet<string> ids)
    {
        foreach (var body in FindNpcBodies(ids))
        {
            body.BodyCanvasGroup.alpha = 0f;
            body.gameObject.SetActive(false);
        }
    }

    private static List<NPCEntry> FindNpcBodies(HashSet<string> ids)
    {
        var result = new List<NPCEntry>();
        foreach (var body in FindObjectsByType<NPCEntry>(FindObjectsSortMode.None))
            if (body.gameObject.scene == SceneManager.GetActiveScene() && body.npcData != null && ids.Contains(body.npcData.id))
                result.Add(body);
        return result;
    }

    void HandleDialogueEnd(DialogueSequence sequence)
    {
        StoryPanel.SetActive(false);
        StoryImage.gameObject.SetActive(false);
        DialoguePanel.SetActive(false);

        //特殊处理的sequence 
        //播放完之后回到卧室
        // if (sequence != null && sequence.id == "seq_day0_before")
        // {
        //     //回到卧室的逻辑
        //     DateController.instance.SetDate(DateController.instance.currentDate + 1);
        //     BedroomController.instance?.InitBedroom();
        // }

        DoAction(sequence.action);
    }

    //特定事件直接根据id触发剧情
    void HandleSpecialEvent(string eventId)
    {
        StoryManager.instance.StartSeqenceByID(eventId);
    }

    //达成指定条件时需要触发特定事件
    public void CheckEvent(SpecialEvent specialEvent)
    {
        //使用story.eventid记录事件是否已经触发过
        switch (specialEvent)
        {
            case SpecialEvent.CollectRelic:
                HandleSpecialEvent("collectRelic");
                break;
            default:
                Debug.LogWarning("Unhandled special event: " + specialEvent);
                break;
        }
    }

}

public enum SpecialEvent
{
    CollectRelic,
    //在这里添加其他特定事件枚举
}
