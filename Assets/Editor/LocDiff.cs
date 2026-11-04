using System.Collections.Generic;
using System.Linq;

public enum LocChangeKind { Added, Removed, Changed }

/// <summary>번역 표 사이의 차이 한 건. Changed일 때만 Column이 채워진다.</summary>
public sealed class LocChange
{
    public LocChangeKind Kind;
    public string Key;
    public string Column;
    public string OldValue;
    public string NewValue;
}

/// <summary>프로젝트의 표와 스프레드시트에서 고친 표를 비교한다.</summary>
public static class LocDiff
{
    /// <param name="before">현재 표. 새 파일이면 null.</param>
    public static List<LocChange> Compare(LocTable before, LocTable after)
    {
        var changes = new List<LocChange>();
        var oldRows = before == null
            ? new Dictionary<string, LocRow>()
            : before.Rows.ToDictionary(r => r.Key);
        var newKeys = new HashSet<string>(after.Rows.Select(r => r.Key));

        foreach (var row in after.Rows)
        {
            if (oldRows.TryGetValue(row.Key, out LocRow old)) CompareRow(old, row, changes);
            else changes.Add(new LocChange
            {
                Kind = LocChangeKind.Added,
                Key = row.Key,
                NewValue = row.Get(LocTable.SourceLanguage),
            });
        }

        foreach (var old in oldRows.Values)
        {
            if (newKeys.Contains(old.Key)) continue;
            changes.Add(new LocChange
            {
                Kind = LocChangeKind.Removed,
                Key = old.Key,
                OldValue = old.Get(LocTable.SourceLanguage),
            });
        }

        return changes;
    }

    private static void CompareRow(LocRow old, LocRow row, List<LocChange> changes)
    {
        foreach (string column in old.Cells.Keys.Union(row.Cells.Keys))
        {
            string oldValue = old.Get(column);
            string newValue = row.Get(column);
            if (oldValue == newValue) continue;

            changes.Add(new LocChange
            {
                Kind = LocChangeKind.Changed,
                Key = row.Key,
                Column = column,
                OldValue = oldValue,
                NewValue = newValue,
            });
        }
    }
}
