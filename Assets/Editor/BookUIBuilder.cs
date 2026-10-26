using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;
using WeGrowPeas.UI.Book;

public static class BookUIBuilder
{
    public const string PrefabPath = "Assets/Resource/Sprites/UI/Book/BookUIBase.prefab";

    [MenuItem("Tools/Book UI/Create Reusable Prefab")]
    public static void Create()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) { Selection.activeObject = existing; return; }
        if (Application.isPlaying) throw new InvalidOperationException("Create the book prefab outside Play Mode.");
        const string designedBook = "Assets/Resource/Sprites/UI/Book/BookUI.prefab";
        string source = AssetDatabase.LoadAssetAtPath<GameObject>(designedBook) != null
            ? designedBook : RecallBookBuilder.PrefabPath;
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        if (!AssetDatabase.CopyAsset(source, PrefabPath))
            throw new InvalidOperationException("Could not copy the existing book shell: " + source);
        // Only the new copy is edited. Keep both the designed BookUI and recall prefab intact.
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            root.name = "BookUIBase";
            var example = root.GetComponent<RecallBookExample>();
            if (example != null) UnityEngine.Object.DestroyImmediate(example);
            var turner = root.GetComponent<BookPageTurner>();
            if (turner == null || turner.LeftPage == null || turner.RightPage == null)
                throw new InvalidOperationException("The source needs a configured Page Turner.");
            foreach (var page in new[] { turner.LeftPage, turner.RightPage })
                while (page.childCount > 0) UnityEngine.Object.DestroyImmediate(page.GetChild(0).gameObject);
            var controller = root.AddComponent<BookLayoutController>();
            var so = new SerializedObject(controller);
            so.FindProperty("turner").objectReferenceValue = turner;
            foreach (string name in new[] { "Previous", "Next" })
            {
                var transform = root.transform.Find(name);
                var button = transform != null ? transform.GetComponent<Button>() : null;
                so.FindProperty(name.ToLowerInvariant()).objectReferenceValue = button;
                if (button == null) continue;
                var bs = new SerializedObject(button);
                bs.FindProperty("m_OnClick.m_PersistentCalls.m_Calls").arraySize = 0;
                bs.ApplyModifiedPropertiesWithoutUndo();
            }
            var layouts = so.FindProperty("layouts");
            layouts.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                var layout = layouts.GetArrayElementAtIndex(i);
                layout.FindPropertyRelative("name").stringValue = "Layout " + i;
                layout.FindPropertyRelative("leftContent").objectReferenceValue = Content(turner.LeftPage, i);
                layout.FindPropertyRelative("rightContent").objectReferenceValue = Content(turner.RightPage, i);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EnsureRootSizing(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log("[Book UI] Created reusable shell: " + PrefabPath);
    }

    public static void EnsureRootSizing(GameObject root)
    {
        var existing = root.GetComponent<BookRootScaler>();
        if (existing != null && existing.Content != null) { existing.Refresh(); return; }
        // Keep every designed child transform and component reference unchanged; only add a shared parent.
        Undo.RegisterFullObjectHierarchyUndo(root, "Enable book root resizing");
        var children = new Transform[root.transform.childCount];
        for (int i = 0; i < children.Length; i++) children[i] = root.transform.GetChild(i);
        var content = new GameObject("BookContent", typeof(RectTransform)).GetComponent<RectTransform>();
        Undo.RegisterCreatedObjectUndo(content.gameObject, "Enable book root resizing");
        content.SetParent(root.transform, false);
        content.sizeDelta = new Vector2(1180, 800);
        foreach (var child in children) child.SetParent(content, false);
        var scaler = existing != null ? existing : Undo.AddComponent<BookRootScaler>(root);
        var so = new SerializedObject(scaler);
        so.FindProperty("content").objectReferenceValue = content;
        so.ApplyModifiedProperties();
        scaler.Refresh();
    }

    private static RectTransform Content(RectTransform parent, int index)
    {
        var rect = new GameObject("Layout" + index, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(40, 45);
        rect.offsetMax = new Vector2(-40, -45);
        rect.gameObject.SetActive(index == 0);
        return rect;
    }
}
