using UnityEngine;


//游戏主循环(流程)管理
public class GameLoop : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static GameLoop instance { get; private set; }

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
}
