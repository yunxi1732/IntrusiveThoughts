using System;

// 旧存档的玩家属性格式，仅供 SaveManager 迁移；运行时统一使用 NPCData。
[Serializable]
public class PlayerData
{
    public System.Collections.Generic.List<StateValueData> attributes = new System.Collections.Generic.List<StateValueData>();

    public int GetValue(string attribute)
    {
        var entry = attributes.Find(item => item.key == attribute);
        return entry == null ? 0 : entry.value;
    }

    public void SetValue(string attribute, int value)
    {
        var entry = attributes.Find(item => item.key == attribute);
        if (entry == null) attributes.Add(new StateValueData { key = attribute, value = value });
        else entry.value = value;
    }
}

