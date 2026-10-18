using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


//theater中的可交互NPC
//挂载在场景中可交互的NPC对象上

public class NPCEntry : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup BodyCanvasGroup
    {
        get
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            return group;
        }
    }
    //public LocData locData;
    public NPCData npcData;   //NPC数据
    [SerializeField] private Image iconImage;

    public void Initialize(NPCData data)
    {
        npcData = data;
        BodyCanvasGroup.alpha = 1f;
        BodyCanvasGroup.blocksRaycasts = true;
        if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);
        if (iconImage != null && npcData != null)
        {
            iconImage.sprite = npcData.icon;
            iconImage.preserveAspect = true;
        }
    }

    public void RefreshIcon()
    {
        if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);
        if (iconImage != null && npcData != null) iconImage.sprite = npcData.icon;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //如果当前在播放剧情，禁止交互
        if (StoryManager.instance.IsPlaying)
        {
            return;
        }
        //点击npc后触发剧情交互
        StoryPlayer.instance.interactiveRelic = null; // 清空当前交互遗物
        // 尝试开始与该NPC相关的剧情
        if (StoryManager.instance.TryStart(npcData.id)
            && StoryPlayer.instance.DialoguePanel.activeInHierarchy)
        {
            AudioManager.instance.PlaySFX(2);
        }
    }
}
