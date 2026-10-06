using System;
using System.Collections.Generic;
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
    public DialogueSequence Select(string npc = null, string location = null, RelicData relicData = null)
    {
        DialogueSequence best = null;
        foreach (var s in sequences)
        {
            if (s.once && played.Contains(s.id)) continue;
            if (!string.IsNullOrEmpty(s.location) && s.location != location) continue;
            if (!string.IsNullOrEmpty(s.npc) && s.npc != npc) continue;
            if (!string.IsNullOrEmpty(s.relic) && s.relic != relicData?.id) continue;
            if (!MeetsConditions(s)) continue;
            if (best == null || s.priority > best.priority) best = s;
        }
        return best;
    }

    private bool MeetsConditions(DialogueSequence s)
    {
        foreach (var c in s.conditions)
            if (!c.Check(GameStateAccess.Get(c.key))) return false;
        return true;
    }

    //选择并开始对话，没有可用对话返回 false
    public bool TryStart(string npc = null, RelicData relicData = null)
    {
        //if (IsPlaying) return false;
        string location = TheaterController.instance.currentLocation.locationName;
        var seq = Select(npc, location, relicData);
        Debug.Log("Selected dialogue sequence: " + (seq != null ? seq.id : "null"));
        if (seq == null || seq.lines.Count == 0) return false;
        current = seq;
        lineIndex = 0;
        OnLine?.Invoke(current.lines[0]);
        return true;
    }

    //推进到下一句，结束时触发 OnDialogueEnd
    public void NextDialogue()
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
        foreach (var c in finished.onFinishSet) GameStateAccess.Set(c.key, c.value);
        OnDialogueEnd?.Invoke(finished);
    }

    public void ResetStory()
    {
        current = null;
        lineIndex = 0;
    }

    public void RestoreProgress(GameData data)
    {
        ResetStory();
        states.Clear();
        played.Clear();
        if (data == null) return;
        foreach (var entry in data.storyStates ?? new List<StateValueData>()) SetStoryState(entry.key, entry.value);
        foreach (var id in data.completedSequenceIds ?? new List<string>()) played.Add(id);
    }

    public void CaptureProgress(GameData data)
    {
        data.storyStates = new List<StateValueData>();
        foreach (var entry in states)
            data.storyStates.Add(new StateValueData { key = entry.Key, value = entry.Value });
        data.completedSequenceIds = new List<string>(played);
    }
}
