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

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowNPCs()
    {
        //刷新大地图NPC显示位置
        GameObject iconPrefab = NPCManager.instance.npcIconPrefab;
        foreach (Transform child in npcIconPanel.transform) Destroy(child.gameObject);
        npcList.Clear();
        foreach (var npc in NPCManager.instance.GetNPCsAtLocation(locationName))
        {
            npcList.Add(npc);
            GameObject icon = Instantiate(iconPrefab, npcIconPanel.transform);
            var image = icon.GetComponent<Image>();
            if (image != null) image.sprite = npc.icon;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // 教程结束后的地图仍处于第 0 天，必须保留返回卧室的入口。
        if (locationName == "出租屋" || npcList.Count > 0)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
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
