using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


//map中的可交互地点

public class LocationEntry : MonoBehaviour, IPointerClickHandler
{
    //public LocData locData;
    public string locationName;   //地点名称

    void Start()
    {
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log("LocationEntry clicked: " + gameObject.name);
        MapController.instance.SelectLocationEntry(this);
    }
}
