using System;
using System.Collections.Generic;
using System.Linq;

public enum LocIssueLevel { Warning, Error }

/// <summary>번역 표에서 찾은 문제. 행 번호는 스프레드시트에서 보이는 행 번호다.</summary>
public sealed class LocIssue
{
    public LocIssueLevel Level;
    public string Table;
    public int Row;
    public string Message;

    public override string ToString()
    {
        string where = Row > 0 ? $"{Table}.csv {Row}행" : $"{Table}.csv";
        return $"{where}: {Message}";
    }
}

/// <summary>번역 표의 한 행. Cells는 언어 코드와 note 열을 키로 갖는다.</summary>
public sealed class LocRow
{
    public string Key;
    public int Row;
    public readonly Dictionary<string, string> Cells = new Dictionary<string, string>();

    public string Get(string column) => Cells.TryGetValue(column, out string value) ? value : "";
}

/// <summary>
/// CSV 파일 하나를 읽은 번역 표.
///
/// 형식: key,ko,en,...,note
///   - key 열과 원문(ko) 열은 필수이고, note는 번역 참고용으로 게임에서 쓰지 않는다.
///   - 그 밖의 열 제목은 언어 코드다. (en, ja, zh-Hans ...)
///   - key가 #로 시작하는 행은 구역 제목으로 보고 건너뛴다.
///
/// UnityEngine에 의존하지 않아 에디터 도구와 런타임이 같은 규칙으로 읽는다.
/// </summary>
public sealed class LocTable
{
    public const string SourceLanguage = "ko";
    public const string NoteColumn = "note";

    private const string KeyColumn = "key";
    private const char BrokenChar = '�'; // UTF-8이 아닌 파일을 읽으면 생기는 문자

    public string Name { get; private set; }
    public readonly List<string> Languages = new List<string>();
    public readonly List<LocRow> Rows = new List<LocRow>();
    public readonly List<LocIssue> Issues = new List<LocIssue>();

    public bool HasErrors => Issues.Any(i => i.Level == LocIssueLevel.Error);

    public static LocTable Parse(string name, string csv)
    {
        var table = new LocTable { Name = name };
        var records = BalanceCsv.Parse(csv);

        if (records.Count == 0)
        {
            table.AddError(0, "내용이 비어 있습니다.");
            return table;
        }

        var header = records[0];
        int keyCol = -1;
        var columns = new Dictionary<int, string>();

        for (int c = 0; c < header.Count; c++)
        {
            string title = header[c].Trim();
            if (title.Length == 0) continue;

            if (title.Equals(KeyColumn, StringComparison.OrdinalIgnoreCase)) keyCol = c;
            else if (title.Equals(NoteColumn, StringComparison.OrdinalIgnoreCase)) columns[c] = NoteColumn;
            else if (IsLanguageCode(title))
            {
                columns[c] = title;
                table.Languages.Add(title);
            }
            else table.AddWarning(1, $"알 수 없는 열 '{title}'은 무시합니다.");
        }

        if (keyCol < 0) table.AddError(1, "key 열이 없습니다.");
        if (!table.Languages.Contains(SourceLanguage)) table.AddError(1, $"원문 열 '{SourceLanguage}'이 없습니다.");
        if (table.HasErrors) return table;

        var seen = new HashSet<string>();
        for (int r = 1; r < records.Count; r++)
            table.ReadRow(records[r], r + 1, header.Count, keyCol, columns, seen);

        return table;
    }

    /// <summary>스프레드시트에서 오타를 내기 쉬운 영문 표기로 키를 제한한다.</summary>
    public static bool IsValidKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        foreach (char c in key)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                   || c == '.' || c == '_' || c == '-';
            if (!ok) return false;
        }
        return true;
    }

    private void ReadRow(List<string> record, int rowNumber, int headerCount, int keyCol,
        Dictionary<int, string> columns, HashSet<string> seen)
    {
        string key = keyCol < record.Count ? record[keyCol].Trim() : "";

        if (key.Length == 0)
        {
            if (record.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                AddWarning(rowNumber, "key가 비어 있어 건너뜁니다.");
            return;
        }
        if (key[0] == '#') return;

        if (!IsValidKey(key))
        {
            AddError(rowNumber, $"key '{key}'에 쓸 수 없는 문자가 있습니다. (영문, 숫자, . _ - 만 가능)");
            return;
        }
        if (!seen.Add(key))
        {
            AddError(rowNumber, $"key '{key}'가 중복되었습니다.");
            return;
        }

        // 따옴표 없는 쉼표가 들어가면 뒤 칸이 밀린다. 값이 헤더 범위를 넘어가면 그 신호다.
        for (int c = headerCount; c < record.Count; c++)
        {
            if (string.IsNullOrWhiteSpace(record[c])) continue;
            AddError(rowNumber, $"'{key}': 칸 수가 헤더보다 많습니다. 쉼표가 들어간 칸이 따옴표로 감싸졌는지 확인하세요.");
            return;
        }

        var row = new LocRow { Key = key, Row = rowNumber };
        foreach (var column in columns)
        {
            string value = column.Key < record.Count ? record[column.Key].Replace("\r\n", "\n") : "";
            row.Cells[column.Value] = value;

            if (value.IndexOf(BrokenChar) >= 0)
                AddError(rowNumber, $"'{key}'의 {column.Value} 칸에 깨진 글자가 있습니다. CSV를 UTF-8로 저장하세요.");
        }

        CheckRow(row);
        Rows.Add(row);
    }

    private void CheckRow(LocRow row)
    {
        string source = row.Get(SourceLanguage);
        if (source.Length == 0)
        {
            AddWarning(row.Row, $"'{row.Key}'의 원문({SourceLanguage})이 비어 있습니다.");
            return;
        }

        var expected = LocFormat.GetPlaceholderNames(source);
        foreach (string language in Languages)
        {
            string text = row.Get(language);
            if (language == SourceLanguage || text.Length == 0) continue;

            if (!expected.SetEquals(LocFormat.GetPlaceholderNames(text)))
                AddWarning(row.Row, $"'{row.Key}': {language}의 자리표시자가 원문과 다릅니다.");
        }
    }

    private static bool IsLanguageCode(string title)
    {
        // en, ja, zh-Hans, pt-BR 형식
        if (title.Length < 2) return false;
        int dash = title.IndexOf('-');
        string language = dash < 0 ? title : title.Substring(0, dash);
        if (language.Length != 2 || !language.All(c => c >= 'a' && c <= 'z')) return false;
        return dash < 0 || (dash < title.Length - 1 && title.Skip(dash + 1).All(char.IsLetterOrDigit));
    }

    private void AddError(int row, string message) => AddIssue(LocIssueLevel.Error, row, message);
    private void AddWarning(int row, string message) => AddIssue(LocIssueLevel.Warning, row, message);

    private void AddIssue(LocIssueLevel level, int row, string message)
        => Issues.Add(new LocIssue { Level = level, Table = Name, Row = row, Message = message });
}
