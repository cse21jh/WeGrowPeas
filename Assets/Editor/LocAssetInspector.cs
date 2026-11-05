using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 문구가 번역 표에 있는 ScriptableObject의 인스펙터 아래에 표의 문구를 읽기 전용으로 보여 준다.
/// 다른 인스펙터가 있는 타입은 건드리지 않는다.
/// </summary>
[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
[CanEditMultipleObjects]
public class LocAssetInspector : Editor
{
    private const string TableFolder = "Assets/Resources/Localization";

    private string table;
    private List<LocRow> rows;

    private void OnEnable()
    {
        var group = target != null ? target.GetType().GetCustomAttribute<LocGroupAttribute>(true) : null;
        if (group == null || targets.Length > 1) return;

        table = group.Prefix;
        string path = Path.Combine(TableFolder, table + ".csv");
        if (!File.Exists(path)) return;

        string id = (target as ILocKeyed)?.LocId;
        if (string.IsNullOrEmpty(id)) id = target.name;

        string prefix = $"{table}.{id}.";
        rows = LocTable.Parse(table, File.ReadAllText(path)).Rows
            .Where(r => r.Key.StartsWith(prefix, System.StringComparison.Ordinal))
            .ToList();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (table == null) return;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"문구는 번역 표 {table}.csv에서 고칩니다. 아래는 읽기 전용입니다.", MessageType.Info);
        if (rows == null || rows.Count == 0)
        {
            EditorGUILayout.HelpBox("표에 이 에셋의 문구가 없습니다. Localization 창의 '빠진 행 추가'를 쓰세요.", MessageType.Warning);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            foreach (var row in rows)
            {
                EditorGUILayout.LabelField(row.Key, EditorStyles.boldLabel);
                EditorGUILayout.TextArea(row.Get(LocTable.SourceLanguage), EditorStyles.wordWrappedLabel);
            }
        }
    }
}
