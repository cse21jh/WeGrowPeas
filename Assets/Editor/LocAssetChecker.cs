using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ScriptableObject에 필요한 문구가 번역 표에 있는지, 코드가 이름으로 찾는 에셋이 있는지 검사한다.
/// 문구는 에셋에 없으므로 표에서 빠지면 게임에 키가 그대로 보인다. 새 에셋을 만들면 여기서 확인한다.
/// </summary>
public static class LocAssetChecker
{
    private const string TableFolder = "Assets/Resources/Localization";
    private const string ExcludedFolder = "/Legacy/"; // 게임에서 쓰지 않는 구버전 데이터
    private const string ExcludedAsset = "IntroCutsceneData_Sample"; // 쓰이지 않는 샘플 데이터

    public sealed class Report
    {
        public int Checked;
        public int TablesChanged;
        public int Added;
        public readonly List<LocIssue> Issues = new List<LocIssue>();
    }

    /// <param name="addMissing">빠진 키를 표에 빈 행으로 추가한다. 문구는 표에서 채운다.</param>
    public static Report Run(bool addMissing)
    {
        var report = new Report();

        foreach (var group in Collect())
        {
            string path = Path.Combine(TableFolder, group.Key + ".csv");
            string existing = File.Exists(path) ? File.ReadAllText(path) : null;
            report.Checked += group.Value.Count;

            if (addMissing)
            {
                var merged = LocExtract.Merge(group.Key, existing, group.Value);
                report.Issues.AddRange(merged.Issues);
                if (merged.Issues.Any(i => i.Level == LocIssueLevel.Error) || merged.Added == 0) continue;

                Directory.CreateDirectory(TableFolder);
                File.WriteAllText(path, merged.Csv, new UTF8Encoding(true));
                report.TablesChanged++;
                report.Added += merged.Added;
            }
            else
            {
                CheckRows(group.Key, existing, group.Value, report);
            }
        }

        CheckNamedAssets(report);
        AssetDatabase.Refresh();
        return report;
    }

    private static void CheckRows(string table, string csv, List<LocEntry> entries, Report report)
    {
        var rows = string.IsNullOrEmpty(csv)
            ? new Dictionary<string, LocRow>()
            : LocTable.Parse(table, csv).Rows.ToDictionary(r => r.Key);

        foreach (var entry in entries)
        {
            if (rows.TryGetValue(entry.Key, out LocRow row) && row.Get(LocTable.SourceLanguage).Length > 0) continue;

            report.Issues.Add(new LocIssue
            {
                Level = LocIssueLevel.Error,
                Table = table,
                Message = $"'{entry.Key}'의 문구가 없습니다. ({entry.Source})",
            });
        }
    }

    // 코드와 옛 세이브 변환 표가 에셋 이름을 문자열로 들고 있어, 이름을 바꾸면 오류 없이 값이 끊긴다.
    private static void CheckNamedAssets(Report report)
    {
        var names = new HashSet<string>(AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources" })
            .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g))));

        var expected = LegacySaveKeys.AllAssetNames().Concat(new[]
        {
            AbilityManager.DefaultUnlockedGeneralAbility,
            ShopManager.ShopContactKey,
            ShopManager.LandDeedKey,
            ShopManager.BreedingKitKey,
        });

        foreach (string name in expected.Distinct())
        {
            report.Checked++;
            if (names.Contains(name)) continue;

            report.Issues.Add(new LocIssue
            {
                Level = LocIssueLevel.Error,
                Table = "assets",
                Message = $"코드가 찾는 에셋 '{name}'이 없습니다. 에셋 이름을 바꿨다면 코드의 이름도 고치세요.",
            });
        }
    }

    /// <summary>표 이름별로 있어야 하는 키. 에셋 경로순이라 결과가 항상 같다.</summary>
    private static Dictionary<string, List<LocEntry>> Collect()
    {
        var tables = new Dictionary<string, List<LocEntry>>();
        var paths = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(p => p, StringComparer.Ordinal);

        foreach (string path in paths)
        {
            if (path.Contains(ExcludedFolder) || path.Contains(ExcludedAsset)) continue;

            // 폰트처럼 큰 에셋을 읽지 않도록 타입부터 본다.
            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
            var group = type?.GetCustomAttribute<LocGroupAttribute>(true);
            if (group == null) continue;

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!tables.TryGetValue(group.Prefix, out var entries))
                tables[group.Prefix] = entries = new List<LocEntry>();

            string id = (asset as ILocKeyed)?.LocId;
            if (string.IsNullOrEmpty(id)) id = asset.name;

            foreach (string suffix in group.Required)
                entries.Add(new LocEntry { Key = LocKey.For(group.Prefix, id, suffix), Source = path });

            if (asset is ILocListSource list)
            {
                if (list.EnsureLocIds())
                {
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                }

                foreach (string itemId in list.GetLocIds())
                    entries.Add(new LocEntry { Key = LocKey.For(group.Prefix, id, itemId), Source = path });
            }
        }
        return tables;
    }
}
