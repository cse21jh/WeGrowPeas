using LeTai.Common;
using UnityEditor;
using UnityEngine;

namespace LeTai.Common.Editor
{
[CustomPropertyDrawer(typeof(DrawInlineAttribute))]
public class DrawInlineDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var child         = property.Copy();
        var end           = child.GetEndProperty();
        var enterChildren = true;

        while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            position.height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(position, child, true);
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            enterChildren = false;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var child         = property.Copy();
        var end           = child.GetEndProperty();
        var enterChildren = true;
        var height        = 0f;

        while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            if (height > 0)
                height += EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(child, true);
            enterChildren = false;
        }

        return height;
    }
}
}
