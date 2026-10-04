using UnityEngine;


//存档系统
//保存当前游戏状态
//载入之前游戏进度
//清空存档
public class SaveManager : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static SaveManager instance { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //保存当前游戏状态
    public void SaveGame()
    {
        //实现保存逻辑
    }

    //载入之前游戏进度
    public void LoadGame()
    {
        //实现载入逻辑
    }

    //清空存档
    public void ClearSave()
    {
        //实现清空存档逻辑
    }

    //检查是否存在存档
    public bool HasSave()
    {
        //实现检查存档逻辑
        return false;
    }
}
