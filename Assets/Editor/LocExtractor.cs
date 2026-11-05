using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>[LocText] 필드를 프로젝트의 ScriptableObject에서 모아 번역 표에 추가한다.</summary>
public static class LocExtractor
{
    private const string TableFolder = "Assets/Resources/Localization";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public sealed class Report
    {
        public int TablesChanged;
        public int Added;
        public readonly List<LocIssue> Issues = new List<LocIssue>();
    }

    public static Report Run()
    {
        var report = new Report();

        foreach (var group in Collect())
        {
            string path = Path.Combine(TableFolder, group.Key + ".csv");
            string existing = File.Exists(path) ? File.ReadAllText(path) : null;

            var merged = LocExtract.Merge(group.Key, existing, group.Value);
            report.Issues.AddRange(merged.Issues);
            if (merged.Issues.Any(i => i.Level == LocIssueLevel.Error) || merged.Added == 0) continue;

            Directory.CreateDirectory(TableFolder);
            File.WriteAllText(path, merged.Csv, new UTF8Encoding(true));
            report.TablesChanged++;
            report.Added += merged.Added;
        }

        AssetDatabase.Refresh();
        return report;
    }

    /// <summary>표 이름별 추출 문구. 에셋 경로순이라 결과가 항상 같다.</summary>
    private static Dictionary<string, List<LocEntry>> Collect()
    {
        var tables = new Dictionary<string, List<LocEntry>>();
        var paths = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(p => p, StringComparer.Ordinal);

        foreach (string path in paths)
        {
            // 폰트처럼 큰 에셋을 읽지 않도록 타입부터 본다.
            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
            var group = type?.GetCustomAttribute<LocGroupAttribute>(true);
            if (group == null) continue;

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!tables.TryGetValue(group.Prefix, out var entries))
                tables[group.Prefix] = entries = new List<LocEntry>();

            foreach (var field in GetFields(type))
            {
                string text = field.GetValue(asset) as string;
                if (string.IsNullOrEmpty(text)) continue;

                string suffix = field.GetCustomAttribute<LocTextAttribute>().Suffix;
                entries.Add(new LocEntry { Key = LocKey.For(group.Prefix, asset.name, suffix), Text = text, Source = path });
            }
        }
        return tables;
    }

    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
        for (Type t = type; t != null && t != typeof(ScriptableObject); t = t.BaseType)
        {
            foreach (var field in t.GetFields(Flags))
            {
                if (field.FieldType == typeof(string) && field.IsDefined(typeof(LocTextAttribute), false))
                    yield return field;
            }
        }
    }
}
