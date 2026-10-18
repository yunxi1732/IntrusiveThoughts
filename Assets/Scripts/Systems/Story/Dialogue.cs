using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string sequenceId;  //所属的对话序列
    public string character;
    [UnityEngine.TextArea] public string content;
    public string relics;  //关联的遗物名称，不为空时表示可以互动
    public string tag;   //Resources/Sprites 下的立绘图片名；FIFA 表示不显示立绘
    public string image;  //关联的图片资源名称
    public string sfx;  //关联的音效资源名称
    public float waitTime;  //等待时长，单位为秒
    public string action;  //关联的特殊动作
    public string leave;   //本句读完后离开的 NPC ID，使用分号分隔

    public Sprite GetCharacterSprite() {
        string imageName = tag?.Trim();
        if (string.Equals(imageName, "FIFA", StringComparison.OrdinalIgnoreCase)) return null;
        if (!string.IsNullOrEmpty(imageName))
        {
            Sprite sprite = Resources.Load<Sprite>("Sprites/" + imageName);
            if (sprite == null)
                Debug.LogWarning($"台词 {sequenceId} 的立绘不存在：Sprites/{imageName}（角色：{character}）");
            return sprite;
        }

        // tag 留空时保留默认立绘；所有角色（包括余予）使用 NPC 配置。
        var npc = NPCManager.instance.GetNPCDataByName(character);
        return npc != null ? npc.model : null;
    }
}

[Serializable]
public class DialogueLineJsons
{
    public DialogueLine[] lines;
}

public enum CompareOp { Equal, NotEqual, Greater, GreaterEqual, Less, LessEqual }

public enum StateChangeOp { Set, Add }

[Serializable]
public class DialogueStateChange
{
    public string key;
    public StateChangeOp op;
    public int value;

    public void Apply()
    {
        int result = op == StateChangeOp.Add ? checked(GameStateAccess.Get(key) + value) : value;
        GameStateAccess.Set(key, result);
    }
}

//条件：读取状态值（key）并与 value 比较。人物统一用 "npc.<名字>.xxx"，player.xxx 兼容映射到主角。
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
    public string location;        //留空表示任意场景
    public string npc;              //留空表示无需特定 NPC
    public string relic;            //留空表示无需特定遗物
    public string seqType;        //对话类型：normal（普通对话）、auto（自动播放对话）、relicfail（遗物交互失败对话）
    public int priority;
    public bool once = true;        //播放一次后不再触发
    public string action;           //关联的特殊动作
    public List<DialogueCondition> conditions = new List<DialogueCondition>();
    public List<DialogueLine> lines = new List<DialogueLine>();
    public List<DialogueStateChange> onFinishSet = new List<DialogueStateChange>();

    public bool CheckRelic(string targetRelic)
    {
        if (seqType == "relicfail") return true;
        if (string.IsNullOrWhiteSpace(relic)) return string.IsNullOrWhiteSpace(targetRelic);
        if (string.IsNullOrWhiteSpace(targetRelic)) return false;
        foreach (var item in relic.Split(';', '；'))
        {
            string id = item.Trim();
            if (id == "any" || id == targetRelic.Trim()) return true;
        }
        return false;
    }
}
