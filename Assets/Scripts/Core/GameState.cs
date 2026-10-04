using UnityEngine;


//保存游戏运行时状态
public class GameState : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static GameState instance { get; private set; }

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

    public void InitializeGameState()
    {
        //根据当前GameState状态初始化游戏
        Inventory.instance.InitInventory();
    }
}
