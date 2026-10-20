using System;
using System.Collections.Generic;

// 四个槽位共享；删除槽位或读取旧进度不应清除全局解锁记录。
[Serializable]
public class GlobalProgressData
{
    public int version = 1;
    public bool hasCompletedGame;
    // 按 priority、firstAchievedOrder 排序，重复达成不改变首次顺序。
    public List<EndingProgressData> achievedEndings = new List<EndingProgressData>();
    public List<string> unlockedEndingIds = new List<string>();
    public List<string> unlockedAchievementIds = new List<string>();
    public List<AchievementProgressData> achievementProgress = new List<AchievementProgressData>();
    // 保存滑条原值，以 Mixer 暴露参数名（bgm、sfx）区分。
    public List<VolumePreferenceData> volumeSettings = new List<VolumePreferenceData>();
}

[Serializable]
public class EndingProgressData
{
    public string endingId;
    public string title;
    public string image;
    public int priority;
    public int firstAchievedOrder;
}

[Serializable]
public class VolumePreferenceData
{
    public string parameter;
    public float value = 1f;
}

[Serializable]
public class AchievementProgressData
{
    public string achievementId;
    public int progress;
}
