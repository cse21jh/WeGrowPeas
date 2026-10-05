// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LeTai.Paraform.Scaffold.Editor
{
[FilePath("ProjectSettings/Paraform.asset", FilePathAttribute.Location.ProjectFolder)]
public class ParaformProjectSettings : ScriptableSingleton<ParaformProjectSettings>
{
    [SerializeReference]
    List<Effect> effectPresets = new();

    [SerializeField]
    bool defaultsAdded;

    static SerializedObject      serializedSettings;
    static ParaformEffectsEditor presetEditor;

    void OnEnable()
    {
        Undo.undoRedoPerformed += SaveAfterUndoRedo;
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= SaveAfterUndoRedo;
    }

    void SaveAfterUndoRedo() => Save(true);

    public IReadOnlyList<Effect> Presets
    {
        get
        {
            EnsureDefaults();
            return effectPresets;
        }
    }

    void EnsureDefaults()
    {
        if (defaultsAdded)
            return;

        defaultsAdded = true;
        AddDefaults();
        Save(true);
    }

    public void ResetToDefaults()
    {
        Undo.RegisterCompleteObjectUndo(this, "Reset Paraform Presets to Defaults");
        effectPresets.Clear();
        defaultsAdded = true;
        AddDefaults();
        Save(true);
    }

    void AddDefaults()
    {
        AddDefaultShadow("xs",  2,  1,  .6f);
        AddDefaultShadow("sm",  10, 4,  .36f);
        AddDefaultShadow("md",  16, 7,  .44f);
        AddDefaultShadow("lg",  34, 14, .6f);
        AddDefaultShadow("xl",  54, 21, .6f);
        AddDefaultShadow("2xl", 70, 29, .6f);

        AddDefault(new Outline { name = "inner", innerWidth = 4, outerWidth = 0 });
        AddDefault(new Outline { name = "outer", outerWidth = 4 });
        AddDefault(new Outline { name = "light", outerWidth = 0, outerSoftness = 100, softnessType = SoftnessType.Light });
    }

    void AddDefaultShadow(string name, float softness, float distance, float alpha)
    {
        AddDefault(new Shadow {
            name         = name,
            softness     = softness,
            offset       = new Vector2(0, -distance),
            color        = new Color(0, 0, 0, alpha),
            softnessType = SoftnessType.Simple
        });
    }

    void AddDefault(Effect effect)
    {
        if (!effectPresets.Exists(preset => preset != null && preset.GetType() == effect.GetType() && preset.name == effect.name))
            effectPresets.Add(effect);
    }

    public string NextName(Type effectType)
    {
        EnsureDefaults();
        var number = 1;
        var prefix = effectType.Name[0];
        while (HasPreset(effectType, $"{prefix}{number}"))
            number++;

        return $"{prefix}{number}";
    }

    public bool HasPreset(Type effectType, string name)
    {
        EnsureDefaults();
        return effectPresets.Exists(preset => preset != null && preset.GetType() == effectType && preset.name == name);
    }

    public void SavePreset(Effect effect)
    {
        EnsureDefaults();
        var index = effectPresets.FindIndex(preset => preset != null && preset.GetType() == effect.GetType() && preset.name == effect.name);
        Undo.RegisterCompleteObjectUndo(this, index >= 0 ? $"Overwrite Paraform Preset {effect.name}" : $"Create Paraform Preset {effect.name}");
        if (index >= 0)
        {
            effectPresets[index] = effect.Clone();
            for (var duplicate = effectPresets.Count - 1; duplicate > index; duplicate--)
                if (effectPresets[duplicate] != null && effectPresets[duplicate].GetType() == effect.GetType() && effectPresets[duplicate].name == effect.name)
                    effectPresets.RemoveAt(duplicate);
        }
        else
            effectPresets.Add(effect.Clone());

        Save(true);
    }

    public void SaveChanges() => Save(true);

    [SettingsProvider]
    static SettingsProvider CreateSettingsProvider()
    {
        return new SettingsProvider("Project/Paraform", SettingsScope.Project) {
            guiHandler = _ => DrawSettings()
        };
    }

    static void DrawSettings()
    {
        var settings = instance;
        settings.EnsureDefaults();
        if (serializedSettings == null || serializedSettings.targetObject != settings)
        {
            serializedSettings = new SerializedObject(settings);
            presetEditor       = new ParaformEffectsEditor(serializedSettings.FindProperty(nameof(effectPresets)), null, settings.SaveChanges, false);
        }

        serializedSettings.Update();
        if (GUILayout.Button("Reset to Defaults", GUILayout.ExpandWidth(false)))
        {
            settings.ResetToDefaults();
            serializedSettings.Update();
        }
        presetEditor.Draw();

        if (serializedSettings.ApplyModifiedProperties())
            settings.SaveChanges();
    }
}
}
