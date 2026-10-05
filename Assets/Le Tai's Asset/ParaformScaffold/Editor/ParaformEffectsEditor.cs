// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LeTai.Paraform.Scaffold.Editor
{
public sealed class ParaformEffectsEditor
{
    [Serializable]
    sealed class EffectClipboardData
    {
        public string effectType;
        public string json;
    }

    readonly SerializedProperty formMode;
    readonly SerializedProperty effectsProperty;
    readonly ReorderableList    effects;
    readonly Action             onListChanged;
    readonly bool               showPresets;
    readonly string             header;

    public ParaformEffectsEditor(
        SerializedProperty                          effectsProperty,
        SerializedProperty                          formMode,
        Action                                      onListChanged,
        bool                                        showPresets,
        ReorderableList.AddDropdownCallbackDelegate addMenu = null,
        string                                      header  = null
    )
    {
        this.formMode        = formMode;
        this.effectsProperty = effectsProperty;
        this.onListChanged   = onListChanged;
        this.showPresets     = showPresets;
        this.header          = header ?? (showPresets ? "Effects" : "Presets");
        effects = new ReorderableList(effectsProperty.serializedObject, effectsProperty, true, true, true, false) {
            drawHeaderCallback    = rect => EditorGUI.LabelField(rect, this.header),
            elementHeightCallback = index => GetEffectHeight(effectsProperty.GetArrayElementAtIndex(index)),
            drawElementCallback   = (rect, index, active, focused) => DrawEffect(rect, index),
            onAddDropdownCallback = ShowAddMenu
        };
        if (addMenu != null)
            effects.onAddDropdownCallback = addMenu;
    }

    public void Draw()
    {
        effects.DoLayoutList();
    }

    float GetEffectHeight(SerializedProperty effect)
    {
        var lines  = showPresets ? 2 : 1;
        var height = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lines + 4;
        if (effect.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight + 4;

        AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Effect.color)));
        AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Effect.useGraphicAlpha)));
        AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Effect.useGraphicColor)));
        AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Effect.softnessType)));

        switch (effect.managedReferenceValue)
        {
        case Shadow:
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Shadow.offset)));
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Shadow.spread)));
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Shadow.quality)));
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Shadow.softness)));
            break;
        case Outline:
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Outline.innerWidth)));
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Outline.innerSoftness)));
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Outline.outerWidth)));
            AddPropertyHeight(ref height, effect.FindPropertyRelative(nameof(Outline.outerSoftness)));
            break;
        }

        return height;
    }

    void DrawEffect(Rect rect, int index)
    {
        var effect = effectsProperty.GetArrayElementAtIndex(index);
        rect.y      += 2;
        rect.height =  EditorGUIUtility.singleLineHeight;
        DrawEffectHeader(rect, effect, index);
        rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;

        if (effect.managedReferenceValue == null)
            return;

        if (showPresets)
            DrawPresets(ref rect, index, effect.managedReferenceValue.GetType());

        DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Effect.color)));
        DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Effect.useGraphicAlpha)));
        DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Effect.useGraphicColor)));
        DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Effect.softnessType)));

        switch (effect.managedReferenceValue)
        {
        case Shadow:
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Shadow.offset)));
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Shadow.spread)));
            var quality     = effect.FindPropertyRelative(nameof(Shadow.quality));
            var isFormAsset = formMode != null && !formMode.hasMultipleDifferentValues && formMode.enumValueIndex == (int)FormMode.Asset;
            if (isFormAsset)
            {
                rect.height = EditorGUI.GetPropertyHeight(quality, true);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.EnumPopup(rect, quality.displayName, ShadowQuality.Low);
                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
            }
            else
                DrawProperty(ref rect, quality);
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Shadow.softness)));
            break;
        case Outline:
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Outline.innerWidth)));
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Outline.innerSoftness)));
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Outline.outerWidth)));
            DrawProperty(ref rect, effect.FindPropertyRelative(nameof(Outline.outerSoftness)));
            break;
        }
    }

    void DrawEffectHeader(Rect rect, SerializedProperty effect, int index)
    {
        if (Event.current.type == EventType.ContextClick
         && rect.Contains(Event.current.mousePosition)
         && effect.managedReferenceValue != null)
        {
            CreateContextMenu(index).ShowAsContext();
            Event.current.Use();
        }

        EditorGUI.BeginProperty(rect, GUIContent.none, effect);
        var spacing    = EditorGUIUtility.standardVerticalSpacing;
        var toggleRect = new Rect(rect.x,                    rect.y, rect.height, rect.height);
        var typeRect   = new Rect(toggleRect.xMax + spacing, rect.y, 60,          rect.height);
        var removeRect = new Rect(rect.xMax - rect.height,   rect.y, rect.height, rect.height);
        var nameRect = new Rect(typeRect.xMax + spacing,
                                rect.y,
                                removeRect.xMin - typeRect.xMax - spacing * 2,
                                rect.height);

        var value = effect.managedReferenceValue;
        if (value == null)
            EditorGUI.LabelField(new Rect(rect.x, rect.y, removeRect.xMin - rect.x - spacing, rect.height), "Missing Effect");
        else
        {
            var enabled = effect.FindPropertyRelative(nameof(Effect.enabled));
            EditorGUI.PropertyField(toggleRect, enabled, GUIContent.none);
            EditorGUI.LabelField(typeRect, value.GetType().Name, EditorStyles.boldLabel);
            EditorGUI.PropertyField(nameRect, effect.FindPropertyRelative(nameof(Effect.name)), GUIContent.none);
        }
        EditorGUI.EndProperty();
        if (GUI.Button(removeRect, "×", EditorStyles.miniButton))
            RemoveEffect(index);
    }

    void RemoveEffect(int index)
    {
        effectsProperty.serializedObject.ApplyModifiedProperties();
        foreach (var target in effectsProperty.serializedObject.targetObjects)
        {
            var serializedTarget = new SerializedObject(target);
            var targetEffects    = serializedTarget.FindProperty(effectsProperty.propertyPath);
            var count            = targetEffects.arraySize;
            targetEffects.DeleteArrayElementAtIndex(index);
            if (targetEffects.arraySize == count)
                targetEffects.DeleteArrayElementAtIndex(index);
            serializedTarget.ApplyModifiedProperties();
        }

        effectsProperty.serializedObject.Update();
        onListChanged?.Invoke();
        GUIUtility.ExitGUI();
    }

    static void AddPropertyHeight(ref float height, SerializedProperty property)
    {
        height += EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing;
    }

    static void DrawProperty(ref Rect rect, SerializedProperty property)
    {
        rect.height = EditorGUI.GetPropertyHeight(property, true);
        EditorGUI.PropertyField(rect, property, true);
        rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
    }

    void DrawPresets(ref Rect rect, int index, Type effectType)
    {
        var presets = new List<Effect>();
        var names   = new List<string>();
        foreach (var preset in ParaformProjectSettings.instance.Presets)
        {
            if (preset?.GetType() != effectType)
                continue;

            presets.Add(preset);
            names.Add(preset.name);
        }

        var optionsRect = new Rect(rect.xMax - 30, rect.y, 30, rect.height);
        if (presets.Count > 0)
        {
            var toolbarRect = rect;
            toolbarRect.xMax = optionsRect.xMin - EditorGUIUtility.standardVerticalSpacing;
            var selected = GUI.Toolbar(toolbarRect, -1, names.ToArray());
            if (selected >= 0)
                ApplyPreset(index, presets[selected]);
        }

        if (GUI.Button(optionsRect, "..."))
            ShowContextMenu(optionsRect, index);
        rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
    }

    void ShowContextMenu(Rect buttonRect, int index)
    {
        CreateContextMenu(index).DropDown(buttonRect);
    }

    GenericMenu CreateContextMenu(int index)
    {
        var menu           = new GenericMenu();
        var isSingleTarget = effectsProperty.serializedObject.targetObjects.Length == 1;
        if (isSingleTarget)
        {
            var effect     = effectsProperty.GetArrayElementAtIndex(index);
            var effectType = effect.managedReferenceValue.GetType();
            var name       = effect.FindPropertyRelative(nameof(Effect.name)).stringValue;
            if (string.IsNullOrWhiteSpace(name))
                name = ParaformProjectSettings.instance.NextName(effectType);

            var action = ParaformProjectSettings.instance.HasPreset(effectType, name) ? "Overwrite" : "Create";
            menu.AddItem(new GUIContent($"{action} Preset {name}"), false, () => SavePreset(index, name));
        }
        else
            menu.AddDisabledItem(new GUIContent("Save Preset"));

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Copy Effect"), false, () => CopyEffect(index));

        if (TryGetClipboardEffect(out var clipboardEffect))
            menu.AddItem(new GUIContent("Paste Effect"), false, () => PasteEffect(index, clipboardEffect));
        else
            menu.AddDisabledItem(new GUIContent("Paste Effect"));

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Manage Presets..."), false,
                     () => SettingsService.OpenProjectSettings("Project/Paraform"));
        return menu;
    }

    void CopyEffect(int index)
    {
        var effect = (Effect)effectsProperty.GetArrayElementAtIndex(index).managedReferenceValue;
        var clipboardData = new EffectClipboardData {
            effectType = effect.GetType().AssemblyQualifiedName,
            json       = JsonUtility.ToJson(effect)
        };
        EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(clipboardData);
    }

    void PasteEffect(int index, Effect clipboardEffect)
    {
        effectsProperty.serializedObject.ApplyModifiedProperties();
        foreach (var target in effectsProperty.serializedObject.targetObjects)
        {
            var serializedTarget = new SerializedObject(target);
            var effect           = serializedTarget.FindProperty(effectsProperty.propertyPath).GetArrayElementAtIndex(index);
            var replacement      = clipboardEffect.Clone();
            var name             = effect.FindPropertyRelative(nameof(Effect.name)).stringValue;
            if (!string.IsNullOrWhiteSpace(name))
                replacement.name = name;
            effect.managedReferenceValue = replacement;
            serializedTarget.ApplyModifiedProperties();
        }

        effectsProperty.serializedObject.Update();
    }

    static bool TryGetClipboardEffect(out Effect effect)
    {
        effect = null;
        try
        {
            var clipboardData = JsonUtility.FromJson<EffectClipboardData>(EditorGUIUtility.systemCopyBuffer);
            if (clipboardData == null || string.IsNullOrEmpty(clipboardData.effectType) ||
                string.IsNullOrEmpty(clipboardData.json))
                return false;

            var effectType = Type.GetType(clipboardData.effectType);
            if (effectType != typeof(Shadow) && effectType != typeof(Outline))
                return false;

            effect = (Effect)JsonUtility.FromJson(clipboardData.json, effectType);
            return effect != null;
        }
        catch (Exception)
        {
            effect = null;
            return false;
        }
    }

    void ApplyPreset(int index, Effect preset)
    {
        effectsProperty.serializedObject.ApplyModifiedProperties();
        foreach (var target in effectsProperty.serializedObject.targetObjects)
        {
            var serializedTarget = new SerializedObject(target);
            var effect           = serializedTarget.FindProperty(effectsProperty.propertyPath).GetArrayElementAtIndex(index);
            var enabled          = effect.FindPropertyRelative(nameof(Effect.enabled)).boolValue;
            var replacement      = preset.Clone();
            replacement.enabled = enabled;
            var name = effect.FindPropertyRelative(nameof(Effect.name)).stringValue;
            if (!string.IsNullOrWhiteSpace(name))
                replacement.name = name;
            effect.managedReferenceValue = replacement;
            serializedTarget.ApplyModifiedProperties();
        }

        effectsProperty.serializedObject.Update();
    }

    void SavePreset(int index, string name)
    {
        var effectProperty = effectsProperty.GetArrayElementAtIndex(index);
        var nameProperty   = effectProperty.FindPropertyRelative(nameof(Effect.name));
        nameProperty.stringValue = name;

        effectsProperty.serializedObject.ApplyModifiedProperties();
        effectsProperty.serializedObject.Update();
        var effect = (Effect)effectsProperty.GetArrayElementAtIndex(index).managedReferenceValue;
        ParaformProjectSettings.instance.SavePreset(effect);
    }

    void ShowAddMenu(Rect buttonRect, ReorderableList _)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent(nameof(Shadow)),  false, () => AddEffect(new Shadow()));
        menu.AddItem(new GUIContent(nameof(Outline)), false, () => AddEffect(new Outline()));
        menu.DropDown(buttonRect);
    }

    void AddEffect(Effect effect)
    {
        effectsProperty.serializedObject.ApplyModifiedProperties();
        var index = effectsProperty.arraySize;
        foreach (var target in effectsProperty.serializedObject.targetObjects)
        {
            var serializedTarget = new SerializedObject(target);
            var targetEffects    = serializedTarget.FindProperty(effectsProperty.propertyPath);
            targetEffects.InsertArrayElementAtIndex(index);
            targetEffects.GetArrayElementAtIndex(index).managedReferenceValue = effect.Clone();
            serializedTarget.ApplyModifiedProperties();
        }

        effectsProperty.serializedObject.Update();
        effects.index = index;
        onListChanged?.Invoke();
    }
}
}
