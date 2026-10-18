using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

//把 DialogueSequence.csv 和 DialogueLine.csv 解析为对话数据
public static class DialogueParse
{
    //CsvToJson 输出的值都是字符串，这里先按字符串读取再手动转换
    [Serializable] private class SequenceRow
    {
        public string id, location, npc, relic, seqType, priority, once, conditions, onFinishSet, action;
    }
    [Serializable] private class SequenceRows { public SequenceRow[] rows; }
    [Serializable] private class LineRow { public string sequenceId, character, content, relics, tag, image, sfx, waitTime, action, leave; }
    [Serializable] private class LineRows { public LineRow[] rows; }

    // 条件与结束时的赋值/累加分开解析。
    private static readonly (string token, CompareOp op)[] Operators =
    {
        (">=", CompareOp.GreaterEqual), ("<=", CompareOp.LessEqual),
        ("==", CompareOp.Equal), ("!=", CompareOp.NotEqual),
        (">", CompareOp.Greater), ("<", CompareOp.Less), ("=", CompareOp.Equal),
    };

    //解析两张表，返回对话列表；allLines 收集全部台词
    public static List<DialogueSequence> Parse(string sequenceCsv, string lineCsv, List<DialogueLine> allLines)
    {
        var seqRows = JsonUtility.FromJson<SequenceRows>("{\"rows\":" + CsvToJson.Convert(sequenceCsv) + "}").rows;
        var lineRows = JsonUtility.FromJson<LineRows>("{\"rows\":" + CsvToJson.Convert(lineCsv) + "}").rows;
        Debug.Log(lineCsv);

        var result = new List<DialogueSequence>();
        var byId = new Dictionary<string, DialogueSequence>();
        foreach (var r in seqRows)
        {
            if (string.IsNullOrEmpty(r.id)) continue;
            int.TryParse(r.priority, out int priority);
            var seq = new DialogueSequence
            {
                id = r.id,
                location = r.location,
                npc = r.npc,
                relic = r.relic,
                seqType = string.IsNullOrEmpty(r.seqType) ? "normal" : r.seqType,   //如果 seqType 为空，则默认为 normal
                priority = priority,
                once = !bool.TryParse(r.once, out bool once) || once,
                action = r.action,
                conditions = ParseConditions(r.conditions, r.id),
                onFinishSet = ParseStateChanges(r.onFinishSet, r.id),
            };
            byId[r.id] = seq;
            result.Add(seq);
        }

        foreach (var r in lineRows)
        {
            if (!byId.TryGetValue(r.sequenceId, out var seq))
            {
                Debug.LogWarning($"台词引用了不存在的对话: {r.sequenceId}");
                continue;
            }
            var line = new DialogueLine {
                    sequenceId = r.sequenceId,
                    character = r.character,
                    content = r.content.Replace("\\n", "\n"),
                    relics = r.relics,
                    tag = r.tag?.Trim(),
                    image = r.image,
                    sfx = r.sfx?.Trim(),
                    waitTime = float.TryParse(r.waitTime, out float wt) ? wt : 0f,
                    action = r.action,
                    leave = r.leave,
                };
            seq.lines.Add(line);
            allLines?.Add(line);
        }
        return result;
    }

    public static List<DialogueCondition> ParseConditions(string text, string seqId)
    {
        var list = new List<DialogueCondition>();
        if (string.IsNullOrWhiteSpace(text)) return list;

        foreach (var part in text.Split(';', '；'))
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            if (part.Contains("+="))
            {
                Debug.LogWarning($"对话 {seqId} 的条件不能使用累加操作：{part}");
                continue;
            }
            bool parsed = false;
            foreach (var (token, op) in Operators)
            {
                int idx = part.IndexOf(token, StringComparison.Ordinal);
                if (idx <= 0) continue;
                if (int.TryParse(part.Substring(idx + token.Length).Trim(), out int value))
                {
                    list.Add(new DialogueCondition { key = part.Substring(0, idx).Trim(), op = op, value = value });
                    parsed = true;
                }
                break;
            }
            if (!parsed) Debug.LogWarning($"对话 {seqId} 的条件无法解析: {part}");
        }
        return list;
    }

    public static List<DialogueStateChange> ParseStateChanges(string text, string seqId)
    {
        var list = new List<DialogueStateChange>();
        if (string.IsNullOrWhiteSpace(text)) return list;
        foreach (var part in text.Split(';', '；'))
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            // == 保留为旧配置的赋值兼容语法；+= 使用执行时的当前值。
            var match = Regex.Match(part.Trim(), @"^([^=+<>!]+?)\s*(\+=|==|=)\s*([+-]?\d+)\s*$");
            if (!match.Success || !int.TryParse(match.Groups[3].Value, out int value))
            {
                Debug.LogWarning($"对话 {seqId} 的结束设置无法解析：{part}");
                continue;
            }
            list.Add(new DialogueStateChange
            {
                key = match.Groups[1].Value.Trim(),
                op = match.Groups[2].Value == "+=" ? StateChangeOp.Add : StateChangeOp.Set,
                value = value,
            });
        }
        return list;
    }
}
