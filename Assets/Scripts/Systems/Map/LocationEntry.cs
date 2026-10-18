using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[System.Serializable]
public class LocationInfo
{
    public string locationName;   //地点名称
    public List<NPCData> npcList = new List<NPCData>();   //该地点的角色列表
}

//map中的可交互地点
public class LocationEntry : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup canvasGroup;   //地点的CanvasGroup，用于控制显示与交互
    public string locationName;   //地点名称
    public List<NPCData> npcList = new List<NPCData>();   //该地点的角色列表
    public GameObject npcIconPanel;   //地点角色展示容器
    private readonly Dictionary<string, GameObject> dialogueMarks = new Dictionary<string, GameObject>();

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowNPCs()
    {
        //刷新大地图NPC显示位置
        GameObject iconPrefab = NPCManager.instance.npcIconPrefab;
        foreach (Transform child in npcIconPanel.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        npcList.Clear();
        dialogueMarks.Clear();
        foreach (var npc in NPCManager.instance.GetNPCsAtLocation(locationName))
        {
            npcList.Add(npc);
            GameObject icon = Instantiate(iconPrefab, npcIconPanel.transform);
            var mark = icon.transform.Find("NewDiaMark") ?? icon.transform.Find("NewDIaMark");
            if (mark != null) dialogueMarks[npc.id] = mark.gameObject;
            // 尺寸和缩放沿用 NPCIcon 预制件；头像放在 Mask 内参与裁剪。
            var portrait = icon.transform.Find("Mask/NPCImage");
            var image = portrait != null ? portrait.GetComponent<Image>() : null;
            if (image != null)
            {
                image.sprite = npc.icon;
                image.enabled = npc.icon != null;
            }
            else
            {
                Debug.LogError("NPCIcon 缺少带 Image 组件的 Mask/NPCImage 子物体。", icon);
            }
        }
        // 头像条与按钮现在是同级，展示元素不能挡住按钮的射线。
        foreach (var graphic in npcIconPanel.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        // 地点美术与点击区域使用同一个开放条件；出租屋始终可以返回。
        bool open = locationName == "出租屋" || npcList.Count > 0;
        npcIconPanel.SetActive(npcList.Count > 0);
        RefreshLocationArtwork(open);
        RefreshDialogueMarks();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        canvasGroup.alpha = open ? 1f : 0f;
        canvasGroup.interactable = open;
        canvasGroup.blocksRaycasts = open;
    }

    private void RefreshLocationArtwork(bool open)
    {
        // 六个地点共用的结构：按钮、地点美术和头像容器同属一个地点根节点。
        // 美术不接收点击，由同级的透明按钮统一处理地点交互。
        if (transform.parent == null) return;
        foreach (Transform sibling in transform.parent)
        {
            if (sibling == transform || sibling.gameObject == npcIconPanel) continue;
            var artwork = sibling.GetComponent<Image>();
            if (artwork != null)
            {
                artwork.enabled = open;
                artwork.raycastTarget = false;
            }
        }
    }

    public void RefreshDialogueMarks()
    {
        if (StoryManager.instance == null) return;
        foreach (var pair in dialogueMarks)
        {
            if (pair.Value == null) continue;
            pair.Value.SetActive(StoryManager.instance.HasNewNpcInteraction(pair.Key, locationName));
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.instance.PlaySFX(3);
        //Debug.Log("LocationEntry clicked: " + gameObject.name);
        StoryPlayer.instance.currentLocation = locationName;

        if (locationName == "出租屋")
        {
            //切换场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("BedRoomScene");
        } else {
            //切换场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("LocationScene");
        }
    }
}
