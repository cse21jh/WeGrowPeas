using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeGrowPeas.UI;

/// <summary>Backs up legacy panels before replacing their existing SettingUI entry points.</summary>
public static class SettingsDialogMigration
{
    public const string BackupFolder = "Assets/Resource/UI/LegacySettingsBackup";
    private const string CanvasPath = "Assets/Resource/Prefabs/Canvas.prefab";
    private const string DialogPath = SettingsDialogBuilder.PrefabPath;

    public static void PrepareDialog()
    {
        RequireEditMode();
        string source = SettingsDialogBuilder.Folder + "/Fonts";
        string shared = SettingsDialogBuilder.SharedFontFolder + "/Cafe24Ssurround";
        if (!AssetDatabase.IsValidFolder(shared)) AssetDatabase.CreateFolder(SettingsDialogBuilder.SharedFontFolder, "Cafe24Ssurround");
        foreach (string name in new[] { "Cafe24Ssurround.ttf", "Cafe24Ssurround SDF.asset" })
            if (AssetDatabase.LoadMainAssetAtPath(source + "/" + name) != null)
            {
                string error = AssetDatabase.MoveAsset(source + "/" + name, shared + "/" + name);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
        var oldTenada = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(source + "/Tenada SDF.asset");
        var tenada = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SettingsDialogBuilder.SharedFontFolder + "/Tenada/Tenada SDF.asset");
        var root = PrefabUtility.LoadPrefabContents(DialogPath);
        try
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                if (oldTenada != null && text.font == oldTenada)
                {
                    var material = text.fontSharedMaterial;
                    bool defaultMaterial = material == oldTenada.material;
                    text.font = tenada;
                    text.fontSharedMaterial = defaultMaterial ? tenada.material : material;
                }
            var view = root.GetComponent<SettingsDialogView>();
            var data = new SerializedObject(view);
            data.FindProperty("openOnStart").boolValue = false;
            root.transform.Find("Modal").gameObject.SetActive(false);
            if (!root.GetComponentsInChildren<SettingsDialogRow>(true).Any(r => r.entry == SettingsEntry.EndlessMode))
            {
                var template = root.GetComponentsInChildren<SettingsDialogRow>(true).Single(r => r.entry == SettingsEntry.MessageAlarm);
                var content = (RectTransform)template.transform.parent;
                foreach (RectTransform row in content)
                    if (row.anchoredPosition.y < ((RectTransform)template.transform).anchoredPosition.y)
                        row.anchoredPosition += Vector2.down * 71;
                var added = UnityEngine.Object.Instantiate(template.gameObject, content).GetComponent<SettingsDialogRow>();
                added.name = "EndlessMode"; added.entry = SettingsEntry.EndlessMode;
                ((RectTransform)added.transform).anchoredPosition += Vector2.down * 71;
                added.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                added.label.text = "무한 모드 (테스트)";
                content.sizeDelta += Vector2.up * 71;
                var definition = ScriptableObject.CreateInstance<SettingsDialogDefinition>();
                var entry = new SerializedObject(definition);
                entry.FindProperty("entry").enumValueIndex = (int)SettingsEntry.EndlessMode;
                entry.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, SettingsDialogBuilder.Folder + "/Descriptions/EndlessMode.asset");
                var definitions = data.FindProperty("definitions");
                definitions.InsertArrayElementAtIndex(definitions.arraySize);
                definitions.GetArrayElementAtIndex(definitions.arraySize - 1).objectReferenceValue = definition;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, DialogPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        if (oldTenada != null)
        {
            string path = AssetDatabase.GetAssetPath(oldTenada);
            string guid = AssetDatabase.AssetPathToGUID(path);
            var remaining = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Where(p => new[] { ".prefab", ".unity", ".asset", ".mat" }.Contains(Path.GetExtension(p)))
                .Where(p => p.Replace('\\', '/') != path && File.ReadAllText(p).Contains(guid)).ToArray();
            if (remaining.Length > 0) throw new InvalidOperationException("Duplicate font still referenced by: " + string.Join(", ", remaining));
            AssetDatabase.DeleteAsset(path);
        }
        if (AssetDatabase.IsValidFolder(source) && Directory.GetFiles(source).Length == 0) AssetDatabase.DeleteAsset(source);
    }

    [MenuItem("Tools/UI/Settings Dialog/Back Up and Replace Legacy Panels")]
    public static void Migrate()
    {
        RequireEditMode();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save current scenes before migration.");
        if (!AssetDatabase.IsValidFolder(BackupFolder)) AssetDatabase.CreateFolder("Assets/Resource/UI", "LegacySettingsBackup");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var scenes = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p != SettingsDialogBuilder.ScenePath && File.ReadAllText(p).Contains("5f335ae22aafbaf4cbd5852ddaf16992")).ToArray();
        var changed = new List<string>();
        // Capture scene-specific panel overrides before changing the shared Canvas prefab.
        foreach (string path in scenes)
        {
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                foreach (var ui in Controllers(scene)) Backup(ui, path, Path.GetFileNameWithoutExtension(path) + "_" + AssetDatabase.AssetPathToGUID(path).Substring(0, 8));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        var canvas = PrefabUtility.LoadPrefabContents(CanvasPath);
        try
        {
            foreach (var ui in canvas.GetComponentsInChildren<SettingUI>(true))
            {
                Backup(ui, CanvasPath, "SharedCanvas");
                Replace(ui, canvas.transform);
            }
            PrefabUtility.SaveAsPrefabAsset(canvas, CanvasPath);
            changed.Add(CanvasPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(canvas); }
        try
        {
            foreach (string path in scenes)
            {
                // Old phone test scenes contain unrelated missing prefabs. Their shared Canvas
                // already inherits the new dialog; avoid reserializing those broken references.
                if (path.StartsWith("Assets/Scenes/TestFolder/", StringComparison.Ordinal)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var ui in Controllers(scene)) Replace(ui, null);
                EditorSceneManager.SaveScene(scene);
                changed.Add(path);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        File.WriteAllText("Builds/SettingsDialog/migrated-assets.json", Newtonsoft.Json.JsonConvert.SerializeObject(changed, Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.Refresh();
        Debug.Log("Replaced legacy settings in " + scenes.Length + " scenes and the shared Canvas prefab. Backups: " + BackupFolder);
    }

    private static IEnumerable<SettingUI> Controllers(Scene scene) => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SettingUI>(true));

    private static void Replace(SettingUI ui, Transform prefabParent)
    {
        var data = new SerializedObject(ui);
        var old = data.FindProperty("SettingPanel").objectReferenceValue as GameObject;
        var dialog = data.FindProperty("settingsDialog").objectReferenceValue as SettingsDialogView;
        if (dialog == null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(DialogPath);
            var instance = prefabParent != null ? (GameObject)PrefabUtility.InstantiatePrefab(asset, prefabParent)
                : (GameObject)PrefabUtility.InstantiatePrefab(asset, ui.gameObject.scene);
            dialog = instance.GetComponent<SettingsDialogView>();
        }
        data.FindProperty("settingsDialog").objectReferenceValue = dialog;
        foreach (string field in new[] { "SettingPanel", "showBreedPopupToggle", "playAlarmForSeenMessagesToggle", "bgmVolumeSlider", "effectVolumeSlider" })
            data.FindProperty(field).objectReferenceValue = null;
        data.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(ui)) PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
    }

    private static void Backup(SettingUI ui, string source, string name)
    {
        var panel = new SerializedObject(ui).FindProperty("SettingPanel").objectReferenceValue as GameObject;
        string path = BackupFolder + "/" + name + ".prefab";
        if (panel == null || File.Exists(path)) return;
        var bindings = new List<object>();
        foreach (var component in panel.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            var properties = new SerializedObject(component).GetIterator();
            while (properties.Next(true))
            {
                if (properties.propertyType != SerializedPropertyType.ObjectReference) continue;
                var target = properties.objectReferenceValue;
                var targetTransform = target is GameObject go ? go.transform : target is Component c ? c.transform : null;
                if (targetTransform == null || targetTransform.IsChildOf(panel.transform)) continue;
                bindings.Add(new { objectPath = AnimationUtility.CalculateTransformPath(component.transform, panel.transform),
                    component = component.GetType().AssemblyQualifiedName, property = properties.propertyPath,
                    target = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() });
            }
        }
        var copy = UnityEngine.Object.Instantiate(panel);
        try
        {
            if (PrefabUtility.IsPartOfPrefabInstance(copy)) PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            copy.name = panel.name;
            PrefabUtility.SaveAsPrefabAsset(copy, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
        File.WriteAllText(BackupFolder + "/" + name + ".bindings.json", Newtonsoft.Json.JsonConvert.SerializeObject(
            new { source, panel = AnimationUtility.CalculateTransformPath(panel.transform, null), controller = GlobalObjectId.GetGlobalObjectIdSlow(ui).ToString(), bindings }, Newtonsoft.Json.Formatting.Indented));
    }

    // Reattach references to scene controllers when restoring a backup prefab in its original scene.
    public static void RestoreExternalBindings(GameObject restoredPanel, string bindingsPath)
    {
        RequireEditMode();
        var manifest = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(bindingsPath));
        if ((string)manifest["source"] != restoredPanel.scene.path) throw new InvalidOperationException("Open the backup's original scene first.");
        foreach (var binding in manifest["bindings"])
        {
            string path = (string)binding["objectPath"];
            var transform = string.IsNullOrEmpty(path) ? restoredPanel.transform : restoredPanel.transform.Find(path);
            var component = transform.GetComponent(Type.GetType((string)binding["component"]));
            if (!GlobalObjectId.TryParse((string)binding["target"], out var id)) continue;
            var data = new SerializedObject(component);
            data.FindProperty((string)binding["property"]).objectReferenceValue = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before migrating settings.");
    }
}
