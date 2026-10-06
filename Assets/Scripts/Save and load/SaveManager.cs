using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


//存档系统
//保存当前游戏状态
//载入之前游戏进度
//清空存档
public class SaveManager : MonoBehaviour
{
    //单例模式，跨场景不销毁
    public static SaveManager instance { get; private set; }
    public int CurrentSlotId { get; private set; }
    private bool entering;
    private GlobalProgressData globalProgress = new GlobalProgressData();
    private bool globalLoaded;
    private string GlobalPath => Path.Combine(Application.persistentDataPath, "Saves", "global_progress.json");

    private void Start()
    {
        if (LoadGlobalProgress()) ApplyVolumePreferences();
    }

    public float GetVolumePreference(string parameter)
    {
        if (!globalLoaded) LoadGlobalProgress();
        var entry = globalProgress.volumeSettings.Find(item => item.parameter == parameter);
        return entry == null ? 1f : entry.value;
    }

    private bool LoadGlobalProgress()
    {
        try
        {
            var data = File.Exists(GlobalPath)
                ? JsonUtility.FromJson<GlobalProgressData>(File.ReadAllText(GlobalPath))
                : new GlobalProgressData();
            if (data == null || data.version != 1) throw new InvalidDataException("全局数据格式无效。");
            data.volumeSettings = data.volumeSettings ?? new List<VolumePreferenceData>();
            var parameters = new HashSet<string>();
            foreach (var entry in data.volumeSettings)
                if (entry == null || string.IsNullOrWhiteSpace(entry.parameter) || !parameters.Add(entry.parameter)
                    || float.IsNaN(entry.value) || float.IsInfinity(entry.value) || entry.value < 0f || entry.value > 1f)
                    throw new InvalidDataException("全局音量设置无效。");
            globalProgress = data;
            globalLoaded = true;
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError($"读取全局数据失败：{error.Message}");
            return false;
        }
    }

    private void ApplyVolumePreferences()
    {
        foreach (var control in UnityEngine.Object.FindObjectsByType<VolumeSlider_UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            control.LoadSlider(GetVolumePreference(control.parameter));
    }

    private void SaveGlobalProgress()
    {
        if (!globalLoaded && !LoadGlobalProgress())
            throw new InvalidDataException("全局数据未能读取，已停止覆盖保存。");
        foreach (var control in UnityEngine.Object.FindObjectsByType<VolumeSlider_UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (control.slider == null || string.IsNullOrWhiteSpace(control.parameter)) continue;
            var entry = globalProgress.volumeSettings.Find(item => item.parameter == control.parameter);
            if (entry == null)
            {
                entry = new VolumePreferenceData { parameter = control.parameter };
                globalProgress.volumeSettings.Add(entry);
            }
            entry.value = Mathf.Clamp01(control.slider.value);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(GlobalPath));
        string temporary = GlobalPath + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(globalProgress, true));
        if (File.Exists(GlobalPath)) File.Replace(temporary, GlobalPath, null);
        else File.Move(temporary, GlobalPath);
    }
    private string SlotPath(int slotId)
    {
        if (slotId < 1 || slotId > 4) throw new ArgumentOutOfRangeException(nameof(slotId));
        return Path.Combine(Application.persistentDataPath, "Saves", $"slot_{slotId}.json");
    }

    public bool EnterSlot(int slotId)
    {
        if (entering) return false;
        try
        {
            string path = SlotPath(slotId);
            GameData data = null;
            if (File.Exists(path))
            {
                data = JsonUtility.FromJson<GameData>(File.ReadAllText(path));
                if (data == null || (data.version != 1 && data.version != 2) || data.slotId != slotId || !data.bedroomInitialized)
                    throw new InvalidDataException("存档格式或槽位不正确。");
                MigrateProgress(data);
                ValidateProgress(data);
            }
            // 先恢复管理器数据，卧室 Start 再根据记录绑定物件。
            foreach (var npc in NPCManager.instance.FullNPCList)
            {
                npc.attributes.Clear();
                npc.tags.Clear();
            }
            if (data == null)
                GameState.instance.InitializeGameState();
            else
            {
                foreach (var progress in data.npcs ?? new List<NPCProgressData>())
                {
                    var npc = NPCManager.instance.GetNPCDataByName(progress.npcId);
                    if (npc == null) throw new InvalidDataException($"存档 NPC 不存在：{progress.npcId}");
                    npc.attributes = progress.attributes ?? new List<StateValueData>();
                    npc.tags = progress.tags ?? new List<string>();
                }
                GameState.instance.RestoreBedroomProgress(data);
                GameState.instance.player = data.player;
            }
            StoryManager.instance.RestoreProgress(data);
            if (LoadGlobalProgress()) ApplyVolumePreferences();
            CurrentSlotId = slotId;
            entering = true;
            SceneManager.LoadScene("BedRoomScene");
            entering = false;
            return true;
        }
        catch (Exception error)
        {
            entering = false;
            Debug.LogError($"进入槽位 {slotId} 失败：{error.Message}");
            return false;
        }
    }

    private static void ValidateProgress(GameData data)
    {
        if (data.currentDate < 0) throw new InvalidDataException("存档日期无效。");
        var instances = new Dictionary<string, BedroomRelicData>();
        var placements = new HashSet<string>();
        foreach (var relic in data.bedroomRelics ?? new List<BedroomRelicData>())
        {
            if (relic == null || string.IsNullOrWhiteSpace(relic.instanceId)
                || string.IsNullOrWhiteSpace(relic.placementId) || instances.ContainsKey(relic.instanceId)
                || !placements.Add(relic.placementId) || RelicManager.instance.GetRelicDataByName(relic.relicId) == null)
                throw new InvalidDataException("卧室遗物记录无效或重复。");
            instances.Add(relic.instanceId, relic);
        }
        var bubbles = new HashSet<string>();
        foreach (var bubble in data.inventoryBubbles ?? new List<BubbleData>())
        {
            if (bubble == null || string.IsNullOrWhiteSpace(bubble.sourceInstanceId)
                || !bubbles.Add(bubble.sourceInstanceId)
                || !instances.TryGetValue(bubble.sourceInstanceId, out var source)
                || !source.collected || source.relicId != bubble.relicId)
                throw new InvalidDataException("气泡与来源遗物的记录不一致。");
        }
        foreach (var npc in data.npcs ?? new List<NPCProgressData>())
            if (npc == null || NPCManager.instance.GetNPCDataByName(npc.npcId) == null)
                throw new InvalidDataException("存档引用了不存在的 NPC。");
        foreach (var state in data.storyStates ?? new List<StateValueData>())
            if (state == null || string.IsNullOrWhiteSpace(state.key) || !state.key.StartsWith("story."))
                throw new InvalidDataException("剧情记录只能保存 story.* 状态。");
        foreach (var state in data.player.attributes)
            if (state == null || string.IsNullOrWhiteSpace(state.key))
                throw new InvalidDataException("玩家属性记录无效。");
    }

    // 第一版的混合状态表拆分到对应数据，不修改原文件。
    private static void MigrateProgress(GameData data)
    {
        data.player = data.player ?? new PlayerData();
        data.player.attributes = data.player.attributes ?? new List<StateValueData>();
        data.storyStates = data.storyStates ?? new List<StateValueData>();
        if (data.version == 1)
        {
            foreach (var state in data.states ?? new List<StateValueData>())
            {
                if (state == null) throw new InvalidDataException("旧存档含空状态。");
                GameStateAccess.ValidateKey(state.key);
                if (state.key.StartsWith("player."))
                    data.player.SetValue(state.key.Substring(7), state.value);
                else if (state.key.StartsWith("story."))
                    data.storyStates.Add(new StateValueData { key = state.key, value = state.value });
                // 旧版日期以 currentDate 为准，NPC 原本已独立保存。
                else if (state.key != "day")
                    throw new InvalidDataException($"旧存档含不支持的状态：{state.key}");
            }
        }
        data.states = new List<StateValueData>();
        data.version = 2;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //保存当前游戏状态
    public void SaveGame()
    {
        if (CurrentSlotId == 0 || SceneManager.GetActiveScene().name != "BedRoomScene"
            || StoryManager.instance.IsPlaying || !GameState.instance.bedroomInitialized) return;
        try
        {
            var data = new GameData
            {
                slotId = CurrentSlotId,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                currentDate = DateController.instance.currentDate,
                player = GameState.instance.player,
                bedroomInitialized = true,
                bedroomRelics = GameState.instance.bedroomRelics,
                inventoryBubbles = Inventory.instance.GetBubbleRecords(),
            };
            StoryManager.instance.CaptureProgress(data);
            foreach (var npc in NPCManager.instance.FullNPCList)
                data.npcs.Add(new NPCProgressData { npcId = npc.id, attributes = npc.attributes, tags = npc.tags });
            string path = SlotPath(CurrentSlotId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
            SaveGlobalProgress();
        }
        catch (Exception error) { Debug.LogError($"存档失败：{error.Message}"); }
    }

    //载入之前游戏进度
    public void LoadGame()
    {
        if (CurrentSlotId != 0 && SceneManager.GetActiveScene().name == "BedRoomScene"
            && !StoryManager.instance.IsPlaying) EnterSlot(CurrentSlotId);
    }

    //清空存档
    public void ClearSave()
    {
        if (CurrentSlotId != 0) ClearSave(CurrentSlotId);
    }

    public bool ClearSave(int slotId)
    {
        try
        {
            string path = SlotPath(slotId);
            if (File.Exists(path)) File.Delete(path);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError($"清除槽位 {slotId} 失败：{error.Message}");
            return false;
        }
    }

    public bool HasSave(int slotId) => File.Exists(SlotPath(slotId));

    //检查是否存在存档
    public bool HasSave()
    {
        //实现检查存档逻辑
        return CurrentSlotId != 0 && HasSave(CurrentSlotId);
    }
}
