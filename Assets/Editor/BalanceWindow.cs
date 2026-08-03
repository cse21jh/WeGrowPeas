using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 밸런스 표(CSV) 내보내기/불러오기 도구.
/// [Balance]가 붙은 필드만 대상으로 하며, 불러오기 전에 변경 내역(Diff)을 먼저 보여준다.
///
/// CSV 형식: 타입별로 한 블록. 첫 줄이 헤더(asset + 필드들), 이후 각 에셋이 한 줄.
/// </summary>
public class BalanceWindow : EditorWindow
{
    private const string DefaultFolder = "Balance";

    private Vector2 scroll;
    private string folderPath;
    private List<DiffEntry> pendingDiff;
    private string status = "";

    [MenuItem("Tools/Balance/Balance Window")]
    public static void Open()
    {
        var w = GetWindow<BalanceWindow>("Balance");
        w.minSize = new Vector2(520, 400);
    }

    private void OnEnable()
    {
        folderPath = EditorPrefs.GetString("Balance_Folder", Path.Combine(Directory.GetCurrentDirectory(), DefaultFolder));
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("밸런스 CSV", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "[Balance] 어트리뷰트가 붙은 필드만 표에 포함됩니다.\n" +
            "내보내기 → 스프레드시트에서 수정 → 불러오기(변경 내역 확인 후 적용)",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("폴더", GUILayout.Width(40));
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string picked = EditorUtility.OpenFolderPanel("밸런스 CSV 폴더", folderPath, "");
            if (!string.IsNullOrEmpty(picked)) folderPath = picked;
        }
        EditorGUILayout.EndHorizontal();
        EditorPrefs.SetString("Balance_Folder", folderPath);

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CSV로 내보내기", GUILayout.Height(30))) Export();
        if (GUILayout.Button("변경 내역 확인", GUILayout.Height(30))) PreviewImport();
        EditorGUILayout.EndHorizontal();

        if (pendingDiff != null && pendingDiff.Count > 0)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"변경될 항목 {pendingDiff.Count}개", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(220));
            foreach (var d in pendingDiff)
                EditorGUILayout.LabelField($"{d.assetName}.{d.fieldLabel}", $"{d.oldValue}  →  {d.newValue}");
            EditorGUILayout.EndScrollView();

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("이대로 적용", GUILayout.Height(30))) ApplyImport();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("취소")) { pendingDiff = null; status = ""; }
        }
        else if (pendingDiff != null)
        {
            EditorGUILayout.HelpBox("CSV와 현재 값이 동일합니다. 적용할 변경이 없습니다.", MessageType.None);
        }

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(status, MessageType.None);
        }
    }

    // ── 내보내기 ──────────────────────────────────────────────────────────────
    private void Export()
    {
        Directory.CreateDirectory(folderPath);
        int fileCount = 0, rowCount = 0;

        foreach (var group in CollectAssets())
        {
            var fields = BalanceSchema.GetFields(group.Key);
            var rowsField = BalanceSchema.GetRowsField(group.Key);

            // 리스트를 행으로 펼치는 타입 (예: DawnStageConfig.stages)
            if (rowsField != null)
            {
                var elemType = BalanceSchema.GetElementType(rowsField.Field);
                var elemFields = BalanceSchema.GetFields(elemType);
                if (elemFields.Count == 0) continue;

                var sb2 = new StringBuilder();
                var header2 = new List<string> { "asset", rowsField.KeyField };
                header2.AddRange(elemFields.Select(BalanceSchema.GetHeader));
                sb2.AppendLine(BalanceCsv.ToLine(header2));

                foreach (var asset in group.Value.OrderBy(a => a.name))
                {
                    int idx = 0;
                    foreach (var item in BalanceSchema.GetRowItems(asset, rowsField.Field))
                    {
                        var row = new List<string>
                        {
                            asset.name,
                            BalanceSchema.GetKeyValue(item, rowsField.KeyField, idx)
                        };
                        row.AddRange(elemFields.Select(f => BalanceSchema.ToCell(item, f)));
                        sb2.AppendLine(BalanceCsv.ToLine(row));
                        rowCount++;
                        idx++;
                    }
                }

                WriteCsv(group.Key, sb2.ToString());
                fileCount++;
                continue;
            }

            if (fields.Count == 0) continue;

            var sb = new StringBuilder();

            // 헤더: asset + "필드명(라벨)" — 영문 필드명이 있어야 한글이 깨져도 매칭된다
            var header = new List<string> { "asset" };
            header.AddRange(fields.Select(BalanceSchema.GetHeader));
            sb.AppendLine(BalanceCsv.ToLine(header));

            foreach (var asset in group.Value.OrderBy(a => a.name))
            {
                var row = new List<string> { asset.name };
                row.AddRange(fields.Select(f => BalanceSchema.ToCell(asset, f)));
                sb.AppendLine(BalanceCsv.ToLine(row));
                rowCount++;
            }

            WriteCsv(group.Key, sb.ToString());
            fileCount++;
        }

        status = $"내보내기 완료: {fileCount}개 파일, {rowCount}행\n{folderPath}";
        Debug.Log($"[Balance] {status}");
        EditorUtility.RevealInFinder(folderPath);
    }

    /// <summary>이 타입의 CSV 파일 경로. [BalanceGroup]이 있으면 카테고리 폴더 아래로.</summary>
    private string CsvPathFor(Type type)
        => Path.Combine(folderPath, BalanceSchema.GetCsvPath(type) + ".csv");

    /// <summary>카테고리 폴더를 만들고 CSV를 쓴다. (엑셀 한글 호환을 위해 BOM 포함)</summary>
    private void WriteCsv(Type type, string content)
    {
        string path = CsvPathFor(type);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content, new UTF8Encoding(true));
    }

    // ── 불러오기 (미리보기 → 적용) ────────────────────────────────────────────
    private void PreviewImport()
    {
        pendingDiff = BuildDiff(out string report);
        status = report;
    }

    private void ApplyImport()
    {
        if (pendingDiff == null) return;

        foreach (var d in pendingDiff)
        {
            Undo.RecordObject(d.asset, "Balance Import");
            BalanceSchema.FromCell(d.target ?? d.asset, d.field, d.newValue);
            EditorUtility.SetDirty(d.asset);
        }

        AssetDatabase.SaveAssets();
        status = $"적용 완료: {pendingDiff.Count}개 값 변경";
        Debug.Log($"[Balance] {status}");
        pendingDiff = null;
    }

    private List<DiffEntry> BuildDiff(out string report)
    {
        var diffs = new List<DiffEntry>();
        int filesRead = 0, missing = 0;
        var unmatchedHeaders = new List<string>();

        foreach (var group in CollectAssets())
        {
            string path = CsvPathFor(group.Key);
            if (!File.Exists(path)) continue;
            filesRead++;

            var rows = BalanceCsv.Parse(File.ReadAllText(path));
            if (rows.Count < 2) continue;

            var header = rows[0];
            var rowsField = BalanceSchema.GetRowsField(group.Key);

            // 리스트를 행으로 펼친 타입: asset, key, 값들...
            if (rowsField != null)
            {
                var elemType = BalanceSchema.GetElementType(rowsField.Field);
                var elemFields = BalanceSchema.GetFields(elemType);

                var colMap = new Dictionary<int, FieldInfo>();
                for (int c = 2; c < header.Count; c++)
                {
                    var f = BalanceSchema.MatchField(elemFields, header[c]);
                    if (f != null) colMap[c] = f;
                }

                var assetsByName = group.Value.ToDictionary(a => a.name, a => a);

                for (int r = 1; r < rows.Count; r++)
                {
                    var row = rows[r];
                    if (row.Count < 2 || string.IsNullOrWhiteSpace(row[0])) continue;

                    if (!assetsByName.TryGetValue(row[0].Trim(), out var asset)) { missing++; continue; }

                    string key = row[1].Trim();
                    object item = BalanceSchema.FindRowItem(asset, rowsField.Field, rowsField.KeyField, key);
                    if (item == null) { missing++; continue; }

                    foreach (var kv in colMap)
                    {
                        if (kv.Key >= row.Count) continue;

                        string newValue = row[kv.Key];
                        string oldValue = BalanceSchema.ToCell(item, kv.Value);
                        if (oldValue == newValue) continue;

                        diffs.Add(new DiffEntry
                        {
                            asset = asset,
                            target = item,
                            assetName = $"{asset.name}[{key}]",
                            field = kv.Value,
                            fieldLabel = BalanceSchema.GetLabel(kv.Value),
                            oldValue = oldValue,
                            newValue = newValue,
                        });
                    }
                }
                continue;
            }

            var fields = BalanceSchema.GetFields(group.Key);

            // 헤더 라벨 → 필드 매핑 (컬럼 순서가 바뀌어도 안전)
            var colToField = new Dictionary<int, FieldInfo>();
            for (int c = 1; c < header.Count; c++)
            {
                var f = BalanceSchema.MatchField(fields, header[c]);
                if (f != null) colToField[c] = f;
                else if (!string.IsNullOrWhiteSpace(header[c])) unmatchedHeaders.Add($"{group.Key.Name}: '{header[c]}'");
            }

            var byName = group.Value.ToDictionary(a => a.name, a => a);

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;

                string assetName = row[0].Trim();
                if (!byName.TryGetValue(assetName, out var asset)) { missing++; continue; }

                foreach (var kv in colToField)
                {
                    int col = kv.Key;
                    if (col >= row.Count) continue;

                    string newValue = row[col];
                    string oldValue = BalanceSchema.ToCell(asset, kv.Value);
                    if (oldValue == newValue) continue;

                    diffs.Add(new DiffEntry
                    {
                        asset = asset,
                        assetName = assetName,
                        field = kv.Value,
                        fieldLabel = BalanceSchema.GetLabel(kv.Value),
                        oldValue = oldValue,
                        newValue = newValue,
                    });
                }
            }
        }

        report = $"CSV {filesRead}개 확인, 변경 {diffs.Count}건"
               + (missing > 0 ? $"\n※ 프로젝트에 없는 항목 {missing}개는 건너뜀" : "");

        if (unmatchedHeaders.Count > 0)
        {
            report += $"\n\n⚠ 인식하지 못한 컬럼 {unmatchedHeaders.Count}개:\n"
                    + string.Join("\n", unmatchedHeaders.Take(5))
                    + (unmatchedHeaders.Count > 5 ? "\n..." : "")
                    + "\n→ 글자가 깨졌다면 CSV를 UTF-8로 저장하세요.";
        }
        return diffs;
    }

    /// <summary>[Balance] 필드를 가진 ScriptableObject를 타입별로 모은다.</summary>
    private static Dictionary<Type, List<ScriptableObject>> CollectAssets()
    {
        var map = new Dictionary<Type, List<ScriptableObject>>();

        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so == null) continue;

            Type t = so.GetType();
            if (!BalanceSchema.HasBalanceFields(t)) continue;

            // [BalanceGroup]이 붙은 상위 타입으로 묶는다 (서브클래스가 한 표에 모이도록)
            Type key = BalanceSchema.GetGroupType(t);

            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<ScriptableObject>();
            list.Add(so);
        }
        return map;
    }

    private class DiffEntry
    {
        public ScriptableObject asset;  // 더티 처리 대상(에셋)
        public object target;           // 실제 값을 쓸 대상. null이면 asset 자신
        public string assetName;
        public FieldInfo field;
        public string fieldLabel;
        public string oldValue;
        public string newValue;
    }
}
