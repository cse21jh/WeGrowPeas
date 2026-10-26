using UnityEditor;
using WeGrowPeas.UI.Book;

[CustomEditor(typeof(BookCloseButton))]
[CanEditMultipleObjects]
public sealed class BookCloseButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            "Normal / Highlighted Visual keep their own sizes. Hover, press and keyboard selection " +
            "show Highlighted Visual, Highlighted Page and Highlighted Book Base together. " +
            "Connect the close action in On Click.",
            MessageType.Info);
        // The standard Button editor hides subclass fields. These transitions are intentionally unused.
        DrawPropertiesExcluding(serializedObject, "m_Script", "m_Transition", "m_Colors",
            "m_SpriteState", "m_AnimationTriggers");
        serializedObject.ApplyModifiedProperties();
    }
}
