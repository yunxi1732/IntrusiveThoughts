using System;

// 条件查询入口：只负责路由，各系统保存自己的数据。
public static class GameStateAccess
{
    //任意状态被 Set 后触发，供 UIGate 等刷新
    public static event Action Changed;    //当游戏中发生状态改变时，可能触发对话，或者设置UI可见性
    private static string Attribute(string key, string prefix)
    {
        string attribute = key.Substring(prefix.Length);
        if (string.IsNullOrWhiteSpace(attribute)) throw new ArgumentException($"属性名不能为空：{key}");
        return attribute;
    }

    public static int Get(string key)
    {
        ValidateKey(key);
        if (key == "day") return DateController.instance.currentDate;
        if (key.StartsWith("player.", StringComparison.Ordinal))
            return GameState.instance.player.GetValue(Attribute(key, "player."));
        if (key.StartsWith("npc.", StringComparison.Ordinal))
        {
            SplitNpc(key, out string id, out string attribute);
            return NPCManager.instance.GetNpcState(id, attribute);
        }
        if (key.StartsWith("relic.", StringComparison.Ordinal))
        {
            //增加遗物相关功能识别,例如玩家是否携带某个遗物 todo
            SplitRelic(key, out string id, out string attribute);
            return RelicManager.instance.GetRelicState(id, attribute);
        }
        return StoryManager.instance.GetStoryState(key);
    }

    public static void Set(string key, int value)
    {
        ValidateKey(key);
        if (key == "day") DateController.instance.SetDate(value);
        else if (key.StartsWith("player.", StringComparison.Ordinal))
            GameState.instance.player.SetValue(Attribute(key, "player."), value);
        else if (key.StartsWith("npc.", StringComparison.Ordinal))
        {
            SplitNpc(key, out string id, out string attribute);
            NPCManager.instance.SetNpcState(id, attribute, value);
        }
        else StoryManager.instance.SetStoryState(key, value);
        Changed?.Invoke();
    }

    private static void SplitNpc(string key, out string id, out string attribute)
    {
        int separator = key.IndexOf('.', 4);
        if (separator <= 4 || separator == key.Length - 1)
            throw new ArgumentException($"NPC 状态必须使用 npc.<ID>.<属性>：{key}");
        id = key.Substring(4, separator - 4);
        attribute = key.Substring(separator + 1);
    }

    private static void SplitRelic(string key, out string id, out string attribute)
    {
        int separator = key.IndexOf('.', 6);
        if (separator <= 6 || separator == key.Length - 1)
            throw new ArgumentException($"遗物状态必须使用 relic.<ID>.<属性>：{key}");
        id = key.Substring(6, separator - 6);
        attribute = key.Substring(separator + 1);
    }

    public static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("状态 key 不能为空。");
        if (key == "day") return;
        if (key.StartsWith("player.", StringComparison.Ordinal)) { Attribute(key, "player."); return; }
        if (key.StartsWith("npc.", StringComparison.Ordinal)) { SplitNpc(key, out _, out _); return; }
        if (key.StartsWith("relic.", StringComparison.Ordinal)) { SplitRelic(key, out _, out _); return; }
        if (key.StartsWith("story.", StringComparison.Ordinal)) { Attribute(key, "story."); return; }
        throw new ArgumentException($"未知状态归属：{key}，请使用 day、player.*、npc.<ID>.*、relic.<ID>.* 或 story.*。");
    }
}
