using LeTai.Common;
using UnityEditor;
using UnityEngine;

namespace LeTai.Common.Editor
{
[CustomPropertyDrawer(typeof(MaxAttribute))]
public sealed class MaxDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var maxAttribute = (MaxAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);

        var wasShowingMixedValue = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

        switch (property.propertyType)
        {
        case SerializedPropertyType.Integer:
            EditorGUI.BeginChangeCheck();
            var intValue = EditorGUI.IntField(position, label, property.intValue);
            if (EditorGUI.EndChangeCheck())
                property.intValue = Mathf.Min(intValue, (int)maxAttribute.max);
            break;
        case SerializedPropertyType.Float:
            EditorGUI.BeginChangeCheck();
            var floatValue = EditorGUI.FloatField(position, label, property.floatValue);
            if (EditorGUI.EndChangeCheck())
                property.floatValue = Mathf.Min(floatValue, maxAttribute.max);
            break;
        default:
            EditorGUI.LabelField(position, label.text, "Use Max with float or int.");
            break;
        }

        EditorGUI.showMixedValue = wasShowingMixedValue;
        EditorGUI.EndProperty();
    }
}
}