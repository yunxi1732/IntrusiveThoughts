using System;
using System.Collections.Generic;

// 单个槽位的纯进度数据。四个槽位各保存一份，读档固定回卧室。
[Serializable]
public class GameData
{
    public int version = 2;
    public int slotId;
    public string savedAtUtc;
    // 与 DateController 一致：0 表示第一天。
    public int currentDate;
    public PlayerData player = new PlayerData();
    // 只保存 story.* 标记；日期、玩家、NPC 各有独立字段。
    public List<StateValueData> storyStates = new List<StateValueData>();
    // 仅用于读取第一版存档并迁移；新存档不在这里记录数据。
    public List<StateValueData> states = new List<StateValueData>();
    public List<string> completedSequenceIds = new List<string>();
    public List<BubbleData> inventoryBubbles = new List<BubbleData>();
    // 区分“当天已经生成，但没有遗物”和“尚未生成”，避免空房间读档后重新刷新。
    public bool bedroomInitialized;
    public List<BedroomRelicData> bedroomRelics = new List<BedroomRelicData>();
    public List<NPCProgressData> npcs = new List<NPCProgressData>();
}

// 文件序列化快照，不是第二份运行时状态。不保存 NPC 的图标和固定资料。
[Serializable]
public class NPCProgressData
{
    public string npcId;
    public List<StateValueData> attributes = new List<StateValueData>();
    public List<string> tags = new List<string>();
}
