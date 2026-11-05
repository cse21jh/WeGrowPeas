using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 코드의 Loc.T("키")가 번역 표에 있는지, 넘기는 자리표시자가 원문과 맞는지 검사한다.
/// 키에 오타가 나면 화면에 키 문자열이 그대로 나오므로 코드 문자열을 고친 뒤 돌린다.
/// 문자열을 조합해 만드는 키는 볼 수 없어 건너뛴다.
/// </summary>
public static class LocKeyChecker
{
    private const string ScriptFolder = "Assets/Scripts";
    private const string Table = "code";

    private static readonly Regex Call = new Regex(
        @"Loc\.T\(""([^""]+)""((?:[^()]|\((?:[^()]|\([^()]*\))*\))*)\)", RegexOptions.Compiled);

    private static readonly Regex Argument = new Regex(@"\(""(\w+)"",", RegexOptions.Compiled);
    private static readonly Regex Comment = new Regex(@"^\s*//", RegexOptions.Compiled);

    public sealed class Result
    {
        public int Checked;
        public readonly List<LocIssue> Issues = new List<LocIssue>();
    }

    public static Result Run(IEnumerable<LocTable> tables)
    {
        var rows = tables.SelectMany(t => t.Rows).GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.First());
        var result = new Result();

        foreach (string path in Directory.GetFiles(ScriptFolder, "*.cs", SearchOption.AllDirectories))
        {
            string code = string.Join("\n", File.ReadAllLines(path).Where(line => !Comment.IsMatch(line)));
            foreach (Match match in Call.Matches(code))
                Check(Path.GetFileName(path), match, rows, result);
        }
        return result;
    }

    private static void Check(string file, Match match, Dictionary<string, LocRow> rows, Result result)
    {
        string key = match.Groups[1].Value;
        result.Checked++;

        if (!rows.TryGetValue(key, out LocRow row))
        {
            Add(result, $"{file}: 표에 없는 키 '{key}'");
            return;
        }

        var expected = LocFormat.GetPlaceholderNames(row.Get(LocTable.SourceLanguage));
        var actual = new HashSet<string>(Argument.Matches(match.Groups[2].Value).Cast<Match>().Select(m => m.Groups[1].Value));
        if (!expected.SetEquals(actual))
            Add(result, $"{file}: '{key}'의 자리표시자가 표와 다릅니다.");
    }

    private static void Add(Result result, string message)
        => result.Issues.Add(new LocIssue { Level = LocIssueLevel.Error, Table = Table, Message = message });
}
