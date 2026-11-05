using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프리팹의 고정 한글 텍스트에 <see cref="LocalizedText"/>를 붙이고 문구를 ui.csv에 추가한다.
/// 같은 문구는 같은 키를 쓴다. 이미 연결된 텍스트는 건너뛰므로 다시 실행해도 된다.
///
/// 코드 필드가 참조하는 텍스트는 코드가 채우는 값이므로 대상에서 뺀다.
/// 놓쳐도 LocalizedText가 코드가 넣은 값을 덮어쓰지 않는다.
/// </summary>
public static class LocUiLinker
{
    private const string TablePath = "Assets/Resources/Localization/ui.csv";
    private const string Table = "ui";

    // 씬은 다루지 않는다. 시작 씬은 프리팹 수집의 출발점으로만 쓴다.
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/StartScene 3.unity",
        "Assets/Scenes/GameOverScene.unity",
        "Assets/Scenes/Garden_GrassUpdate.unity",
        "Assets/Scenes/IntroScene.unity",
        "Assets/Scenes/Tutorial.unity",
    };

    // 코드가 전부 채우거나 쓰지 않는 프리팹이다. 안의 문구는 미리보기용 임시 문구다.
    private static readonly string[] ExcludedPrefabs =
    {
        "RecallEndingContent.prefab",
        "MenuBtn_.prefab",
        "ChatMsgItem_DateSeparator.prefab",
        "ArticleBigIconDescription.prefab",
        "ArticleDescription.prefab",
        "ArticleIconDescription.prefab",
        "ArticleTitle.prefab",
        "TMI.prefab",
    };

    // 런타임에 코드가 바꾸는 임시 값이다. 번역할 문구가 아니다.
    private static readonly string[] ExcludedTexts =
    {
        "스테이지 1", "4개", "4회", "여름", "B 등급", "자연사 저항", "정부", "추가 스킵 0회\nD", "홍수가 덮쳐옵니다.....",
    };

    private static readonly Regex Hangul = new Regex("[가-힣]");

    public sealed class Item
    {
        public string Prefab;
        public string Path;
        public string Text;
        public bool ReferencedByCode;
    }

    public sealed class Report
    {
        public readonly List<Item> Linked = new List<Item>();
        public readonly List<Item> Skipped = new List<Item>();
        public readonly List<LocIssue> Issues = new List<LocIssue>();
        public int PrefabsChanged;
        public int RowsAdded;
    }

    /// <param name="apply">false면 아무것도 저장하지 않고 대상만 모은다.</param>
    public static Report Run(bool apply)
    {
        var report = new Report();
        string existing = File.Exists(TablePath) ? File.ReadAllText(TablePath) : null;
        var keys = ReadKeys(existing);
        var entries = new List<LocEntry>();

        foreach (string path in CollectPrefabs())
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = Process(path, root, keys, entries, report, apply);
                if (apply && changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.PrefabsChanged++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (apply && entries.Count > 0)
        {
            var merged = LocExtract.Merge(Table, existing, entries);
            report.Issues.AddRange(merged.Issues);
            report.RowsAdded = merged.Added;
            File.WriteAllText(TablePath, merged.Csv, new UTF8Encoding(true));
            AssetDatabase.Refresh();
        }
        else
        {
            report.RowsAdded = entries.Count;
        }

        return report;
    }

    private static IEnumerable<string> CollectPrefabs()
    {
        var roots = Scenes.Where(File.Exists).ToList();
        roots.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath));

        return AssetDatabase.GetDependencies(roots.ToArray(), true)
            .Where(p => p.StartsWith("Assets/") && p.EndsWith(".prefab"))
            .Where(p => !p.Contains("/Legacy/"))
            .Where(p => !ExcludedPrefabs.Contains(Path.GetFileName(p)))
            .OrderBy(p => p, StringComparer.Ordinal);
    }

    private static bool Process(string path, GameObject root, Dictionary<string, string> keys,
        List<LocEntry> entries, Report report, bool apply)
    {
        var referenced = FindReferencedTexts(root);
        bool changed = false;

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string value = text.text;
            if (string.IsNullOrEmpty(value) || !Hangul.IsMatch(value)) continue;
            if (ExcludedTexts.Contains(value)) continue;

            // 중첩 프리팹의 텍스트는 그 프리팹을 처리할 때 다룬다.
            if (PrefabUtility.IsPartOfPrefabInstance(text)) continue;
            if (text.GetComponent<LocalizedText>() != null) continue;

            var item = new Item
            {
                Prefab = Path.GetFileName(path),
                Path = HierarchyPath(text.transform, root.transform),
                Text = value,
                ReferencedByCode = referenced.Contains(text),
            };

            if (item.ReferencedByCode)
            {
                report.Skipped.Add(item);
                continue;
            }

            report.Linked.Add(item);
            if (!keys.TryGetValue(value, out string key))
            {
                key = $"{Table}.t{keys.Count + 1:0000}";
                keys[value] = key;
                entries.Add(new LocEntry { Key = key, Text = value, Source = path, Note = $"{item.Prefab}: {item.Path}" });
            }

            if (apply)
            {
                text.gameObject.AddComponent<LocalizedText>().Link(key, value);
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>스크립트 필드가 가리키는 TMP 텍스트. 코드가 값을 넣는 대상이다.</summary>
    private static HashSet<TMP_Text> FindReferencedTexts(GameObject root)
    {
        var result = new HashSet<TMP_Text>();

        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour is TMP_Text || behaviour is UnityEngine.UI.Selectable) continue;
            if (behaviour.GetType().Namespace?.StartsWith("UnityEngine") == true) continue;

            var property = new SerializedObject(behaviour).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue is TMP_Text text) result.Add(text);
            }
        }
        return result;
    }

    private static Dictionary<string, string> ReadKeys(string csv)
    {
        var keys = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(csv)) return keys;

        foreach (var row in LocTable.Parse(Table, csv).Rows)
            keys[row.Get(LocTable.SourceLanguage)] = row.Key;
        return keys;
    }

    private static string HierarchyPath(Transform transform, Transform root)
    {
        var names = new List<string>();
        for (var t = transform; t != null && t != root; t = t.parent) names.Add(t.name);
        names.Reverse();
        return string.Join("/", names);
    }
}
