using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;


//map中的可交互地点

public class LocationEntry : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup canvasGroup;   //地点的CanvasGroup，用于控制显示与交互
    public string locationName;   //地点名称
    public List<NPCData> npcList = new List<NPCData>();   //该地点的角色列表
    public GameObject npcIconPanel;   //大地图上的地点图标

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowNPCs()
    {
        //根据npc刷新大地图图标显示
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

        if (npcList.Count > 0)
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
        MapController.instance.SelectLocationEntry(this);
    }
}
