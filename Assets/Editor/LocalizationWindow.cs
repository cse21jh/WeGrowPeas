using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 번역 표(CSV) 관리 도구.
/// 표의 원본은 프로젝트 안(Resources/Localization)에 있고, 스프레드시트 수정은 외부 폴더 사본으로 한다.
/// 내보내기 → 스프레드시트에서 수정 → 변경 내역 확인 → 적용. 오류가 있으면 적용할 수 없다.
/// </summary>
public class LocalizationWindow : EditorWindow
{
    private const string TableFolder = "Assets/Resources/Localization";
    private const string DefaultExportFolder = "Localization";
    private const string FolderPrefKey = "Localization_Folder";

    private Vector2 scroll;
    private string exportFolder;
    private string status = "";
    private List<LocIssue> issues = new List<LocIssue>();
    private List<PendingTable> pending;

    [MenuItem("Tools/Localization/Localization Window")]
    public static void Open()
    {
        var w = GetWindow<LocalizationWindow>("Localization");
        w.minSize = new Vector2(560, 420);
    }

    private void OnEnable()
    {
        exportFolder = EditorPrefs.GetString(FolderPrefKey, Path.Combine(Directory.GetCurrentDirectory(), DefaultExportFolder));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("번역 표 CSV", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "내보내기 → 스프레드시트에서 수정 → 변경 내역 확인 → 적용\n" +
            "스프레드시트에서는 'CSV UTF-8' 형식으로 저장하세요.",
            MessageType.Info);

        DrawFolderField();

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("표 검사", GUILayout.Height(30))) Validate();
        if (GUILayout.Button("에셋 문구 검사", GUILayout.Height(30))) CheckAssets(addMissing: false);
        if (GUILayout.Button("빠진 행 추가", GUILayout.Height(30))) CheckAssets(addMissing: true);
        if (GUILayout.Button("UI 문구 연결", GUILayout.Height(30))) LinkUiText();
        if (GUILayout.Button("코드 키 검사", GUILayout.Height(30))) CheckCodeKeys();
        if (GUILayout.Button("내보내기", GUILayout.Height(30))) Export();
        if (GUILayout.Button("변경 내역 확인", GUILayout.Height(30))) PreviewImport();
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawPending();
        DrawIssues();
        EditorGUILayout.EndScrollView();

        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);

        DrawLanguagePreview();
    }

    private void DrawFolderField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("폴더", GUILayout.Width(40));
        exportFolder = EditorGUILayout.TextField(exportFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string picked = EditorUtility.OpenFolderPanel("번역 CSV 폴더", exportFolder, "");
            if (!string.IsNullOrEmpty(picked)) exportFolder = picked;
        }
        EditorGUILayout.EndHorizontal();
        EditorPrefs.SetString(FolderPrefKey, exportFolder);
    }

    private void DrawPending()
    {
        if (pending == null) return;

        int total = pending.Sum(p => p.Changes.Count);
        if (total == 0)
        {
            EditorGUILayout.HelpBox("프로젝트의 표와 동일합니다. 적용할 변경이 없습니다.", MessageType.None);
            return;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"변경될 항목 {total}개", EditorStyles.boldLabel);

        foreach (var table in pending)
        {
            foreach (var change in table.Changes)
                EditorGUILayout.LabelField($"{table.Name} / {change.Key}", Describe(change));
        }

        bool blocked = issues.Any(i => i.Level == LocIssueLevel.Error);
        if (blocked) EditorGUILayout.HelpBox("오류를 고친 뒤 다시 확인하세요.", MessageType.Error);

        using (new EditorGUI.DisabledScope(blocked))
        {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("이대로 적용", GUILayout.Height(30))) ApplyImport();
            GUI.backgroundColor = Color.white;
        }
        if (GUILayout.Button("취소")) { pending = null; status = ""; }
    }

    private void DrawIssues()
    {
        if (issues.Count == 0) return;

        EditorGUILayout.Space(6);
        foreach (var issue in issues.OrderByDescending(i => i.Level))
        {
            var type = issue.Level == LocIssueLevel.Error ? MessageType.Error : MessageType.Warning;
            EditorGUILayout.HelpBox(issue.ToString(), type);
        }
    }

    private void DrawLanguagePreview()
    {
        if (!Application.isPlaying) return;

        var languages = Loc.AvailableLanguages.ToList();
        int current = languages.IndexOf(Loc.Language);

        EditorGUI.BeginChangeCheck();
        int picked = EditorGUILayout.Popup("언어 미리보기", current, languages.ToArray());
        if (EditorGUI.EndChangeCheck()) Loc.SetLanguage(languages[picked], save: false);
    }

    private static string Describe(LocChange change)
    {
        switch (change.Kind)
        {
            case LocChangeKind.Added: return $"추가: {change.NewValue}";
            case LocChangeKind.Removed: return $"삭제: {change.OldValue}";
            default: return $"[{change.Column}] {change.OldValue}  →  {change.NewValue}";
        }
    }

    // ── 검사 ─────────────────────────────────────────────────────────────────
    private void Validate()
    {
        pending = null;
        var tables = LoadTables(TableFolder);
        var database = BuildDatabase(tables.Values);
        issues = database.Issues;

        var sb = new StringBuilder($"표 {tables.Count}개, 키 {database.Count}개");
        foreach (string language in database.Languages)
        {
            int filled = tables.Values.Sum(t => t.Rows.Count(r => r.Get(language).Length > 0));
            sb.Append($"\n{language}: {filled}/{database.Count}");
        }
        status = sb.ToString();
    }

    // ── 에셋 문구 검사 ───────────────────────────────────────────────────────
    private void CheckAssets(bool addMissing)
    {
        pending = null;
        var report = LocAssetChecker.Run(addMissing);
        issues = report.Issues;
        status = addMissing
            ? $"검사 완료: 키 {report.Checked}개, 표 {report.TablesChanged}개에 빈 행 {report.Added}개 추가"
            : $"검사 완료: 키 {report.Checked}개, 문제 {report.Issues.Count}건";
    }

    // ── UI 문구 연결 ─────────────────────────────────────────────────────────
    private void LinkUiText()
    {
        if (!EditorUtility.DisplayDialog("UI 문구 연결",
                "프리팹과 씬의 고정 문구에 LocalizedText를 붙이고 저장합니다.\n저장하지 않은 씬은 건너뜁니다.", "진행", "취소"))
        {
            return;
        }

        pending = null;
        var report = LocUiLinker.Run(apply: true);
        issues = report.Issues;
        status = $"연결 완료: 텍스트 {report.Linked.Count}개, 표 {report.RowsAdded}행, 프리팹 {report.PrefabsChanged}개, 씬 {report.ScenesChanged}개";
    }

    // ── 코드 키 검사 ─────────────────────────────────────────────────────────
    private void CheckCodeKeys()
    {
        pending = null;
        var result = LocKeyChecker.Run(LoadTables(TableFolder).Values);
        issues = result.Issues;
        status = $"코드 키 {result.Checked}개 검사, 문제 {result.Issues.Count}건";
    }

    // ── 내보내기 ─────────────────────────────────────────────────────────────
    private void Export()
    {
        var sources = GetCsvFiles(TableFolder);
        if (sources.Length == 0)
        {
            status = $"내보낼 표가 없습니다: {TableFolder}";
            return;
        }

        Directory.CreateDirectory(exportFolder);

        // 외부 사본은 수정 중인 작업물일 수 있으므로 내용이 다르면 먼저 묻는다.
        var edited = sources
            .Select(s => (source: s, target: Path.Combine(exportFolder, Path.GetFileName(s))))
            .Where(f => File.Exists(f.target) && File.ReadAllText(f.target) != File.ReadAllText(f.source))
            .Select(f => Path.GetFileName(f.target))
            .ToList();

        if (edited.Count > 0 && !EditorUtility.DisplayDialog(
                "수정 중인 파일 덮어쓰기",
                "폴더의 파일이 프로젝트의 표와 다릅니다. 덮어쓰면 수정 내용이 사라집니다.\n\n" + string.Join("\n", edited),
                "덮어쓰기", "취소"))
        {
            return;
        }

        foreach (string source in sources)
            File.WriteAllText(Path.Combine(exportFolder, Path.GetFileName(source)), File.ReadAllText(source), new UTF8Encoding(true));

        status = $"내보내기 완료: {sources.Length}개 파일\n{exportFolder}";
        EditorUtility.RevealInFinder(exportFolder);
    }

    // ── 불러오기 (미리보기 → 적용) ───────────────────────────────────────────
    private void PreviewImport()
    {
        var current = LoadTables(TableFolder);
        var merged = new Dictionary<string, LocTable>(current);
        pending = new List<PendingTable>();

        foreach (string path in GetCsvFiles(exportFolder))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string text = File.ReadAllText(path);
            var table = LocTable.Parse(name, text);

            current.TryGetValue(name, out LocTable before);
            merged[name] = table;
            pending.Add(new PendingTable { Name = name, Text = text, Changes = LocDiff.Compare(before, table) });
        }

        issues = BuildDatabase(merged.Values).Issues;
        status = $"CSV {pending.Count}개 확인, 변경 {pending.Sum(p => p.Changes.Count)}건";
    }

    private void ApplyImport()
    {
        Directory.CreateDirectory(TableFolder);

        var changed = pending.Where(p => p.Changes.Count > 0).ToList();
        foreach (var table in changed)
            File.WriteAllText(Path.Combine(TableFolder, table.Name + ".csv"), table.Text, new UTF8Encoding(true));

        AssetDatabase.Refresh();
        if (Application.isPlaying) Loc.Reload();

        status = $"적용 완료: 표 {changed.Count}개, {changed.Sum(p => p.Changes.Count)}건";
        Debug.Log($"[Loc] {status}");
        pending = null;
    }

    // ── 공용 ─────────────────────────────────────────────────────────────────
    private static string[] GetCsvFiles(string folder)
    {
        if (!Directory.Exists(folder)) return new string[0];
        return Directory.GetFiles(folder, "*.csv").OrderBy(p => p, System.StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, LocTable> LoadTables(string folder)
    {
        var tables = new Dictionary<string, LocTable>();
        foreach (string path in GetCsvFiles(folder))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            tables[name] = LocTable.Parse(name, File.ReadAllText(path));
        }
        return tables;
    }

    private static LocDatabase BuildDatabase(IEnumerable<LocTable> tables)
    {
        var database = new LocDatabase();
        foreach (var table in tables.OrderBy(t => t.Name, System.StringComparer.Ordinal))
            database.Add(table);
        return database;
    }

    private class PendingTable
    {
        public string Name;
        public string Text;
        public List<LocChange> Changes;
    }
}
