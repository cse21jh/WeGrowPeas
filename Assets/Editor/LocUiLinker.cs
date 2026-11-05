using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 프리팹과 씬의 고정 한글 텍스트에 <see cref="LocalizedText"/>를 붙이고 문구를 ui.csv에 추가한다.
/// 같은 문구는 같은 키를 쓴다. 이미 연결된 텍스트는 건너뛰므로 다시 실행해도 된다.
///
/// 코드 필드가 참조하는 텍스트는 코드가 채우는 값이므로 대상에서 뺀다.
/// 놓쳐도 LocalizedText가 코드가 넣은 값을 덮어쓰지 않는다.
/// </summary>
public static class LocUiLinker
{
    private const string TablePath = "Assets/Resources/Localization/ui.csv";
    private const string Table = "ui";

    private static readonly string[] Scenes =
    {
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

    // MultiTargetButton처럼 색만 바꾸려고 텍스트를 참조하는 경우다. 값은 코드가 바꾸지 않는다.
    private static readonly string[] ForcedTexts =
    {
        "회상", "프로필", "도감", "업적", "치트메뉴", "모든 요소 잠그기", "모든 요소 해금하기",
        "고정 상품", "로테이션 품목", "고정 품목", "전체", "현재는 등록된 퀘스트가 없습니다.", "완두콩의 선물.pdf",
        "메인 메뉴.pdf",
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
        public int ScenesChanged;
        public int RowsAdded;
    }

    /// <param name="apply">false면 아무것도 저장하지 않고 대상만 모은다.</param>
    public static Report Run(bool apply)
    {
        var report = new Report();
        string existing = File.Exists(TablePath) ? File.ReadAllText(TablePath) : null;
        var keys = ReadKeys(existing);
        var entries = new List<LocEntry>();

        if (apply && !CanModifyScenes(report)) return report;

        foreach (string path in CollectPrefabs())
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = Process(path, new[] { root }, keys, entries, report, apply);
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

        foreach (string path in Scenes.Where(File.Exists))
            ProcessScene(path, keys, entries, report, apply);

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

    private static bool CanModifyScenes(Report report)
    {
        if (!Application.isPlaying) return true;

        report.Issues.Add(new LocIssue { Level = LocIssueLevel.Error, Table = Table, Message = "플레이 중에는 실행할 수 없습니다." });
        return false;
    }

    private static void ProcessScene(string path, Dictionary<string, string> keys, List<LocEntry> entries, Report report, bool apply)
    {
        var scene = SceneManager.GetSceneByPath(path);

        // 저장하면 사용자의 저장 안 된 작업까지 함께 저장되므로 건너뛴다.
        if (scene.isLoaded && scene.isDirty)
        {
            report.Issues.Add(new LocIssue { Level = LocIssueLevel.Warning, Table = Table, Message = $"저장하지 않은 씬이라 건너뜁니다: {path}" });
            return;
        }

        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        try
        {
            if (Process(path, scene.GetRootGameObjects(), keys, entries, report, apply))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.ScenesChanged++;
            }
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static bool Process(string path, GameObject[] roots, Dictionary<string, string> keys,
        List<LocEntry> entries, Report report, bool apply)
    {
        var referenced = FindReferencedTexts(roots);
        bool changed = false;

        foreach (var text in roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
        {
            string value = text.text;
            if (string.IsNullOrEmpty(value) || !Hangul.IsMatch(value)) continue;
            if (ExcludedTexts.Contains(value)) continue;

            // 중첩 프리팹의 텍스트는 그 프리팹을 처리할 때 다룬다. 문구를 덮어쓴 인스턴스만 여기서 연결한다.
            if (PrefabUtility.IsPartOfPrefabInstance(text) && !HasTextOverride(text)) continue;

            // 인스턴스가 문구를 덮어쓰면 물려받은 원문과 달라져 번역되지 않으므로 그 문구로 다시 연결한다.
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null && localized.SourceText == value) continue;

            var item = new Item
            {
                Prefab = Path.GetFileName(path),
                Path = HierarchyPath(text.transform),
                Text = value,
                ReferencedByCode = referenced.Contains(text),
            };

            if (item.ReferencedByCode && !ForcedTexts.Contains(value))
            {
                report.Skipped.Add(item);
                continue;
            }

            report.Linked.Add(item);
            if (!keys.TryGetValue(value, out string key))
            {
                key = NextKey(keys);
                keys[value] = key;
                entries.Add(new LocEntry { Key = key, Text = value, Source = path, Note = $"{item.Prefab}: {item.Path}" });
            }

            if (apply)
            {
                if (localized == null) localized = text.gameObject.AddComponent<LocalizedText>();
                localized.Link(key, value);
                PrefabUtility.RecordPrefabInstancePropertyModifications(localized);
                changed = true;
            }
        }
        return changed;
    }

    private static bool HasTextOverride(TMP_Text text)
    {
        var source = PrefabUtility.GetCorrespondingObjectFromSource(text);
        return source != null && source.text != text.text;
    }

    /// <summary>스크립트 필드가 가리키는 TMP 텍스트. 코드가 값을 넣는 대상이다.</summary>
    private static HashSet<TMP_Text> FindReferencedTexts(GameObject[] roots)
    {
        var result = new HashSet<TMP_Text>();

        foreach (var behaviour in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
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

        // 자리에 맞춰 줄인 전용 키(ui.menu.*)는 같은 원문이 있어도 다른 곳에 재사용하지 않는다.
        foreach (var row in LocTable.Parse(Table, csv).Rows)
            if (row.Key.StartsWith($"{Table}.t")) keys[row.Get(LocTable.SourceLanguage)] = row.Key;
        return keys;
    }

    private static string NextKey(Dictionary<string, string> keys)
    {
        int number = keys.Count + 1;
        string key;
        do key = $"{Table}.t{number++:0000}";
        while (keys.ContainsValue(key));
        return key;
    }

    private static string HierarchyPath(Transform transform)
    {
        var names = new List<string>();
        for (var t = transform; t != null; t = t.parent) names.Add(t.name);
        names.Reverse();
        return string.Join("/", names);
    }
}
