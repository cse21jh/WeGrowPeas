#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(MultiTargetButton))]
[CanEditMultipleObjects]
public class MultiTargetButtonEditor : ButtonEditor
{
    public override void OnInspectorGUI()
    {
        // 기존 Button 인스펙터 표시
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Multi Target 설정",
            EditorStyles.boldLabel
        );

        SerializedProperty targets =
            serializedObject.FindProperty(
                "additionalTargetGraphics"
            );

        EditorGUILayout.PropertyField(
            targets,
            true
        );

        serializedObject.ApplyModifiedProperties();
    }
}

#endif
