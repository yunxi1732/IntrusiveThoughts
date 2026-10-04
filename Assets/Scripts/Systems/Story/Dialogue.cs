using System;
using System.Collections.Generic;

[Serializable]
public class DialogueLine
{
    public string character;
    [UnityEngine.TextArea] public string content;
    public string relic;  //关联的遗物名称，不为空时表示可以互动
}

[Serializable]
public class DialogueLineJsons
{
    public DialogueLine[] lines;
}

public enum CompareOp { Equal, NotEqual, Greater, GreaterEqual, Less, LessEqual }

//条件：读取状态值（key）并与 value 比较。玩家状态用 "player.xxx"，NPC 状态用 "npc.<名字>.xxx"
[Serializable]
public class DialogueCondition
{
    public string key;
    public CompareOp op = CompareOp.GreaterEqual;
    public int value = 1;

    public bool Check(int current)
    {
        switch (op)
        {
            case CompareOp.Equal: return current == value;
            case CompareOp.NotEqual: return current != value;
            case CompareOp.Greater: return current > value;
            case CompareOp.GreaterEqual: return current >= value;
            case CompareOp.Less: return current < value;
            default: return current <= value;
        }
    }
}

//一段对话：满足场景、NPC、条件时可触发，同时满足多段时取 priority 最高的
[Serializable]
public class DialogueSequence
{
    public string id;
    public string sceneName;        //留空表示任意场景
    public string npc;              //留空表示无需特定 NPC
    public string relic;            //留空表示无需特定遗物
    public int priority;
    public bool once = true;        //播放一次后不再触发
    public List<DialogueCondition> conditions = new List<DialogueCondition>();
    public List<DialogueLine> lines = new List<DialogueLine>();
    public List<DialogueCondition> onFinishSet = new List<DialogueCondition>();  //结束后设置状态（key = value）
}
