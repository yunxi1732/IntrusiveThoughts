using System;
using System.Collections.Generic;

// 单个槽位的纯进度数据。
[Serializable]
public class GameData
{
    public int version = 5;
    public string sceneName = "BedRoomScene";
    public string locationName = "出租屋";
    public bool showingBedroom;
    public int slotId;
    public string savedAtUtc;
    public bool endingCompleted;
    public List<string> achievedEndingIds = new List<string>();
    // 与 DateController 一致：0 为教程，1～7 为正式游戏日期。
    public int currentDate;
    // 仅用于读取旧存档；主角属性现保存在 npcs 的余予记录中。
    public PlayerData player;
    // 只保存 story.* 标记；日期和人物属性由各自字段保存。
    public List<StateValueData> storyStates = new List<StateValueData>();
    // 仅用于读取第一版存档并迁移；新存档不在这里记录数据。
    public List<StateValueData> states = new List<StateValueData>();
    public List<string> completedSequenceIds = new List<string>();
    public List<NPCDepartureData> npcDepartures = new List<NPCDepartureData>();
    public List<BubbleData> inventoryBubbles = new List<BubbleData>();
    // 区分“当天已经生成，但没有遗物”和“尚未生成”，避免空房间读档后重新刷新。
    public bool bedroomInitialized;
    public List<BedroomRelicData> bedroomRelics = new List<BedroomRelicData>();
    public List<NPCProgressData> npcs = new List<NPCProgressData>();
}

[Serializable]
public class NPCDepartureData
{
    public string npcId;
    public string location;
    public string sequenceId;
    // 离开时已有的剧情不能立刻把人物重新生成；后续解锁的新剧情才允许返回。
    public List<string> availableSequenceIds = new List<string>();
}

// 文件序列化快照，不是第二份运行时状态。不保存 NPC 的图标和固定资料。
[Serializable]
public class NPCProgressData
{
    public string npcId;
    public List<StateValueData> attributes = new List<StateValueData>();
    public List<string> tags = new List<string>();
}
