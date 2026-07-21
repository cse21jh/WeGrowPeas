using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PhoneHomeGrid의 커스텀 Inspector.
/// 항목 등록과 시각적 셀 선택 기능을 제공한다.
/// </summary>
[CustomEditor(typeof(PhoneHomeGrid))]
public sealed class PhoneHomeGridEditor : Editor
{
    private SerializedProperty _contentRoot;
    private SerializedProperty _generatedRoot;

    private SerializedProperty _columns;
    private SerializedProperty _rows;
    private SerializedProperty _spacing;
    private SerializedProperty _padding;

    private SerializedProperty _previewInEditMode;
    private SerializedProperty _removeUnusedGeneratedObjects;
    private SerializedProperty _entries;

    private int _selectedEntryIndex = -1;
    private GameObject _newPrefab;

    private void OnEnable()
    {
        _contentRoot =
            serializedObject.FindProperty(
                "contentRoot");

        _generatedRoot =
            serializedObject.FindProperty(
                "generatedRoot");

        _columns =
            serializedObject.FindProperty(
                "columns");

        _rows =
            serializedObject.FindProperty(
                "rows");

        _spacing =
            serializedObject.FindProperty(
                "spacing");

        _padding =
            serializedObject.FindProperty(
                "padding");

        _previewInEditMode =
            serializedObject.FindProperty(
                "previewInEditMode");

        _removeUnusedGeneratedObjects =
            serializedObject.FindProperty(
                "removeUnusedGeneratedObjects");

        _entries =
            serializedObject.FindProperty(
                "entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        DrawRootSettings();
        DrawGridSettings();
        DrawEntryCreator();
        DrawEntries();
        DrawVisualGrid();

        bool propertyChanged =
            EditorGUI.EndChangeCheck();

        bool applied =
            serializedObject.ApplyModifiedProperties();

        if (propertyChanged || applied)
        {
            RequestRebuildForTargets();
        }

        EditorGUILayout.Space(8f);
        DrawUtilityButtons();
    }

    private void DrawRootSettings()
    {
        EditorGUILayout.LabelField(
            "Grid Root",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            _contentRoot);

        EditorGUILayout.PropertyField(
            _generatedRoot);

        EditorGUILayout.Space(5f);
    }

    private void DrawGridSettings()
    {
        EditorGUILayout.LabelField(
            "Grid Settings",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            _columns);

        EditorGUILayout.PropertyField(
            _rows);

        EditorGUILayout.PropertyField(
            _spacing);

        EditorGUILayout.PropertyField(
            _padding,
            true);

        EditorGUILayout.PropertyField(
            _previewInEditMode);

        EditorGUILayout.PropertyField(
            _removeUnusedGeneratedObjects);

        EditorGUILayout.Space(8f);
    }

    private void DrawEntryCreator()
    {
        EditorGUILayout.LabelField(
            "Add App / Widget",
            EditorStyles.boldLabel);

        _newPrefab =
            EditorGUILayout.ObjectField(
                "Prefab",
                _newPrefab,
                typeof(GameObject),
                false)
            as GameObject;

        using (
            new EditorGUI.DisabledScope(
                _newPrefab == null))
        {
            if (GUILayout.Button(
                "Add Prefab To Grid",
                GUILayout.Height(26f)))
            {
                AddNewEntry(
                    _newPrefab);

                _newPrefab = null;
            }
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawEntries()
    {
        EditorGUILayout.LabelField(
            "Grid Entries",
            EditorStyles.boldLabel);

        if (_entries.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "등록된 앱 또는 위젯이 없습니다.",
                MessageType.Info);

            return;
        }

        int deleteIndex = -1;

        for (int i = 0;
             i < _entries.arraySize;
             i++)
        {
            SerializedProperty entry =
                _entries.GetArrayElementAtIndex(i);

            SerializedProperty displayName =
                entry.FindPropertyRelative(
                    "displayName");

            SerializedProperty prefab =
                entry.FindPropertyRelative(
                    "prefab");

            SerializedProperty cell =
                entry.FindPropertyRelative(
                    "cell");

            SerializedProperty span =
                entry.FindPropertyRelative(
                    "span");

            SerializedProperty active =
                entry.FindPropertyRelative(
                    "active");

            Color previousColor =
                GUI.backgroundColor;

            if (_selectedEntryIndex == i)
            {
                GUI.backgroundColor =
                    new Color(
                        0.65f,
                        0.9f,
                        0.65f);
            }

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            GUI.backgroundColor =
                previousColor;

            EditorGUILayout.BeginHorizontal();

            string visibleName =
                !string.IsNullOrWhiteSpace(
                    displayName.stringValue)
                    ? displayName.stringValue
                    : prefab.objectReferenceValue != null
                        ? prefab.objectReferenceValue.name
                        : $"Entry {i + 1}";

            if (GUILayout.Toggle(
                _selectedEntryIndex == i,
                $"{i + 1}. {visibleName}",
                "Button"))
            {
                _selectedEntryIndex = i;
            }

            if (GUILayout.Button(
                "삭제",
                GUILayout.Width(45f)))
            {
                deleteIndex = i;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(
                active);

            EditorGUILayout.PropertyField(
                displayName);

            GameObject newPrefab =
                EditorGUILayout.ObjectField(
                    "Prefab",
                    prefab.objectReferenceValue,
                    typeof(GameObject),
                    false)
                as GameObject;

            prefab.objectReferenceValue =
                newPrefab;

            cell.vector2IntValue =
                EditorGUILayout.Vector2IntField(
                    "Cell",
                    cell.vector2IntValue);

            span.vector2IntValue =
                EditorGUILayout.Vector2IntField(
                    "Span",
                    span.vector2IntValue);

            ClampEntry(
                cell,
                span);

            EditorGUILayout.EndVertical();
        }

        if (deleteIndex >= 0)
        {
            _entries.DeleteArrayElementAtIndex(
                deleteIndex);

            if (_selectedEntryIndex ==
                deleteIndex)
            {
                _selectedEntryIndex = -1;
            }
            else if (_selectedEntryIndex >
                    deleteIndex)
            {
                _selectedEntryIndex--;
            }
        }

        EditorGUILayout.Space(8f);
    }

    private void DrawVisualGrid()
    {
        int columns =
            Mathf.Max(
                1,
                _columns.intValue);

        int rows =
            Mathf.Max(
                1,
                _rows.intValue);

        EditorGUILayout.LabelField(
            "Visual Placement Grid",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "항목을 먼저 선택한 뒤 빈 셀을 누르면 해당 위치로 이동합니다. " +
            "이미 점유된 셀을 누르면 그 항목을 선택합니다.",
            MessageType.None);

        int[,] occupancy =
            BuildOccupancy(
                columns,
                rows,
                out bool[,] overlap);

        const float cellButtonSize = 34f;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(24f);

        for (int x = 0;
             x < columns;
             x++)
        {
            GUILayout.Label(
                x.ToString(),
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(
                    cellButtonSize));
        }

        EditorGUILayout.EndHorizontal();

        for (int y = 0;
             y < rows;
             y++)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(
                y.ToString(),
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(20f));

            for (int x = 0;
                 x < columns;
                 x++)
            {
                int occupant =
                    occupancy[x, y];

                Color previousColor =
                    GUI.backgroundColor;

                if (overlap[x, y])
                {
                    GUI.backgroundColor =
                        new Color(
                            1f,
                            0.45f,
                            0.45f);
                }
                else if (occupant ==
                         _selectedEntryIndex)
                {
                    GUI.backgroundColor =
                        new Color(
                            0.55f,
                            0.95f,
                            0.55f);
                }
                else if (occupant >= 0)
                {
                    GUI.backgroundColor =
                        new Color(
                            0.6f,
                            0.75f,
                            1f);
                }
                else
                {
                    GUI.backgroundColor =
                        new Color(
                            0.85f,
                            0.85f,
                            0.85f);
                }

                string label =
                    occupant >= 0
                        ? (occupant + 1)
                            .ToString()
                        : string.Empty;

                if (GUILayout.Button(
                    label,
                    GUILayout.Width(
                        cellButtonSize),
                    GUILayout.Height(
                        cellButtonSize)))
                {
                    HandleGridCellClick(
                        x,
                        y,
                        occupant);
                }

                GUI.backgroundColor =
                    previousColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8f);
    }

    private int[,] BuildOccupancy(
        int columns,
        int rows,
        out bool[,] overlap)
    {
        int[,] occupancy =
            new int[columns, rows];

        overlap =
            new bool[columns, rows];

        for (int x = 0;
             x < columns;
             x++)
        {
            for (int y = 0;
                 y < rows;
                 y++)
            {
                occupancy[x, y] = -1;
            }
        }

        for (int i = 0;
             i < _entries.arraySize;
             i++)
        {
            SerializedProperty entry =
                _entries.GetArrayElementAtIndex(i);

            SerializedProperty active =
                entry.FindPropertyRelative(
                    "active");

            if (!active.boolValue)
            {
                continue;
            }

            Vector2Int cell =
                entry.FindPropertyRelative(
                    "cell")
                .vector2IntValue;

            Vector2Int span =
                entry.FindPropertyRelative(
                    "span")
                .vector2IntValue;

            for (int x = cell.x;
                 x < cell.x + span.x;
                 x++)
            {
                for (int y = cell.y;
                     y < cell.y + span.y;
                     y++)
                {
                    if (x < 0 ||
                        x >= columns ||
                        y < 0 ||
                        y >= rows)
                    {
                        continue;
                    }

                    if (occupancy[x, y] >= 0 &&
                        occupancy[x, y] != i)
                    {
                        overlap[x, y] = true;
                    }
                    else
                    {
                        occupancy[x, y] = i;
                    }
                }
            }
        }

        return occupancy;
    }

    private void HandleGridCellClick(
        int x,
        int y,
        int occupant)
    {
        if (occupant >= 0 &&
            occupant != _selectedEntryIndex)
        {
            _selectedEntryIndex =
                occupant;

            Repaint();
            return;
        }

        if (_selectedEntryIndex < 0 ||
            _selectedEntryIndex >=
            _entries.arraySize)
        {
            return;
        }

        SerializedProperty selectedEntry =
            _entries.GetArrayElementAtIndex(
                _selectedEntryIndex);

        SerializedProperty cell =
            selectedEntry.FindPropertyRelative(
                "cell");

        SerializedProperty span =
            selectedEntry.FindPropertyRelative(
                "span");

        int maxX =
            Mathf.Max(
                0,
                _columns.intValue -
                span.vector2IntValue.x);

        int maxY =
            Mathf.Max(
                0,
                _rows.intValue -
                span.vector2IntValue.y);

        cell.vector2IntValue =
            new Vector2Int(
                Mathf.Clamp(
                    x,
                    0,
                    maxX),
                Mathf.Clamp(
                    y,
                    0,
                    maxY));

        serializedObject.ApplyModifiedProperties();

        RequestRebuildForTargets();
        Repaint();
    }

    private void AddNewEntry(
        GameObject prefab)
    {
        Undo.RecordObject(
            target,
            "Add Phone Home Grid Entry");

        int newIndex =
            _entries.arraySize;

        _entries.InsertArrayElementAtIndex(
            newIndex);

        SerializedProperty entry =
            _entries.GetArrayElementAtIndex(
                newIndex);

        entry.FindPropertyRelative(
                "id")
            .stringValue =
                Guid.NewGuid()
                    .ToString("N");

        entry.FindPropertyRelative(
                "displayName")
            .stringValue =
                prefab != null
                    ? prefab.name
                    : string.Empty;

        entry.FindPropertyRelative(
                "prefab")
            .objectReferenceValue =
                prefab;

        entry.FindPropertyRelative(
                "cell")
            .vector2IntValue =
                FindFirstEmptyCell();

        entry.FindPropertyRelative(
                "span")
            .vector2IntValue =
                Vector2Int.one;

        entry.FindPropertyRelative(
                "active")
            .boolValue =
                true;

        _selectedEntryIndex =
            newIndex;

        serializedObject.ApplyModifiedProperties();

        RequestRebuildForTargets();
    }

    private Vector2Int FindFirstEmptyCell()
    {
        int columns =
            Mathf.Max(
                1,
                _columns.intValue);

        int rows =
            Mathf.Max(
                1,
                _rows.intValue);

        int[,] occupancy =
            BuildOccupancy(
                columns,
                rows,
                out _);

        for (int y = 0;
             y < rows;
             y++)
        {
            for (int x = 0;
                 x < columns;
                 x++)
            {
                if (occupancy[x, y] < 0)
                {
                    return new Vector2Int(
                        x,
                        y);
                }
            }
        }

        return Vector2Int.zero;
    }

    private void ClampEntry(
        SerializedProperty cell,
        SerializedProperty span)
    {
        int columns =
            Mathf.Max(
                1,
                _columns.intValue);

        int rows =
            Mathf.Max(
                1,
                _rows.intValue);

        Vector2Int spanValue =
            span.vector2IntValue;

        spanValue.x =
            Mathf.Clamp(
                spanValue.x,
                1,
                columns);

        spanValue.y =
            Mathf.Clamp(
                spanValue.y,
                1,
                rows);

        span.vector2IntValue =
            spanValue;

        Vector2Int cellValue =
            cell.vector2IntValue;

        cellValue.x =
            Mathf.Clamp(
                cellValue.x,
                0,
                columns -
                spanValue.x);

        cellValue.y =
            Mathf.Clamp(
                cellValue.y,
                0,
                rows -
                spanValue.y);

        cell.vector2IntValue =
            cellValue;
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "Rebuild Preview"))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (UnityEngine.Object selectedTarget
                     in targets)
            {
                PhoneHomeGrid grid =
                    selectedTarget
                    as PhoneHomeGrid;

                grid?.Rebuild();
            }
        }

        if (GUILayout.Button(
            "Clear Generated"))
        {
            foreach (UnityEngine.Object selectedTarget
                     in targets)
            {
                PhoneHomeGrid grid =
                    selectedTarget
                    as PhoneHomeGrid;

                grid?.ClearGenerated();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RequestRebuildForTargets()
    {
        foreach (UnityEngine.Object selectedTarget
                 in targets)
        {
            PhoneHomeGrid grid =
                selectedTarget
                as PhoneHomeGrid;

            if (grid == null)
            {
                continue;
            }

            EditorUtility.SetDirty(grid);
            grid.RequestRebuild();
        }
    }
}
