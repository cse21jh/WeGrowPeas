using System.Collections.Generic;

/// <summary>
/// 여러 번역 표를 하나로 합친 조회용 데이터. 표 파일은 관리 편의로 나눌 뿐, 키는 전체에서 유일해야 한다.
/// UnityEngine에 의존하지 않는다.
/// </summary>
public sealed class LocDatabase
{
    private readonly Dictionary<string, LocRow> rows = new Dictionary<string, LocRow>();
    private readonly List<string> languages = new List<string> { LocTable.SourceLanguage };

    public readonly List<LocIssue> Issues = new List<LocIssue>();

    /// <summary>번역문이 하나 이상 있는 언어 목록. 원문 언어가 항상 맨 앞이다.</summary>
    public IReadOnlyList<string> Languages => languages;

    public int Count => rows.Count;

    public void Add(LocTable table)
    {
        Issues.AddRange(table.Issues);

        foreach (var row in table.Rows)
        {
            if (rows.TryGetValue(row.Key, out LocRow existing))
            {
                Issues.Add(new LocIssue
                {
                    Level = LocIssueLevel.Error,
                    Table = table.Name,
                    Row = row.Row,
                    Message = $"key '{row.Key}'가 다른 표에 이미 있습니다. (먼저 읽은 표의 값을 씁니다, {existing.Row}행)",
                });
                continue;
            }

            rows.Add(row.Key, row);
            foreach (string language in table.Languages)
            {
                if (!languages.Contains(language) && row.Get(language).Length > 0)
                    languages.Add(language);
            }
        }
    }

    public bool Contains(string key) => !string.IsNullOrEmpty(key) && rows.ContainsKey(key);

    /// <summary>해당 언어 문장을 찾는다. 번역이 비어 있으면 원문으로 대체한다.</summary>
    public bool TryGet(string key, string language, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(key) || !rows.TryGetValue(key, out LocRow row)) return false;

        text = row.Get(language);
        if (text.Length == 0) text = row.Get(LocTable.SourceLanguage);
        return text.Length > 0;
    }
}
