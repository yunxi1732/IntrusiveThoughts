using System;

// 条件查询入口：只负责路由，各系统保存自己的数据。
public static class GameStateAccess
{
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
    }

    private static void SplitNpc(string key, out string id, out string attribute)
    {
        int separator = key.IndexOf('.', 4);
        if (separator <= 4 || separator == key.Length - 1)
            throw new ArgumentException($"NPC 状态必须使用 npc.<ID>.<属性>：{key}");
        id = key.Substring(4, separator - 4);
        attribute = key.Substring(separator + 1);
    }

    public static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("状态 key 不能为空。");
        if (key == "day") return;
        if (key.StartsWith("player.", StringComparison.Ordinal)) { Attribute(key, "player."); return; }
        if (key.StartsWith("npc.", StringComparison.Ordinal)) { SplitNpc(key, out _, out _); return; }
        if (key.StartsWith("story.", StringComparison.Ordinal)) { Attribute(key, "story."); return; }
        throw new ArgumentException($"未知状态归属：{key}，请使用 day、player.*、npc.<ID>.* 或 story.*。");
    }
}
