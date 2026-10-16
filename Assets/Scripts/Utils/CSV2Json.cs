using System.Collections.Generic;
using System.Text;

public static class CsvToJson
{
    public static string Convert(string csvText)
    {
        if (string.IsNullOrEmpty(csvText)) return "[]";
        var rows = ReadRows(csvText);
        if (rows.Count < 2) return "[]";

        // 解析表头
        var headers = rows[0];
        for (int i = 0; i < headers.Length; i++)
            headers[i] = headers[i].Trim().TrimStart('\uFEFF');

        var sb = new StringBuilder();
        sb.Append("[");

        int rowCount = 0;
        for (int i = 1; i < rows.Count; i++)
        {
            var values = rows[i];
            if (System.Array.TrueForAll(values, string.IsNullOrWhiteSpace)) continue;
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

    // 只有引号外的换行才结束一条记录；引号内的逗号、换行和双引号属于字段内容。
    private static List<string[]> ReadRows(string text)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        bool inQuotes = false;
        var cur = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cur.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(cur.ToString());
                cur.Clear();
            }
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                fields.Add(cur.ToString());
                rows.Add(fields.ToArray());
                fields.Clear();
                cur.Clear();
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
            }
            else cur.Append(c);
        }
        if (inQuotes) throw new System.FormatException("CSV 字段的双引号未闭合。");
        if (cur.Length > 0 || fields.Count > 0)
        {
            fields.Add(cur.ToString());
            rows.Add(fields.ToArray());
        }
        return rows;
    }

    private static string Escape(string s)
    {
        var result = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': result.Append("\\\\"); break;
                case '"': result.Append("\\\""); break;
                case '\r': result.Append("\\r"); break;
                case '\n': result.Append("\\n"); break;
                case '\t': result.Append("\\t"); break;
                default:
                    if (c < 0x20) result.Append("\\u").Append(((int)c).ToString("x4"));
                    else result.Append(c);
                    break;
            }
        }
        return result.ToString();
    }
}
