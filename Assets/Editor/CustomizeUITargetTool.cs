using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 여러 씬·프리팹에 흩어진 나무 UI 이미지(Btn_wood, Base_wood, Base_longWood)에
/// <see cref="CustomizeTarget"/>을 일괄로 붙여 UI 치장 대상으로 만든다.
///
/// 규칙
///   - 프리팹 에셋을 먼저 처리한다. 프리팹 인스턴스에 속한 Image는 원본 프리팹에서 처리되므로 건너뛰고,
///     인스턴스에서 스프라이트만 다르게 바꿔 둔 경우에만 그 자리(씬/상위 프리팹)에 붙인다.
///   - 씬은 <see cref="TargetScenes"/>에 적힌 씬만 처리한다.
///   - 이미 같은 부위로 붙어 있으면 건너뛴다. 여러 번 실행해도 된다(새 버튼을 만든 뒤 다시 돌리면 된다).
///   - Image가 아닌 곳에서 이 스프라이트를 참조하는 경우(버튼 상태 그림, 스크립트 필드 등)는 고치지 않고 목록만 보여 준다.
///     그런 곳은 실행 중에 원래 나무 그림을 다시 끼울 수 있으니 따로 확인해야 한다.
/// </summary>
public static class CustomizeUITargetTool
{
    private const string MenuRoot = "Tools/Customize/";

    private static readonly (string path, CustomizePart part)[] Rules =
    {
        ("Assets/Resource/Sprites/UI/phoneUI/Legacy/ReNew/Btn_wood.png", CustomizePart.UIBtnWood),
        ("Assets/Resource/Sprites/UI/phoneUI/Legacy/ReNew/Base_wood.png", CustomizePart.UIBaseWood),
        ("Assets/Resource/Sprites/UI/phoneUI/Legacy/ReNew/Base_longWood.png", CustomizePart.UIBaseLongWood),
    };

    // 처리할 씬. 여기 없는 씬(튜토리얼, 예전 시작 씬, 테스트 씬 등)은 건드리지 않는다.
    private static readonly string[] TargetScenes =
    {
        "Assets/Scenes/CustomizeScene.unity",
        "Assets/Scenes/GameOverScene.unity",
        "Assets/Scenes/Garden_GrassUpdate.unity",
        "Assets/Scenes/IntroScene.unity",
        "Assets/Scenes/StartScene 3.unity",
    };

    // 외부 에셋·백업 폴더는 건드리지 않는다.
    private static readonly string[] SkipFolders =
    {
        "Assets/TextMesh Pro", "Assets/Plugins", "Assets/Le Tai's Asset", "Assets/_Recovery",
    };

    [MenuItem(MenuRoot + "UI 스프라이트 대상 미리보기")]
    private static void PreviewMenu()
    {
        Debug.Log(Run(apply: false));
    }

    [MenuItem(MenuRoot + "UI 스프라이트 대상 연결")]
    private static void ApplyMenu()
    {
        if (!EditorUtility.DisplayDialog("UI 스프라이트 대상 연결",
                "대상 씬과 프리팹을 열어 나무 UI 이미지에 CustomizeTarget을 붙이고 저장합니다.\n계속할까요?",
                "연결", "취소")) return;
        Debug.Log(Run(apply: true));
    }

    /// <summary>실행 결과 보고서를 돌려준다. apply가 false면 아무것도 바꾸지 않는다.</summary>
    public static string Run(bool apply)
    {
        var report = new StringBuilder();
        report.AppendLine(apply ? "[CustomizeUITargetTool] 연결" : "[CustomizeUITargetTool] 미리보기 (변경 없음)");

        var map = LoadSpriteMap(report);
        if (map.Count == 0) return report.ToString();
        var guids = Rules.Select(r => AssetDatabase.AssetPathToGUID(r.path)).ToArray();

        if (apply && Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            report.AppendLine("열려 있는 씬에 저장하지 않은 변경이 있어 중단합니다. 저장하거나 되돌린 뒤 다시 실행하세요.");
            return report.ToString();
        }

        int added = 0, existing = 0, skippedInstance = 0;
        var otherRefs = new List<string>();

        // 1) 프리팹
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (SkipFolders.Any(f => path.StartsWith(f))) continue;
            if (!FileReferencesAny(path, guids)) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var r = Process(root, map, apply, path, otherRefs);
                added += r.added; existing += r.existing; skippedInstance += r.skipped;
                if (r.added > 0) report.AppendLine($"  프리팹 {path}: +{r.added}");
                if (apply && r.added > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // 2) 대상 씬
        var originalScene = SceneManager.GetActiveScene().path;
        foreach (var s in TargetScenes)
        {
            if (!File.Exists(s)) { report.AppendLine($"  씬을 찾지 못함: {s}"); continue; }
            if (!FileReferencesAny(s, guids)) continue;
            bool alreadyOpen = SceneManager.GetSceneByPath(s).isLoaded;
            var scene = apply || !alreadyOpen
                ? EditorSceneManager.OpenScene(s, apply ? OpenSceneMode.Single : OpenSceneMode.Additive)
                : SceneManager.GetSceneByPath(s);
            try
            {
                int sceneAdded = 0;
                foreach (var go in scene.GetRootGameObjects())
                {
                    var r = Process(go, map, apply, s, otherRefs);
                    sceneAdded += r.added; existing += r.existing; skippedInstance += r.skipped;
                }
                added += sceneAdded;
                if (sceneAdded > 0) report.AppendLine($"  씬 {s}: +{sceneAdded}");
                if (apply && sceneAdded > 0) EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                // 미리보기로 잠깐 연 씬만 닫는다(원래 열려 있던 씬은 그대로).
                if (!apply && !alreadyOpen && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }
        if (apply && !string.IsNullOrEmpty(originalScene) && SceneManager.GetActiveScene().path != originalScene)
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        report.AppendLine($"새로 붙임 {added}개 / 이미 붙어 있음 {existing}개 / 원본 프리팹에서 처리되어 건너뜀 {skippedInstance}개");
        if (otherRefs.Count > 0)
        {
            report.AppendLine($"Image 말고 이 스프라이트를 참조하는 곳 {otherRefs.Count}개 (자동으로 고치지 않음, 확인 필요):");
            foreach (var o in otherRefs.Distinct()) report.AppendLine("  - " + o);
        }
        return report.ToString();
    }

    private static Dictionary<Sprite, CustomizePart> LoadSpriteMap(StringBuilder report)
    {
        var map = new Dictionary<Sprite, CustomizePart>();
        foreach (var (path, part) in Rules)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length == 0) report.AppendLine($"  스프라이트를 찾지 못함: {path}");
            foreach (var sp in sprites) map[sp] = part;
        }
        return map;
    }

    private static bool FileReferencesAny(string assetPath, string[] guids)
    {
        try
        {
            var text = File.ReadAllText(assetPath);
            return guids.Any(g => !string.IsNullOrEmpty(g) && text.Contains(g));
        }
        catch { return true; } // 못 읽으면 열어서 확인한다
    }

    private static (int added, int existing, int skipped) Process(GameObject root, Dictionary<Sprite, CustomizePart> map,
        bool apply, string where, List<string> otherRefs)
    {
        int added = 0, existing = 0, skipped = 0;

        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null || !map.TryGetValue(img.sprite, out var part)) continue;

            // 원본 프리팹에서 같은 스프라이트를 쓰면 원본에서 처리된다.
            if (PrefabUtility.IsPartOfPrefabInstance(img))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(img);
                if (source != null && source.sprite == img.sprite) { skipped++; continue; }
            }

            if (HasTarget(img, part)) { existing++; continue; }

            added++;
            if (!apply) continue;

            var target = img.gameObject.AddComponent<CustomizeTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("part").intValue = (int)part;
            var images = so.FindProperty("images");
            images.arraySize = 1;
            images.GetArrayElementAtIndex(0).objectReferenceValue = img;
            so.FindProperty("logChanges").boolValue = false; // 대상이 많아 로그는 끈다
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Image.m_Sprite 말고 이 스프라이트를 들고 있는 곳(버튼 상태 그림, 스크립트 필드 등)
        foreach (var comp in root.GetComponentsInChildren<Component>(true))
        {
            if (comp == null || comp is Transform || comp is CustomizeTarget) continue;
            var so = new SerializedObject(comp);
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (!(it.objectReferenceValue is Sprite sp) || !map.ContainsKey(sp)) continue;
                if (comp is Image && it.propertyPath == "m_Sprite") continue;
                otherRefs.Add($"{where} : {GetPath(comp.transform)} [{comp.GetType().Name}.{it.propertyPath}] = {sp.name}");
            }
        }

        return (added, existing, skipped);
    }

    private static bool HasTarget(Image img, CustomizePart part)
    {
        foreach (var t in img.GetComponents<CustomizeTarget>())
        {
            var so = new SerializedObject(t);
            if (so.FindProperty("part").intValue != (int)part) continue;
            var images = so.FindProperty("images");
            for (int i = 0; i < images.arraySize; i++)
                if (images.GetArrayElementAtIndex(i).objectReferenceValue == img) return true;
        }
        return false;
    }

    private static string GetPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
