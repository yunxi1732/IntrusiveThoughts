using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


//theater中的可交互NPC

public class NPCEntry : MonoBehaviour, IPointerClickHandler
{
    //public LocData locData;
    public NPCData npcData;   //NPC数据
    private Image iconImage;

    void Start()
    {
        npcData = NPCManager.instance.GetNPCDataByName(npcData.id);
        iconImage = GetComponent<Image>();
        if (iconImage != null && npcData != null)
        {
            iconImage.sprite = npcData.icon;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        //如果当前在播放剧情，禁止交互
        if (StoryManager.instance.IsPlaying)
        {
            return;
        }
        //点击npc后触发剧情交互
        TheaterController.instance.interactiveRelic = null; // 清空当前交互遗物
        StoryManager.instance.TryStart(npcData.id); // 尝试开始与该NPC相关的剧情
    }
}
