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
    public string tag;   //关联角色图片序号
    public string image;  //关联的图片资源名称
    public string sfx;  //关联的音效资源名称
    public float waitTime;  //等待时长，单位为秒
    public string action;  //关联的特殊动作

    public Sprite GetCharacterSprite() {
        if (character =="余予")
        {
            //sprite for 余予
            return NPCManager.instance.playerIcon;
        } else
        {
            var npc = NPCManager.instance.GetNPCDataByName(character);
            if (npc == null)
            {
                Debug.LogWarning($"对白角色未配置：{character}");
                return null;
            }
            return npc.GetModel(tag);
        }
    }
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
    public string location;        //留空表示任意场景
    public string npc;              //留空表示无需特定 NPC
    public string relic;            //留空表示无需特定遗物
    public string seqType;        //对话类型：normal（普通对话）、auto（自动播放对话）、relicfail（遗物交互失败对话）
    public int priority;
    public bool once = true;        //播放一次后不再触发
    public string action;           //关联的特殊动作
    public List<DialogueCondition> conditions = new List<DialogueCondition>();
    public List<DialogueLine> lines = new List<DialogueLine>();
    public List<DialogueCondition> onFinishSet = new List<DialogueCondition>();  //结束后设置状态（key = value）

    public bool CheckRelic(string targetRelic)
    {
        //relic为空不过滤
        //if (string.IsNullOrEmpty(relic)) return true;
        //都为空匹配
        if (string.IsNullOrEmpty(relic) && string.IsNullOrEmpty(targetRelic)) return true;

        //relic为any可以匹配任意互动遗物
        if (relic == "any" && !string.IsNullOrEmpty(targetRelic)) return true;
        //否则必须精确匹配互动遗物
        return relic == targetRelic;
    }
}
