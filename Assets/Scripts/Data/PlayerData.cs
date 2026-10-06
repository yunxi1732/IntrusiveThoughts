using System;

// 玩家自己的运行时数据；属性名不带 player. 前缀。
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

