using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using WeGrowPeas.UI;

[CustomEditor(typeof(SettingsDialogView))]
public sealed class SettingsDialogViewEditor : Editor
{
    private int selected;
    private bool showReferences;
    private Editor definitionEditor;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("설정 이름 · 설명 편집", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("기존 언어 시스템의 CSV 번역표를 편집합니다. 항목을 선택하고 문구를 고친 뒤 '번역표에 저장 · 적용'을 누르세요. 플레이 중에는 화면도 즉시 갱신됩니다.", MessageType.Info);
        var definitions = serializedObject.FindProperty("definitions");
        var assets = Enumerable.Range(0, definitions.arraySize)
            .Select(i => definitions.GetArrayElementAtIndex(i).objectReferenceValue as SettingsDialogDefinition)
            .Where(d => d != null).ToArray();
        if (assets.Length > 0)
        {
            selected = Mathf.Clamp(selected, 0, assets.Length - 1);
            selected = EditorGUILayout.Popup("편집할 설정", selected, assets.Select(d => d.Label + "  (" + d.Entry + ")").ToArray());
            var asset = assets[selected];
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("설명 연결 파일", asset, typeof(SettingsDialogDefinition), false);
            Editor.CreateCachedEditor(asset, null, ref definitionEditor);
            definitionEditor.OnInspectorGUI();
        }
        else EditorGUILayout.HelpBox("설명 연결 파일이 없습니다. 아래 참조에서 Definitions를 지정하세요.", MessageType.Warning);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("openOnStart"), new GUIContent("시작할 때 열기"));
        showReferences = EditorGUILayout.Foldout(showReferences, "UI 참조 · 이벤트 · 설명 파일 목록", true);
        if (showReferences) DrawPropertiesExcluding(serializedObject, "m_Script", "openOnStart");
        serializedObject.ApplyModifiedProperties();
    }

    private void OnDisable() { if (definitionEditor != null) DestroyImmediate(definitionEditor); }
}

[CustomEditor(typeof(SettingsDialogDefinition))]
public sealed class SettingsDialogDefinitionEditor : Editor
{
    private LocTable table;
    private string loadedText, error;
    private readonly Dictionary<string, Dictionary<string, string>> edits = new Dictionary<string, Dictionary<string, string>>();
    private readonly Dictionary<string, Dictionary<string, string>> originals = new Dictionary<string, Dictionary<string, string>>();
    private SettingsDialogDefinition Definition => (SettingsDialogDefinition)target;

    private void OnEnable() => Reload();
    private void Reload()
    {
        error = null; edits.Clear(); originals.Clear();
        if (!File.Exists(SettingsDialogCsvEditor.TablePath)) { error = "설정 번역표를 찾을 수 없습니다."; return; }
        loadedText = File.ReadAllText(SettingsDialogCsvEditor.TablePath);
        table = LocTable.Parse(SettingsDialogDefinition.LocPrefix, loadedText);
        foreach (string key in new[] { Definition.LabelKey, Definition.DescriptionKey })
        {
            var row = table.Rows.FirstOrDefault(r => r.Key == key);
            if (row == null) { error = "번역표에 키가 없습니다: " + key; continue; }
            edits[key] = table.Languages.ToDictionary(l => l, row.Get);
            originals[key] = new Dictionary<string, string>(edits[key]);
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("번역 키", Definition.DescriptionKey);
        if (table != null && edits.Count == 2)
        {
            foreach (string language in table.Languages)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(language == "ko" ? "한국어 (ko)" : language, EditorStyles.boldLabel);
                edits[Definition.LabelKey][language] = EditorGUILayout.TextField("이름", edits[Definition.LabelKey][language]);
                EditorGUILayout.LabelField("설명");
                edits[Definition.DescriptionKey][language] = EditorGUILayout.TextArea(
                    edits[Definition.DescriptionKey][language], EditorStyles.textArea, GUILayout.MinHeight(64));
            }
            bool changed = edits.Any(row => row.Value.Any(cell => cell.Value != originals[row.Key][cell.Key]));
            using (new EditorGUI.DisabledScope(!changed))
            {
                if (GUILayout.Button("번역표에 저장 · 적용"))
                {
                    try
                    {
                        var patches = new List<SettingsDialogCsvEditor.CellEdit>();
                        foreach (var row in edits)
                            foreach (var cell in row.Value)
                                if (cell.Value != originals[row.Key][cell.Key])
                                    patches.Add(new SettingsDialogCsvEditor.CellEdit(row.Key, cell.Key, originals[row.Key][cell.Key], cell.Value));
                        SettingsDialogCsvEditor.Save(patches);
                        Reload();
                    }
                    catch (Exception e) { error = e.Message; }
                }
            }
            if (changed) EditorGUILayout.HelpBox("아직 저장하지 않은 문구가 있습니다. 항목을 바꾸기 전에 저장하세요.", MessageType.None);
        }
        if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("다시 불러오기")) Reload();
        if (GUILayout.Button("CSV 파일 선택")) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(SettingsDialogCsvEditor.TablePath));
        EditorGUILayout.EndHorizontal();
    }
}

/// <summary>Uses the project's CSV parser/writer; only edited cells change and external edits are checked.</summary>
public static class SettingsDialogCsvEditor
{
    public const string TablePath = "Assets/Resources/Localization/settings-dialog.csv";
    public sealed class CellEdit
    {
        public readonly string Key, Language, Before, After;
        public CellEdit(string key, string language, string before, string after)
        { Key = key; Language = language; Before = before; After = after; }
    }

    public static string Patch(string csv, IEnumerable<CellEdit> edits)
    {
        var table = LocTable.Parse(SettingsDialogDefinition.LocPrefix, csv);
        if (table.HasErrors) throw new InvalidOperationException("번역표 오류를 먼저 수정하세요.");
        var records = BalanceCsv.Parse(csv);
        int keyColumn = records[0].FindIndex(c => c.Trim().Equals("key", StringComparison.OrdinalIgnoreCase));
        foreach (var edit in edits)
        {
            int column = records[0].FindIndex(c => c.Trim() == edit.Language);
            var row = records.Skip(1).FirstOrDefault(r => r.Count > keyColumn && r[keyColumn].Trim() == edit.Key);
            if (column < 0 || row == null) throw new InvalidOperationException("번역 키 또는 언어 열이 없습니다: " + edit.Key);
            while (row.Count <= column) row.Add("");
            if (row[column].Replace("\r\n", "\n") != edit.Before.Replace("\r\n", "\n"))
                throw new InvalidOperationException("같은 문구가 외부에서 변경되었습니다. '다시 불러오기' 후 편집하세요: " + edit.Key);
            if (edit.Language == LocTable.SourceLanguage && string.IsNullOrWhiteSpace(edit.After))
                throw new InvalidOperationException("한국어 원문은 비워둘 수 없습니다.");
            row[column] = edit.After ?? "";
        }
        string newline = csv.Contains("\r\n") ? "\r\n" : "\n";
        string result = string.Join(newline, records.Select(BalanceCsv.ToLine)) + newline;
        if (LocTable.Parse(SettingsDialogDefinition.LocPrefix, result).HasErrors)
            throw new InvalidOperationException("변경된 번역표 형식이 올바르지 않습니다.");
        return result;
    }

    public static void Save(IEnumerable<CellEdit> edits)
    {
        string current = File.ReadAllText(TablePath);
        string updated = Patch(current, edits);
        File.WriteAllText(TablePath, updated, new UTF8Encoding(true));
        AssetDatabase.ImportAsset(TablePath, ImportAssetOptions.ForceSynchronousImport);
        Loc.Reload();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}
