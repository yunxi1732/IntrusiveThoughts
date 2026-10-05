using System;
using System.Collections.Generic;
using UnityEngine;

//把 DialogueSequence.csv 和 DialogueLine.csv 解析为对话数据
public static class DialogueParse
{
    //CsvToJson 输出的值都是字符串，这里先按字符串读取再手动转换
    [Serializable] private class SequenceRow
    {
        public string id, location, npc, relic, priority, once, conditions, onFinishSet;
    }
    [Serializable] private class SequenceRows { public SequenceRow[] rows; }
    [Serializable] private class LineRow { public string sequenceId, character, content, relic; }
    [Serializable] private class LineRows { public LineRow[] rows; }

    //格式: "a>=1;b==2"，结束设置项写 "a=1"（此时比较符只是占位，取 value）
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
                priority = priority,
                once = !bool.TryParse(r.once, out bool once) || once,
                conditions = ParseConditions(r.conditions, r.id),
                onFinishSet = ParseConditions(r.onFinishSet, r.id),
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
                    character = r.character,
                    content = r.content.Replace("\\n", "\n"),
                    relic = r.relic
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

        foreach (var part in text.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
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
}
