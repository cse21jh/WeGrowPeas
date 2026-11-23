using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// 치장 아이템이 목록에 보이는 순서를 끌어다 놓아서 바꾸는 창. (Tools/Customize/아이템 순서 편집)
///
/// 종류를 고르면 그 종류의 아이템이 지금 목록 순서(sortOrder, 같으면 에셋 이름 순)대로 나온다.
/// 줄을 끌어 옮기면 그 종류 아이템 전부의 sortOrder를 위에서부터 0, 1, 2…로 다시 매겨 바로 저장한다.
/// 게임은 지금처럼 sortOrder만 보므로 실행 코드는 바뀌지 않는다. 새 아이템을 만들면 이 창에 자동으로 나타난다.
/// </summary>
public class CustomizeItemOrderWindow : EditorWindow
{
    private const float RowHeight = 40f;
    private const float IconSize = 34f;

    private CustomizeCategory _category = CustomizeCategory.Decoration;
    private readonly List<CustomizeItemData> _items = new List<CustomizeItemData>();
    private ReorderableList _list;
    private Vector2 _scroll;

    [MenuItem("Tools/Customize/아이템 순서 편집")]
    private static void Open()
    {
        var window = GetWindow<CustomizeItemOrderWindow>("치장 아이템 순서");
        window.minSize = new Vector2(360f, 300f);
    }

    private void OnEnable()
    {
        Reload();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    // 아이템을 새로 만들거나 지우거나 이름을 바꾸면 목록을 다시 읽는다.
    private void OnProjectChange()
    {
        Reload();
        Repaint();
    }

    private void OnUndoRedo()
    {
        Reload();
        Repaint();
    }

    // ── 읽기 ─────────────────────────────────────────────────────────────────

    private void Reload()
    {
        _items.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(CustomizeItemData)))
        {
            var item = AssetDatabase.LoadAssetAtPath<CustomizeItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.category == _category) _items.Add(item);
        }

        // 게임(CustomizeCatalog)과 같은 순서
        _items.Sort((a, b) => a.sortOrder != b.sortOrder
            ? a.sortOrder.CompareTo(b.sortOrder)
            : string.CompareOrdinal(a.name, b.name));

        _list = new ReorderableList(_items, typeof(CustomizeItemData), true, true, false, false)
        {
            elementHeight = RowHeight,
            drawHeaderCallback = DrawHeader,
            drawElementCallback = DrawRow,
            onReorderCallback = _ => ApplyOrder(),
            onSelectCallback = l =>
            {
                if (l.index >= 0 && l.index < _items.Count) EditorGUIUtility.PingObject(_items[l.index]);
            },
        };
    }

    // ── 그리기 ───────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            _category = (CustomizeCategory)EditorGUILayout.EnumPopup("종류", _category);
            if (EditorGUI.EndChangeCheck()) Reload();

            if (GUILayout.Button("새로고침", GUILayout.Width(70f))) Reload();
        }

        EditorGUILayout.HelpBox(
            "줄 왼쪽 손잡이(=)를 끌어 순서를 바꾸면 위에서부터 sortOrder가 0, 1, 2…로 다시 매겨져 바로 저장됩니다. " +
            "줄을 누르면 Project 창에서 그 에셋을 찾아 줍니다. Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.None);

        if (HasDuplicateOrders())
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox("sortOrder가 겹치는 아이템이 있어 에셋 이름 순으로 보이고 있습니다. 지금 보이는 순서로 번호를 정리할 수 있습니다.", MessageType.Info);
                if (GUILayout.Button("지금 순서로\n번호 정리", GUILayout.Width(80f), GUILayout.Height(38f))) ApplyOrder();
            }
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _list.DoLayoutList();
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, $"{_category} — {_items.Count}개 (위가 목록 앞쪽)");
    }

    private void DrawRow(Rect rect, int index, bool isActive, bool isFocused)
    {
        var item = _items[index];
        if (item == null) return;

        rect.y += (RowHeight - IconSize) * 0.5f;
        var iconRect = new Rect(rect.x, rect.y, IconSize, IconSize);
        if (item.icon != null)
        {
            var preview = AssetPreview.GetAssetPreview(item.icon) ?? AssetPreview.GetMiniThumbnail(item.icon);
            if (preview != null) GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
            else if (AssetPreview.IsLoadingAssetPreview(item.icon.GetInstanceID())) Repaint();
        }
        else
        {
            EditorGUI.DrawRect(iconRect, new Color(0f, 0f, 0f, 0.15f));
        }

        float x = iconRect.xMax + 8f;
        float orderWidth = 46f;
        var nameRect = new Rect(x, rect.y, rect.xMax - x - orderWidth, 18f);
        var idRect = new Rect(x, rect.y + 17f, rect.xMax - x - orderWidth, 16f);
        var orderRect = new Rect(rect.xMax - orderWidth, rect.y + 8f, orderWidth, 18f);

        string title = string.IsNullOrEmpty(item.displayName) ? "(이름 없음)" : item.displayName;
        if (item.isDefault) title += "  [기본]";
        EditorGUI.LabelField(nameRect, title, EditorStyles.boldLabel);
        EditorGUI.LabelField(idRect, item.name, EditorStyles.miniLabel);
        EditorGUI.LabelField(orderRect, item.sortOrder.ToString(), EditorStyles.centeredGreyMiniLabel);
    }

    // ── 저장 ─────────────────────────────────────────────────────────────────

    private bool HasDuplicateOrders()
        => _items.Count > 1 && _items.Select(i => i.sortOrder).Distinct().Count() != _items.Count;

    /// <summary>지금 보이는 순서대로 sortOrder를 0부터 다시 매기고 저장한다. 바뀐 에셋만 기록한다.</summary>
    private void ApplyOrder()
    {
        var changed = new List<CustomizeItemData>();
        for (int i = 0; i < _items.Count; i++)
            if (_items[i] != null && _items[i].sortOrder != i) changed.Add(_items[i]);
        if (changed.Count == 0) return;

        Undo.RecordObjects(changed.ToArray(), "치장 아이템 순서 변경");
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item == null || item.sortOrder == i) continue;
            item.sortOrder = i;
            EditorUtility.SetDirty(item);
        }
        AssetDatabase.SaveAssets();
    }
}
