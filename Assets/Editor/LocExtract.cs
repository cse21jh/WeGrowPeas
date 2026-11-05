using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>추출할 문구 하나. Source는 문제가 생겼을 때 보여줄 에셋 이름이다.</summary>
public sealed class LocEntry
{
    public string Key;
    public string Text;
    public string Source;
    public string Note;
}

/// <summary>
/// 추출한 문구를 번역 표 CSV에 합친다. 표에 없는 키만 맨 뒤에 추가하고 기존 행은 건드리지 않는다.
/// 표에서 고친 문구와 번역을 지키기 위해서다.
/// </summary>
public static class LocExtract
{
    private const string DefaultHeader = "key,ko,en,note";

    public sealed class Result
    {
        public string Csv;
        public int Added;
        public readonly List<LocIssue> Issues = new List<LocIssue>();
    }

    /// <param name="existingCsv">현재 표 내용. 새 표면 null.</param>
    public static Result Merge(string tableName, string existingCsv, IEnumerable<LocEntry> entries)
    {
        var result = new Result();
        string csv = string.IsNullOrEmpty(existingCsv) ? DefaultHeader + "\r\n" : existingCsv;

        var table = LocTable.Parse(tableName, csv);
        if (table.HasErrors)
        {
            result.Csv = existingCsv;
            result.Issues.AddRange(table.Issues);
            return result;
        }

        var rows = table.Rows.ToDictionary(r => r.Key);
        var header = BalanceCsv.Parse(csv)[0].Select(c => c.Trim()).ToList();
        var added = new Dictionary<string, LocEntry>();
        var lines = new StringBuilder();

        foreach (var entry in entries)
        {
            if (!LocTable.IsValidKey(entry.Key))
            {
                result.Issues.Add(Error(tableName, $"키로 쓸 수 없는 이름입니다: '{entry.Key}' ({entry.Source})"));
                continue;
            }

            if (added.TryGetValue(entry.Key, out LocEntry first))
            {
                if (first.Text != entry.Text)
                    result.Issues.Add(Error(tableName, $"'{entry.Key}': {first.Source}와 {entry.Source}의 문구가 다른데 키가 같습니다."));
                continue;
            }

            if (rows.TryGetValue(entry.Key, out LocRow row))
            {
                // 표를 고친 경우가 정상이므로 알리기만 한다. 게임은 표의 문구를 쓴다.
                if (Normalize(row.Get(LocTable.SourceLanguage)) != Normalize(entry.Text))
                    result.Issues.Add(Warning(tableName, $"'{entry.Key}': 표의 원문이 {entry.Source}와 다릅니다. 표의 문구를 씁니다."));
                continue;
            }

            added.Add(entry.Key, entry);
            var cells = header.Select(column => CellFor(column, entry));
            lines.Append(BalanceCsv.ToLine(cells)).Append("\r\n");
        }

        result.Added = added.Count;
        if (result.Added == 0)
        {
            result.Csv = existingCsv;
            return result;
        }

        if (!csv.EndsWith("\n")) csv += "\r\n";
        result.Csv = csv + lines;
        return result;
    }

    private static string CellFor(string column, LocEntry entry)
    {
        if (column == "key") return entry.Key;
        if (column == LocTable.NoteColumn) return entry.Note ?? "";
        return column == LocTable.SourceLanguage ? entry.Text : "";
    }

    private static string Normalize(string text) => (text ?? "").Replace("\r\n", "\n");

    private static LocIssue Error(string table, string message)
        => new LocIssue { Level = LocIssueLevel.Error, Table = table, Message = message };

    private static LocIssue Warning(string table, string message)
        => new LocIssue { Level = LocIssueLevel.Warning, Table = table, Message = message };
}
