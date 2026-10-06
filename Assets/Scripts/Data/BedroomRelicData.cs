using System;

// 当天卧室中的一件遗物。收集后仍保留记录，防止物件重新出现。
[Serializable]
public class BedroomRelicData
{
    public string instanceId;
    public string relicId;
    // 对应场景内固定摆放位置的唯一编号。
    public string placementId;
    public bool collected;
}
