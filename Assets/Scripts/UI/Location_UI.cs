using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class LocationViewSettings
{
    public string locationName;
    public GameObject background;
    public GameObject npcPanel;
}

//设置游戏
//管理游戏设置，音量，语言等
public class Location_UI : MonoBehaviour
{
    //单例模式
    public static Location_UI instance { get; private set; }

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

    public GameObject LocationPanel;   //地点主界面
    public Image BackgroundImage;   //背景图片组件
    [Header("各地点的背景对象与 NPC 容器（位置直接在场景中编辑）")]
    public List<LocationViewSettings> locations = new List<LocationViewSettings>();

    public LocationViewSettings CurrentView => locations.Find(item => item.locationName == StoryPlayer.instance.currentLocation);

    void Start()
    {
        LocationPanel.SetActive(true);
    }


    public void OnExitLocationClicked()
    {
        if (StoryManager.instance.IsProcessingLeave) return;

        //如果教程3没有完成，无法睡觉
        if (StoryManager.instance.GetState("story.tutorial.3") != 1)
        {
            //触发警告对话
            StoryManager.instance.TryStart(null, null, "tutorial3");
            return;
        }

        AudioManager.instance.PlaySFX("effect_button");

        //结束剧情交互
        StoryManager.instance.ResetStory();
        //关闭背包按钮
        Inventory.instance.DisableInventory();
        //切换场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    public void ShowBackGround()
    {
        LocationPanel.SetActive(true);
        var view = CurrentView;
        foreach (var settings in locations)
            if (settings.background != null) settings.background.SetActive(settings == view);
        if (view != null && view.background != null)
            BackgroundImage = view.background.GetComponent<Image>();
    }
}
