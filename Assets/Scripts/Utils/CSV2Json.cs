using System.IO;
using System.Text;
using UnityEngine;

public static class CsvToJson
{
    public static string Convert(string csvText)
    {
        var lines = csvText.Split('\n');
        if (lines.Length < 2) return "[]";

        // 解析表头
        var headers = SplitCsvLine(lines[0].Trim());

        var sb = new StringBuilder();
        sb.Append("[");

        int rowCount = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var values = SplitCsvLine(line);
            if (rowCount > 0) sb.Append(",");
            sb.Append("{");

            for (int j = 0; j < headers.Length; j++)
            {
                if (j > 0) sb.Append(",");
                string val = j < values.Length ? values[j] : "";
                sb.Append($"\"{Escape(headers[j])}\":\"{Escape(val)}\"");
            }

            sb.Append("}");
            rowCount++;
        }

        sb.Append("]");
        return sb.ToString();
    }

    // 处理带引号的 CSV 字段（含逗号的情况）
    private static string[] SplitCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        var cur = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(cur.ToString());
                cur.Clear();
            }
            else cur.Append(c);
        }
        result.Add(cur.ToString());
        return result.ToArray();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}