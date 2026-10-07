using UnityEngine;
using UnityEngine.UI;
using TMPro;

//设置游戏
//管理游戏设置，音量，语言等
public class Map_UI : MonoBehaviour
{
    //单例模式
    public static Map_UI instance { get; private set; }

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

    public GameObject TheaterPanel;   //剧场主界面

    //结束当天按钮
    public void OnEndDayClicked()
    {
        //endConfirmPanel.SetActive(true);
        //结算当天数据，保存状态，更新游戏时间
        DateController.instance.PassDay();
        //加载卧室场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("BedRoomScene");
    }

    public void OnExitTheaterClicked()
    {
        //结束剧情交互
        StoryManager.instance.ResetStory();
        //关闭背包按钮
        Inventory.instance.DisableInventory();
        TheaterPanel.SetActive(false);
    }

    public void ShowTheaterPanel()
    {
        TheaterPanel.SetActive(true);
    }
}
