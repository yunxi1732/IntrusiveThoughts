using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

//对话剧情管理
//管理所有的对白和剧情信息，控制剧情播放，根据状态触发合适的剧情
//挂载在GameRoot场景中，不销毁

public class StoryManager : MonoBehaviour
{
    public static StoryManager instance { get; private set; }

    public List<DialogueLine> fullLines = new List<DialogueLine>();

    public List<DialogueSequence> sequences = new List<DialogueSequence>();

    // 仅保存 story.* 剧情标记。
    private readonly Dictionary<string, int> states = new Dictionary<string, int>();
    private readonly HashSet<string> played = new HashSet<string>();

    private DialogueSequence current;
    private int lineIndex;
    private Coroutine leaveRoutine;
    private bool processingLeave;
    private readonly List<NPCDepartureData> departures = new List<NPCDepartureData>();
    public bool IsProcessingLeave => processingLeave;

    public bool IsPlaying => current != null;

    //UI 订阅这些事件来显示对话
    public event Action<DialogueLine> OnLine;
    public event Action<DialogueSequence> OnDialogueEnd;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitSequences();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 兼容旧调用；数据路由由 GameStateAccess 负责。
    public int GetState(string key) => GameStateAccess.Get(key);
    public void SetState(string key, int value) => GameStateAccess.Set(key, value);
    public void SetNpcState(string npc, string key, int value) => GameStateAccess.Set($"npc.{npc}.{key}", value);
    public void SetPlayerState(string key, int value) => GameStateAccess.Set($"player.{key}", value);

    public int GetStoryState(string key)
    {
        if (!key.StartsWith("story.", StringComparison.Ordinal)) throw new ArgumentException("剧情状态必须使用 story. 前缀。");
        return states.TryGetValue(key, out var value) ? value : 0;
    }

    public void SetStoryState(string key, int value)
    {
        if (!key.StartsWith("story.", StringComparison.Ordinal)) throw new ArgumentException("剧情状态必须使用 story. 前缀。");
        states[key] = value;
    }

    //从 Resources 下的 DialogueSequence.csv 和 DialogueLine.csv 加载对话
    public void InitSequences()
    {
        var seqCsv = Resources.Load<TextAsset>("CSV/CSV-Sequence");
        var lineCsv = Resources.Load<TextAsset>("CSV/CSV-Line");
        if (seqCsv == null || lineCsv == null)
        {
            Debug.LogError("Resources 下缺少 DialogueSequence.csv 或 DialogueLine.csv");
            return;
        }

        fullLines.Clear();
        sequences.AddRange(DialogueParse.Parse(seqCsv.text, lineCsv.text, fullLines));
    }

    //选出当前场景下满足条件、优先级最高的对话
    public DialogueSequence Select(string npc = null, string location = null, RelicData relicData = null, string seqType = "normal")
    {
        Debug.Log("Select param: " + npc + ", " + location + ", " + (relicData != null ? relicData.id : "null") + ", " + seqType);
        return SelectAvailable(npc, location, relicData, seqType);
    }

    private DialogueSequence SelectAvailable(string npc, string location, RelicData relicData, string seqType)
    {
        DialogueSequence best = null;
        foreach (var s in sequences)
        {
            if (s.once && played.Contains(s.id)) continue;
            if (!string.IsNullOrEmpty(s.location) && s.location != location) continue;
            if (!string.IsNullOrEmpty(s.npc) && s.npc != npc) continue;
            if (!string.IsNullOrEmpty(s.seqType) && s.seqType != seqType) continue;   // 检查对话类型
            if (!s.CheckRelic(relicData?.id)) continue;
            try
            {
                if (!MeetsConditions(s)) continue;
            }
            catch (ArgumentException error)
            {
                Debug.LogWarning($"对白 {s.id} 的条件无效，跳过：{error.Message}");
                continue;
            }
            if (best == null || s.priority > best.priority) best = s;
        }
        return best;
    }

    // 扩展非对白交互时可订阅此查询；只有实际可完成且尚未完成时返回 true。
    public event Func<string, string, bool> HasAdditionalNpcInteraction;

    public bool HasNewNpcInteraction(string npc, string location)
    {
        if (IsNewNpcSequence(SelectAvailable(npc, location, null, "normal"), npc)) return true;
        if (Inventory.instance != null)
        {
            foreach (var item in Inventory.instance.inventoryItems)
            {
                if (item == null || item.relicData == null) continue;
                // 与 Bubble 的实际操作相同：先匹配遗物对白，再匹配失败反馈。
                var sequence = SelectAvailable(npc, location, item.relicData, "normal");
                if (sequence == null || sequence.lines.Count == 0)
                    sequence = SelectAvailable(npc, location, item.relicData, "relicfail");
                if (IsNewNpcSequence(sequence, npc)) return true;
            }
        }
        if (HasAdditionalNpcInteraction != null)
            foreach (Func<string, string, bool> query in HasAdditionalNpcInteraction.GetInvocationList())
                if (query(npc, location)) return true;
        return false;
    }

    private bool IsNewNpcSequence(DialogueSequence sequence, string npc)
    {
        // played 同样记录 once=false 的对白；通用场景对白不提示到每个 NPC 头上。
        return sequence != null && sequence.npc == npc && sequence.lines.Count > 0 && !played.Contains(sequence.id);
    }

    private bool MeetsConditions(DialogueSequence s)
    {
        foreach (var c in s.conditions)
        {
            if (!c.Check(GameStateAccess.Get(c.key))) return false;
        }
        return true;
    }

    //选择并开始对话，没有可用对话返回 false
    public bool TryStart(string npc = null, RelicData relicData = null, string seqType = "normal")
    {
        if (processingLeave) return false;
        //if (IsPlaying) return false;
        string location = StoryPlayer.instance.currentLocation;
        Debug.Log("Select param: " + npc + ", " + location + ", " + (relicData != null ? relicData.id : "null") + ", " + seqType);
        var seq = Select(npc, location, relicData, seqType);
        Debug.Log("Selected dialogue sequence: " + (seq != null ? seq.id : "null"));
        if (seq == null || seq.lines.Count == 0) return false;
        current = seq;
        lineIndex = 0;
        OnLine?.Invoke(current.lines[0]);
        BroadcastPlayingState();
        return true;
    }

    public bool StartSeqenceByID(string seqID)
    {
        if (processingLeave) return false;
        var seq = sequences.Find(s => s.id == seqID);
        if (seq == null || seq.lines.Count == 0) return false;
        if (seq.once && played.Contains(seq.id)) return false;
        Debug.Log("Selected dialogue sequence: " + seq.id);
        current = seq;
        lineIndex = 0;
        OnLine?.Invoke(current.lines[0]);
        BroadcastPlayingState();
        return true;
    }

    //推进到下一句，结束时触发 OnDialogueEnd
    public void NextDialogue()
    {
        if (current == null || processingLeave) return;
        var ids = ParseLeaveIds(current.lines[lineIndex].leave);
        if (ids.Count > 0)
        {
            processingLeave = true;
            leaveRoutine = StartCoroutine(LeaveAndAdvance(ids));
            return;
        }
        AdvanceLine();
    }

    private IEnumerator LeaveAndAdvance(HashSet<string> ids)
    {
        RegisterDepartures(ids);
        yield return StoryPlayer.instance.FadeNpcBodies(ids);
        processingLeave = false;
        leaveRoutine = null;
        AdvanceLine();
    }

    private void AdvanceLine()
    {
        if (current == null) return;
        lineIndex++;
        if (lineIndex < current.lines.Count)
        {
            OnLine?.Invoke(current.lines[lineIndex]);
            return;
        }
        var finished = current;
        current = null;
        played.Add(finished.id);
        foreach (var c in finished.onFinishSet) 
        {
            Debug.Log("Setting " + c.key + " to " + c.value);
            c.Apply();
        }
        FinalizeDepartures(finished.id);
        OnDialogueEnd?.Invoke(finished);
        BroadcastTutorialCompleted(finished);
        BroadcastPlayingState();
    }

    public void SkipDialogue()
    {
        if (current == null) return;
        CancelLeaveAnimation();
        // 跳过不等待动画，但当前句和剩余台词的离开结果仍需执行。
        for (int i = lineIndex; i < current.lines.Count; i++)
        {
            var ids = ParseLeaveIds(current.lines[i].leave);
            RegisterDepartures(ids);
            StoryPlayer.instance.HideNpcBodies(ids);
        }
        var finished = current;
        current = null;
        played.Add(finished.id);
        foreach (var c in finished.onFinishSet) 
        {
            Debug.Log("Setting " + c.key + " to " + c.value);
            c.Apply();
        }
        FinalizeDepartures(finished.id);
        OnDialogueEnd?.Invoke(finished);
        BroadcastTutorialCompleted(finished);
        BroadcastPlayingState();
    }

    private void BroadcastTutorialCompleted(DialogueSequence finished)
    {
        if (finished.id == "seq_tut_5_emg")
            EventManagerNP.TriggerEvent(GameEvents.TutorialCompleted);
    }

    public void ResetStory()
    {
        CancelLeaveAnimation();
        current = null;
        lineIndex = 0;
        BroadcastPlayingState();
    }

    private void BroadcastPlayingState()
    {
        // 结束回调可能启动下一段对话，广播回调执行后的实际状态。
        EventManager1P<bool>.TriggerEvent(GameEvents.DialoguePlayingChanged, IsPlaying);
    }

    public void RestoreProgress(GameData data)
    {
        ResetStory();
        states.Clear();
        played.Clear();
        departures.Clear();
        if (data == null) return;
        foreach (var entry in data.storyStates ?? new List<StateValueData>()) SetStoryState(entry.key, entry.value);
        foreach (var id in data.completedSequenceIds ?? new List<string>()) played.Add(id);
        foreach (var departure in data.npcDepartures ?? new List<NPCDepartureData>())
            if (departure != null) departures.Add(CloneDeparture(departure));
    }

    public void CaptureProgress(GameData data)
    {
        data.storyStates = new List<StateValueData>();
        foreach (var entry in states)
            data.storyStates.Add(new StateValueData { key = entry.Key, value = entry.Value });
        data.completedSequenceIds = new List<string>(played);
        data.npcDepartures = departures.ConvertAll(CloneDeparture);
    }

    private static HashSet<string> ParseLeaveIds(string text)
    {
        var ids = new HashSet<string>();
        if (string.IsNullOrWhiteSpace(text)) return ids;
        foreach (string part in text.Split(';', '；'))
            if (!string.IsNullOrWhiteSpace(part)) ids.Add(part.Trim());
        return ids;
    }

    private void RegisterDepartures(HashSet<string> ids)
    {
        foreach (string id in ids)
        {
            if (NPCManager.instance.GetNPCDataByName(id) == null)
            {
                Debug.LogWarning($"leave 引用了不存在的 NPC：{id}");
                continue;
            }
            string location = StoryPlayer.instance.currentLocation;
            departures.RemoveAll(item => item.npcId == id && item.location == location);
            var record = new NPCDepartureData { npcId = id, location = location, sequenceId = current.id };
            SnapshotAvailable(record);
            departures.Add(record);
        }
    }

    private bool IsAvailableForReturn(DialogueSequence sequence, NPCDepartureData record)
    {
        // 新剧情提示/返回资格不要求当前已经选中遗物。
        bool candidate = sequence.npc == record.npcId && sequence.location == record.location
            && sequence.seqType == "normal" && sequence.lines.Count > 0
            && sequence.id != record.sequenceId && !played.Contains(sequence.id);
        if (!candidate) return false;
        try { return MeetsConditions(sequence); }
        catch (ArgumentException error)
        {
            Debug.LogWarning($"对白 {sequence.id} 的返回条件无效：{error.Message}");
            return false;
        }
    }

    private void SnapshotAvailable(NPCDepartureData record)
    {
        foreach (var sequence in sequences)
            if (IsAvailableForReturn(sequence, record) && !record.availableSequenceIds.Contains(sequence.id))
                record.availableSequenceIds.Add(sequence.id);
    }

    private void FinalizeDepartures(string sequenceId)
    {
        // 结束状态写完后再记录基线，防止 updatenpc 立刻重生刚离开的人物。
        foreach (var record in departures)
            if (record.sequenceId == sequenceId) SnapshotAvailable(record);
    }

    public bool IsNpcAbsent(string npcId, string location)
    {
        var record = departures.Find(item => item.npcId == npcId && item.location == location);
        if (record == null) return false;
        if (current != null && current.id == record.sequenceId) return true;
        foreach (var sequence in sequences)
        {
            if (record.availableSequenceIds.Contains(sequence.id) || !IsAvailableForReturn(sequence, record)) continue;
            departures.Remove(record);
            return false;
        }
        return true;
    }

    private static NPCDepartureData CloneDeparture(NPCDepartureData record)
    {
        return new NPCDepartureData
        {
            npcId = record.npcId, location = record.location, sequenceId = record.sequenceId,
            availableSequenceIds = new List<string>(record.availableSequenceIds ?? new List<string>()),
        };
    }

    private void CancelLeaveAnimation()
    {
        if (processingLeave && current != null && StoryPlayer.instance != null)
            StoryPlayer.instance.HideNpcBodies(ParseLeaveIds(current.lines[lineIndex].leave));
        if (leaveRoutine != null) StopCoroutine(leaveRoutine);
        leaveRoutine = null;
        processingLeave = false;
    }
}
