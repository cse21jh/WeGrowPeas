using UnityEditor;
using UnityEngine;
using WeGrowPeas.UI.Book;

[CustomEditor(typeof(BookLayoutController))]
public sealed class BookLayoutControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var controller = (BookLayoutController)target;
        if (!controller.TryValidate(out string problem)) EditorGUILayout.HelpBox(problem, MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying || controller.Turner == null ||
            controller.Turner.LeftPage == null || controller.Turner.RightPage == null))
        {
            if (GUILayout.Button("Add Empty Layout")) AddLayout(controller);
            for (int i = 0; i < controller.LayoutCount; i++)
            {
                int index = i;
                if (GUILayout.Button("Preview " + i + ": " + controller.Layouts[i].name))
                    Preview(controller, index);
            }
        }
    }

    private void AddLayout(BookLayoutController controller)
    {
        int index = controller.LayoutCount;
        var left = CreateRoot(controller.Turner.LeftPage, "Layout" + index, index == 0);
        var right = CreateRoot(controller.Turner.RightPage, "Layout" + index, index == 0);
        serializedObject.Update();
        var layouts = serializedObject.FindProperty("layouts");
        layouts.arraySize++;
        var entry = layouts.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("name").stringValue = "Layout " + index;
        entry.FindPropertyRelative("leftContent").objectReferenceValue = left;
        entry.FindPropertyRelative("rightContent").objectReferenceValue = right;
        entry.FindPropertyRelative("onShow.m_PersistentCalls.m_Calls").arraySize = 0;
        serializedObject.ApplyModifiedProperties();
    }

    private static RectTransform CreateRoot(RectTransform page, string name, bool active)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add book layout");
        Undo.SetTransformParent(go.transform, page, "Add book layout");
        var rect = (RectTransform)go.transform;
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(40, 45);
        rect.offsetMax = new Vector2(-40, -45);
        go.SetActive(active);
        return rect;
    }

    private static void Preview(BookLayoutController controller, int index)
    {
        if (!controller.TryValidate(out _)) return;
        for (int i = 0; i < controller.LayoutCount; i++)
        {
            var layout = controller.Layouts[i];
            foreach (var rect in new[] { layout.leftContent, layout.rightContent })
            {
                if (rect == null) continue;
                Undo.RecordObject(rect.gameObject, "Preview book layout");
                rect.gameObject.SetActive(i == index);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect.gameObject);
            }
        }
        // Preview only changes visibility. Data events and saved initial selection stay untouched.
    }
}
