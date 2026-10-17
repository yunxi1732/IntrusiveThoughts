using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class EndingData
{
    public string 结局ID, 结局标题, condition, txt, priority, image;
    public int Priority => int.Parse(priority);
}

public static class EndingRules
{
    [Serializable] private class Rows { public EndingData[] rows; }
    private static readonly Regex Comparison = new Regex(@"^(.+?)\s*(>=|<=|==|!=|>|<|=)\s*([+-]?\d+)\s*$");
    private static readonly Regex Difference = new Regex(@"\s+-\s+");

    public static List<EndingData> Load(string csv)
    {
        var data = JsonUtility.FromJson<Rows>("{\"rows\":" + CsvToJson.Convert(csv) + "}");
        var result = new List<EndingData>();
        var ids = new HashSet<string>();
        var fallbacks = new HashSet<int>();
        foreach (var row in data.rows)
        {
            if (string.IsNullOrWhiteSpace(row.结局ID) || !ids.Add(row.结局ID))
                throw new FormatException("结局 ID 为空或重复。");
            if (!int.TryParse(row.priority, out int priority) || priority < 0 || priority > 4)
                throw new FormatException($"结局 {row.结局ID} 的 priority 必须为 0～4。");
            if (IsFallback(row.condition))
            {
                if (!fallbacks.Add(priority)) throw new FormatException($"结局分组 {priority} 有多个 else。");
            }
            else
                // 只校验语法与 key，不在加载阶段读取游戏状态。
                Matches(row.condition, key => { GameStateAccess.ValidateKey(key); return 0; }, true);
            result.Add(row);
        }
        if (!result.Exists(row => row.Priority == 4)) throw new FormatException("结局表缺少 priority 4 的结束页。");
        return result;
    }

    public static EndingData Select(List<EndingData> endings, int priority, Func<string, int> get)
    {
        EndingData fallback = null;
        EndingData selected = null;
        foreach (var row in endings)
        {
            if (row.Priority != priority) continue;
            if (IsFallback(row.condition)) fallback = row;
            else if (Matches(row.condition, get) && selected == null) selected = row;
        }
        return selected ?? fallback;
    }

    private static bool IsFallback(string condition) => string.Equals(condition?.Trim(), "else", StringComparison.OrdinalIgnoreCase);

    public static bool Matches(string condition, Func<string, int> get, bool validateOnly = false)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;
        bool result = true;
        foreach (var part in condition.Split(';', '；'))
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            var match = Comparison.Match(part.Trim());
            if (!match.Success || !int.TryParse(match.Groups[3].Value, out int right))
                throw new FormatException($"无法解析结局条件：{part}");
            string[] terms = Difference.Split(match.Groups[1].Value.Trim());
            if (terms.Length > 2) throw new FormatException($"结局条件仅支持两个属性相减：{part}");
            long left = get(terms[0].Trim());
            if (terms.Length == 2) left -= get(terms[1].Trim());
            if (validateOnly) continue;
            switch (match.Groups[2].Value)
            {
                case ">=": result &= left >= right; break;
                case "<=": result &= left <= right; break;
                case "!=": result &= left != right; break;
                case ">": result &= left > right; break;
                case "<": result &= left < right; break;
                default: result &= left == right; break;
            }
        }
        return result;
    }
}
