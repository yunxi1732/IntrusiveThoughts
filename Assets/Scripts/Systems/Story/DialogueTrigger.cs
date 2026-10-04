using UnityEngine;

//挂在场景或NPC上：进入场景时自动触发，或由交互调用 Trigger()
public class DialogueTrigger : MonoBehaviour
{
    public string npc;
    public bool triggerOnStart = true;

    private void Start()
    {
        if (triggerOnStart) Trigger();
    }

    public void Trigger()
    {
        if (StoryManager.instance != null) StoryManager.instance.TryStart(npc);
    }
}
