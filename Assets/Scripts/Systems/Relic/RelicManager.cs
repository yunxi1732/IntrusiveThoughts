using System;
using System.Collections.Generic;
using UnityEngine;

// 遗物配置管理，场景对象的显示状态由 CSV 条件决定。
public class RelicManager : MonoBehaviour
{
    public static RelicManager instance { get; private set; }

    public List<RelicData> FullRelicList = new List<RelicData>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CSV/CSV-Relic");
        if (csvFile == null)
        {
            Debug.LogError("未找到遗物配置：Resources/CSV/CSV-Relic.csv。");
            return;
        }
        ReadRelicJson(CsvToJson.Convert(csvFile.text));
    }

    public void ReadRelicJson(string jsonData)
    {
        FullRelicList.Clear();
        var parsedRows = JsonUtility.FromJson<RelicCsvRows>("{\"relics\":" + jsonData + "}");
        if (parsedRows == null || parsedRows.relics == null)
        {
            Debug.LogError("遗物 CSV 无法解析。");
            return;
        }

        var ids = new HashSet<string>();
        foreach (var row in parsedRows.relics)
        {
            if (!TryCreateRelicData(row, out var data))
                continue;
            if (!ids.Add(data.id))
            {
                Debug.LogError($"遗物 CSV 存在重复 id：{data.id}。该行不会参与显示。");
                continue;
            }
            FullRelicList.Add(data);
        }
    }

    private static bool TryCreateRelicData(RelicCsvRow row, out RelicData data)
    {
        data = null;
        if (row == null || string.IsNullOrWhiteSpace(row.id))
        {
            Debug.LogError("遗物 CSV 存在空 id，该行不会参与显示。");
            return false;
        }
        if (!TryParseConditions(row.condition, row.id, out _))
            return false;

        data = new RelicData
        {
            id = row.id.Trim(),
            desc = row.desc,
            iconString = row.iconString,
            condition = row.condition,
            icon = string.IsNullOrWhiteSpace(row.iconString)
                ? null
                : Resources.Load<Sprite>(row.iconString),
        };
        return true;
    }

    public bool IsEligible(RelicData relic)
    {
        if (relic == null) return false;
        if (!TryParseConditions(relic.condition, relic.id, out var conditions)) return false;
        foreach (var condition in conditions)
        {
            try
            {
                if (!condition.Check(GameStateAccess.Get(condition.key))) return false;
            }
            catch (ArgumentException error)
            {
                Debug.LogError($"遗物 {relic.id} 的条件无效：{error.Message}");
                return false;
            }
        }
        return true;
    }

    private static bool TryParseConditions(string text, string relicId,
        out List<DialogueCondition> conditions)
    {
        conditions = new List<DialogueCondition>();
        if (string.IsNullOrWhiteSpace(text)) return true;

        foreach (string rawPart in text.Split(';'))
        {
            string part = rawPart.Trim();
            if (part.Length == 0) continue;
            if (!TryParseCondition(part, out var condition))
            {
                Debug.LogError($"遗物 {relicId} 的 condition 无法解析：{part}。");
                return false;
            }
            try
            {
                GameStateAccess.ValidateKey(condition.key);
            }
            catch (ArgumentException error)
            {
                Debug.LogError($"遗物 {relicId} 的 condition 无效：{error.Message}");
                return false;
            }
            conditions.Add(condition);
        }
        return true;
    }

    private static bool TryParseCondition(string text, out DialogueCondition condition)
    {
        var operators = new[]
        {
            (">=", CompareOp.GreaterEqual), ("<=", CompareOp.LessEqual),
            ("==", CompareOp.Equal), ("!=", CompareOp.NotEqual),
            (">", CompareOp.Greater), ("<", CompareOp.Less), ("=", CompareOp.Equal),
        };
        foreach (var (token, op) in operators)
        {
            int index = text.IndexOf(token, StringComparison.Ordinal);
            if (index <= 0) continue;
            if (!int.TryParse(text.Substring(index + token.Length).Trim(), out int value)) break;
            string key = text.Substring(0, index).Trim();
            if (string.IsNullOrWhiteSpace(key)) break;
            condition = new DialogueCondition { key = key, op = op, value = value };
            return true;
        }
        condition = null;
        return false;
    }

    public RelicData GetRelicDataByName(string relicName)
    {
        return FullRelicList.Find(relic => relic.id == relicName);
    }

    public int GetRelicState(string relicid, string state)
    {
        switch (state)
        {
            case "collected":
                //查看遗物是否在背包中
                return Inventory.instance.HasRelic(relicid) ? 1 : 0;
            default:
                Debug.LogError($"未知的遗物状态：{state}");
                return 0;
        }
    }
}
