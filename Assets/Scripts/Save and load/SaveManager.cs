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
    // 更改密码后，已有加密存档将无法读取。
    private const string SavePassword = "2610171930";
    //单例模式，跨场景不销毁
    public static SaveManager instance { get; private set; }
    public int CurrentSlotId { get; private set; }
    private bool entering;
    private GlobalProgressData globalProgress = new GlobalProgressData();
    private bool globalLoaded;
    public string LastError { get; private set; }
    public bool? RestoredBedroomView { get; private set; }
    public bool? ConsumeRestoredBedroomView()
    {
        var view = RestoredBedroomView;
        RestoredBedroomView = null;
        return view;
    }
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
            var data = SaveFileStorage.Exists(GlobalPath)
                ? JsonUtility.FromJson<GlobalProgressData>(SaveFileStorage.ReadText(GlobalPath, SavePassword))
                : new GlobalProgressData();
            if (data == null || data.version != 1) throw new InvalidDataException("全局数据格式无效。");
            data.volumeSettings = data.volumeSettings ?? new List<VolumePreferenceData>();
            data.unlockedEndingIds = data.unlockedEndingIds ?? new List<string>();
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
        foreach (var control in FindObjectsByType<VolumeSlider_UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            control.LoadSlider(GetVolumePreference(control.parameter));
    }

    private void SaveGlobalProgress()
    {
        if (!globalLoaded && !LoadGlobalProgress())
            throw new InvalidDataException("全局数据未能读取，已停止覆盖保存。");
        foreach (var control in FindObjectsByType<VolumeSlider_UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
        SaveFileStorage.WriteText(GlobalPath, JsonUtility.ToJson(globalProgress, true), SavePassword);
    }
    private string SlotPath(int slotId)
    {
        if (slotId < 1 || slotId > 4) throw new ArgumentOutOfRangeException(nameof(slotId));
        return Path.Combine(Application.persistentDataPath, "Saves", $"slot_{slotId}.json");
    }

    public bool EnterSlot(int slotId)
    {
        if (entering || (ScreenTransition.Instance != null && ScreenTransition.Instance.IsTransitioning)) return false;
        try
        {
            if (!TryReadSlot(slotId, out var data, out string error))
                throw new InvalidDataException(error);
            // 先恢复管理器数据，卧室 Start 再根据记录绑定物件。
            foreach (var npc in NPCManager.instance.FullNPCList)
            {
                npc.attributes.Clear();
                npc.tags.Clear();
            }
            foreach (var progress in data.npcs ?? new List<NPCProgressData>())
            {
                var npc = NPCManager.instance.GetNPCDataByName(progress.npcId);
                if (npc == null) throw new InvalidDataException($"存档 NPC 不存在：{progress.npcId}");
                npc.attributes = progress.attributes ?? new List<StateValueData>();
                npc.tags = progress.tags ?? new List<string>();
            }
            GameState.instance.RestoreBedroomProgress(data);
            StoryManager.instance.RestoreProgress(data);
            if (LoadGlobalProgress()) ApplyVolumePreferences();
            CurrentSlotId = slotId;
            RestoredBedroomView = data.sceneName == "BedRoomScene" ? data.showingBedroom : (bool?)null;
            StoryPlayer.instance.currentLocation = data.locationName;
            if (Settings_UI.instance != null) Settings_UI.instance.SettingsPanel.SetActive(false);
            entering = true;
            SceneManager.LoadScene(data.sceneName);
            entering = false;
            LastError = null;
            return true;
        }
        catch (Exception error)
        {
            entering = false;
            LastError = error.Message;
            Debug.LogError($"进入槽位 {slotId} 失败：{error}");
            return false;
        }
    }

    public bool StartNewGame()
    {
        if (entering || (ScreenTransition.Instance != null && ScreenTransition.Instance.IsTransitioning)) return false;
        try
        {
            foreach (var npc in NPCManager.instance.FullNPCList)
            {
                npc.attributes.Clear();
                npc.tags.Clear();
            }
            StoryManager.instance.RestoreProgress(null);
            StoryManager.instance.BeginOpening();
            if (AudioManager.instance != null) AudioManager.instance.StopMusic();
            GameState.instance.InitializeGameState();
            CurrentSlotId = 0;
            RestoredBedroomView = null;
            StoryPlayer.instance.currentLocation = "出租屋";
            if (Settings_UI.instance != null) Settings_UI.instance.SettingsPanel.SetActive(false);
            SceneManager.LoadScene("BedRoomScene");
            LastError = null;
            return true;
        }
        catch (Exception error) { LastError = error.Message; Debug.LogError(error); return false; }
    }

    public bool TryReadSlot(int slotId, out GameData data, out string error)
    {
        data = null;
        error = null;
        try
        {
            string path = SlotPath(slotId);
            if (!SaveFileStorage.Exists(path)) { error = "空槽位"; return false; }
            var loaded = JsonUtility.FromJson<GameData>(SaveFileStorage.ReadText(path, SavePassword));
            if (loaded == null || loaded.version < 1 || loaded.version > 5 || loaded.slotId != slotId || !loaded.bedroomInitialized)
                throw new InvalidDataException("存档格式或槽位不正确。");
            MigrateProgress(loaded);
            ValidateProgress(loaded);
            data = loaded;
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    public bool HasAnyValidSave()
    {
        for (int slot = 1; slot <= 4; slot++)
            if (TryReadSlot(slot, out _, out _)) return true;
        return false;
    }

    public bool CanSave(out string reason, bool completedEnding = false)
    {
        reason = null;
        string scene = SceneManager.GetActiveScene().name;
        if (completedEnding ? scene != "EndScene" : scene != "BedRoomScene" && scene != "LocationScene" && scene != "MapScene")
            reason = "当前场景不能保存。";
        else if (GameState.instance == null || !GameState.instance.bedroomInitialized || StoryManager.instance == null)
            reason = "游戏尚未初始化。";
        else if (DateController.instance != null && DateController.instance.currentDate == 0)
            reason = "无常那边流程还没走完，等会再来吧。";
        else if (StoryManager.instance.IsPlaying || StoryManager.instance.IsProcessingLeave)
            reason = "请等待对话或人物离场结束。";
        else if (ScreenTransition.Instance != null && ScreenTransition.Instance.IsTransitioning)
            reason = "请等待转场结束。";
        return reason == null;
    }

    private static void ValidateProgress(GameData data)
    {
        if (data.currentDate < 0) throw new InvalidDataException("存档日期无效。");
        if (data.sceneName != "BedRoomScene" && data.sceneName != "LocationScene" && data.sceneName != "MapScene" && data.sceneName != "EndScene")
            throw new InvalidDataException("存档场景无效。");
        if (string.IsNullOrWhiteSpace(data.locationName) || (data.sceneName == "EndScene" && !data.endingCompleted))
            throw new InvalidDataException("存档地点或通关状态无效。");
        if (data.sceneName == "LocationScene")
        {
            // 验证当前项目人物日程中配置的地点。
            if (!NPCManager.instance.Schedules.Exists(schedule => schedule.location == data.locationName))
                throw new InvalidDataException("存档地点不在人物日程配置中。");
        }
        var instances = new Dictionary<string, BedroomRelicData>();
        foreach (var relic in data.bedroomRelics ?? new List<BedroomRelicData>())
        {
            if (relic == null || string.IsNullOrWhiteSpace(relic.instanceId)
                || instances.ContainsKey(relic.instanceId)
                || RelicManager.instance.GetRelicDataByName(relic.relicId) == null)
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
        var npcIds = new HashSet<string>();
        foreach (var npc in data.npcs ?? new List<NPCProgressData>())
        {
            if (npc == null || NPCManager.instance.GetNPCDataByName(npc.npcId) == null)
                throw new InvalidDataException("存档引用了不存在的 NPC。");
            if (!npcIds.Add(npc.npcId)) throw new InvalidDataException("存档人物重复。");
            var keys = new HashSet<string>();
            foreach (var attribute in npc.attributes ?? new List<StateValueData>())
                if (attribute == null || string.IsNullOrWhiteSpace(attribute.key) || !keys.Add(attribute.key))
                    throw new InvalidDataException("人物属性为空或重复。");
        }
        foreach (var state in data.storyStates ?? new List<StateValueData>())
            if (state == null || string.IsNullOrWhiteSpace(state.key) || !state.key.StartsWith("story."))
                throw new InvalidDataException("剧情记录只能保存 story.* 状态。");
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
        data.npcs = data.npcs ?? new List<NPCProgressData>();
        if (data.version < 4)
        {
            var playerNpc = data.npcs.Find(npc => npc != null && npc.npcId == NPCManager.PlayerId);
            if (playerNpc == null)
            {
                playerNpc = new NPCProgressData { npcId = NPCManager.PlayerId };
                data.npcs.Add(playerNpc);
            }
            playerNpc.attributes = playerNpc.attributes ?? new List<StateValueData>();
            foreach (var state in data.player.attributes)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.key))
                    throw new InvalidDataException("旧存档玩家属性记录无效。");
                // 已有统一人物记录时以该记录为准，避免覆盖较新的值。
                if (!playerNpc.attributes.Exists(entry => entry != null && entry.key == state.key))
                    playerNpc.attributes.Add(new StateValueData { key = state.key, value = state.value });
            }
        }
        if (data.version < 5)
        {
            data.sceneName = "BedRoomScene";
            data.locationName = "出租屋";
            data.showingBedroom = data.currentDate == 0;
        }
        data.achievedEndingIds = data.achievedEndingIds ?? new List<string>();
        data.player = null;
        data.states = new List<StateValueData>();
        data.version = 5;
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
        if (SaveSlot_UI.instance != null) SaveSlot_UI.instance.OpenSave();
        else Debug.LogError("尚未配置 SaveSlot_UI。请先绑定存档面板。");
    }

    // 普通保存和通关保存都必须由玩家确认目标槽位。不写入全局结局解锁。
    public bool SaveToSlot(int slotId, bool completedEnding = false, List<string> endingIds = null)
    {
        if (!CanSave(out string reason, completedEnding))
        {
            LastError = reason;
            return false;
        }
        if (completedEnding && (endingIds == null || endingIds.Count == 0))
        {
            LastError = "本次没有有效的结局记录。";
            return false;
        }
        return SaveProgress(slotId, completedEnding, endingIds);
    }

    private bool SaveProgress(int slotId, bool completedEnding, List<string> endingIds)
    {
        try
        {
            var data = new GameData
            {
                slotId = slotId,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                endingCompleted = completedEnding,
                achievedEndingIds = completedEnding ? new List<string>(new HashSet<string>(endingIds)) : new List<string>(),
                sceneName = SceneManager.GetActiveScene().name,
                locationName = completedEnding ? "结局结算" : SceneManager.GetActiveScene().name == "MapScene" ? "地图" : StoryPlayer.instance.currentLocation,
                showingBedroom = Bedroom_UI.instance != null && Bedroom_UI.instance.BedroomPanel.activeSelf,
                currentDate = DateController.instance.currentDate,
                bedroomInitialized = true,
                bedroomRelics = GameState.instance.bedroomRelics,
                inventoryBubbles = Inventory.instance.GetBubbleRecords(),
            };
            StoryManager.instance.CaptureProgress(data);
            foreach (var npc in NPCManager.instance.FullNPCList)
                data.npcs.Add(new NPCProgressData { npcId = npc.id, attributes = npc.attributes, tags = npc.tags });
            ValidateProgress(data);
            string path = SlotPath(slotId);
            SaveFileStorage.WriteText(path, JsonUtility.ToJson(data, true), SavePassword);
            CurrentSlotId = slotId;
            LastError = null;
            return true;
        }
        catch (Exception error) { LastError = error.Message; Debug.LogError($"存档失败：{error.Message}"); return false; }
    }

    public bool SavePreferences()
    {
        try { SaveGlobalProgress(); return true; }
        catch (Exception error) { Debug.LogError($"设置保存失败：{error.Message}"); return false; }
    }

    //载入之前游戏进度
    public void LoadGame()
    {
        if (SaveSlot_UI.instance != null) SaveSlot_UI.instance.OpenLoad();
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
            foreach (string file in new[] { path, path + ".tmp", path + ".bak" })
                if (File.Exists(file)) File.Delete(file);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogError($"清除槽位 {slotId} 失败：{error.Message}");
            return false;
        }
    }

    public bool HasSave(int slotId) => slotId >= 1 && slotId <= 4 && SaveFileStorage.Exists(SlotPath(slotId));

    [ContextMenu("Delete saved file")]
    public void DeleteSavedFile()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Application.persistentDataPath))
                throw new InvalidOperationException("存档目录不可用。");
            string persistentRoot = Path.GetFullPath(Application.persistentDataPath);
            string saveDirectory = Path.GetFullPath(Path.Combine(persistentRoot, "Saves"));
            if (!string.Equals(Path.GetDirectoryName(saveDirectory), persistentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("存档清理路径无效。");
            if (Directory.Exists(saveDirectory)) Directory.Delete(saveDirectory, true);
            CurrentSlotId = 0;
            RestoredBedroomView = null;
            globalProgress = new GlobalProgressData();
            globalLoaded = true;
            LastError = null;
            if (SaveSlot_UI.instance != null) SaveSlot_UI.instance.ResetAfterDeletingSaves();
            ApplyVolumePreferences();
            Debug.Log("已清除全部槽位、全局存档及临时/备份文件。");
        }
        catch (Exception error) { LastError = error.Message; Debug.LogError($"清除全部存档失败：{error.Message}"); }
    }

    //检查是否存在存档
    public bool HasSave()
    {
        //实现检查存档逻辑
        return CurrentSlotId != 0 && HasSave(CurrentSlotId);
    }
}
