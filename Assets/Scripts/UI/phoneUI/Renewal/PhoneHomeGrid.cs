using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 휴대폰 홈 화면의 앱 및 위젯을 배치하는 커스텀 UI 그리드.
///
/// 좌상단 셀을 (0, 0)으로 사용하며,
/// 앱과 위젯은 Span 값에 따라 여러 셀을 차지할 수 있다.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PhoneHomeGrid : MonoBehaviour
{
    [Serializable]
    public sealed class GridEntry
    {
        [SerializeField, HideInInspector]
        private string id;

        [Tooltip("Inspector에 표시할 이름입니다.")]
        [SerializeField]
        private string displayName;

        [Tooltip("이 위치에 생성할 UI 프리팹입니다.")]
        [SerializeField]
        private GameObject prefab;

        [Tooltip("좌상단을 기준으로 한 셀 좌표입니다.")]
        [SerializeField]
        private Vector2Int cell;

        [Tooltip("가로와 세로로 차지할 셀 개수입니다.")]
        [SerializeField]
        private Vector2Int span = Vector2Int.one;

        [Tooltip("비활성화하면 생성 및 배치에서 제외됩니다.")]
        [SerializeField]
        private bool active = true;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        public Vector2Int Cell
        {
            get => cell;
            set => cell = value;
        }

        public Vector2Int Span
        {
            get => span;
            set => span = value;
        }

        public bool Active
        {
            get => active;
            set => active = value;
        }

        public string GetVisibleName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            return prefab != null
                ? prefab.name
                : "Empty";
        }
    }

    #region Inspector

    [Header("Grid Root")]

    [Tooltip(
        "그리드 영역으로 사용할 RectTransform입니다. " +
        "비워두면 이 컴포넌트가 붙은 RectTransform을 사용합니다.")]
    [SerializeField]
    private RectTransform contentRoot;

    [Tooltip(
        "자동 생성된 오브젝트가 들어갈 부모입니다. " +
        "비워두면 자동 생성합니다.")]
    [SerializeField]
    private RectTransform generatedRoot;

    [Header("Grid Size")]

    [Min(1)]
    [SerializeField]
    private int columns = 5;

    [Min(1)]
    [SerializeField]
    private int rows = 8;

    [Tooltip("각 셀 사이의 가로·세로 간격입니다.")]
    [SerializeField]
    private Vector2 spacing = new Vector2(8f, 10f);

    [Tooltip("그리드 영역 내부의 좌우상하 여백입니다.")]
    [SerializeField]
    private RectOffset padding;

    [Header("Editor Preview")]

    [Tooltip("플레이하지 않아도 에디터에서 배치 결과를 생성합니다.")]
    [SerializeField]
    private bool previewInEditMode = true;

    [Tooltip(
        "Entries에서 제거된 항목에 대응하는 생성 오브젝트도 삭제합니다.")]
    [SerializeField]
    private bool removeUnusedGeneratedObjects = true;

    [Header("Entries")]

    [SerializeField]
    private List<GridEntry> entries =
        new List<GridEntry>();

    #endregion

    private const string GeneratedRootName =
        "__GeneratedHomeGrid";

    private bool _isRebuilding;

#if UNITY_EDITOR
    private bool _editorRebuildQueued;
#endif

    public int Columns => columns;
    public int Rows => rows;

    public IReadOnlyList<GridEntry> Entries =>
        entries;

    private void Reset()
    {
        contentRoot =
            transform as RectTransform;

        EnsurePadding();
        EnsureEntryData();
        RequestRebuild();
    }

    private void OnEnable()
    {
        EnsurePadding();

        if (Application.IsPlaying(gameObject) ||
            previewInEditMode)
        {
            RequestRebuild();
        }
    }

    private void OnValidate()
    {
        columns =
            Mathf.Max(1, columns);

        rows =
            Mathf.Max(1, rows);

        spacing.x =
            Mathf.Max(0f, spacing.x);

        spacing.y =
            Mathf.Max(0f, spacing.y);

        EnsurePadding();

        EnsureEntryData();

        if (previewInEditMode)
        {
            RequestRebuild();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (Application.IsPlaying(gameObject) ||
            previewInEditMode)
        {
            RequestRebuild();
        }
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -=
            DelayedEditorRebuild;

        _editorRebuildQueued = false;
#endif
    }

    /// <summary>
    /// Padding이 아직 생성되지 않았다면 안전한 Unity 콜백 시점에 생성한다.
    /// </summary>
    private void EnsurePadding()
    {
        if (padding != null)
        {
            return;
        }

        padding = new RectOffset(
            8,
            8,
            8,
            8);
    }

    /// <summary>
    /// 현재 설정으로 그리드 재생성을 요청한다.
    /// 에디터에서는 OnValidate 도중 RectTransform을 직접 변경하지 않도록
    /// 다음 에디터 갱신 시점으로 처리를 미룬다.
    /// </summary>
    public void RequestRebuild()
    {
        if (_isRebuilding)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            /*
             * 일반 Prefab Asset 자체에는 생성하지 않는다.
             * Scene 또는 Prefab Mode에서만 생성한다.
             */
            if (!gameObject.scene.IsValid())
            {
                return;
            }

            if (!previewInEditMode)
            {
                return;
            }

            if (_editorRebuildQueued)
            {
                return;
            }

            _editorRebuildQueued = true;

            EditorApplication.delayCall +=
                DelayedEditorRebuild;

            return;
        }
#endif

        Rebuild();
    }

#if UNITY_EDITOR
    private void DelayedEditorRebuild()
    {
        EditorApplication.delayCall -=
            DelayedEditorRebuild;

        _editorRebuildQueued = false;

        if (this == null ||
            Application.IsPlaying(gameObject) ||
            !previewInEditMode ||
            !isActiveAndEnabled ||
            !gameObject.scene.IsValid())
        {
            return;
        }

        Rebuild();
    }
#endif

    /// <summary>
    /// 현재 Entries를 기준으로 프리팹을 생성하고 배치한다.
    /// </summary>
    [ContextMenu("Rebuild Grid")]
    public void Rebuild()
    {
        if (_isRebuilding)
        {
            return;
        }

        _isRebuilding = true;

        try
        {
            EnsurePadding();

            if (!ValidateRoot())
            {
                return;
            }

            EnsureEntryData();
            EnsureGeneratedRoot();

            if (generatedRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Dictionary<string, PhoneHomeGridGeneratedSlot>
                existingSlots =
                    CollectExistingSlots();

            HashSet<string> usedEntryIds =
                new HashSet<string>();

            bool[,] occupied =
                new bool[columns, rows];

            for (int i = 0; i < entries.Count; i++)
            {
                GridEntry entry =
                    entries[i];

                if (entry == null ||
                    !entry.Active ||
                    entry.Prefab == null)
                {
                    continue;
                }

                if (!IsEntryInsideGrid(entry))
                {
                    Debug.LogWarning(
                        $"{name}: '{entry.GetVisibleName()}'의 " +
                        $"위치 또는 크기가 그리드 범위를 벗어났습니다.",
                        this);

                    continue;
                }

                if (OverlapsOccupiedCell(
                    entry,
                    occupied))
                {
                    Debug.LogWarning(
                        $"{name}: '{entry.GetVisibleName()}'이 " +
                        "다른 앱 또는 위젯과 겹칩니다.",
                        this);

                    continue;
                }

                MarkOccupied(
                    entry,
                    occupied);

                PhoneHomeGridGeneratedSlot slot =
                    GetOrCreateSlot(
                        entry,
                        existingSlots);

                if (slot == null)
                {
                    continue;
                }

                usedEntryIds.Add(
                    entry.Id);

                slot.transform.SetSiblingIndex(i);

                ApplySlotLayout(
                    slot.GetComponent<RectTransform>(),
                    entry);

                EnsurePrefabInstance(
                    slot,
                    entry);
            }

            if (removeUnusedGeneratedObjects)
            {
                RemoveUnusedSlots(
                    existingSlots,
                    usedEntryIds);
            }

#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(generatedRoot);
            }
#endif
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    [ContextMenu("Clear Generated Grid")]
    public void ClearGenerated()
    {
        if (generatedRoot == null)
        {
            return;
        }

        List<GameObject> children =
            new List<GameObject>();

        for (int i = 0;
             i < generatedRoot.childCount;
             i++)
        {
            children.Add(
                generatedRoot
                    .GetChild(i)
                    .gameObject);
        }

        for (int i = 0;
             i < children.Count;
             i++)
        {
            DestroySmart(children[i]);
        }
    }

    private bool ValidateRoot()
    {
        if (contentRoot == null)
        {
            contentRoot =
                transform as RectTransform;
        }

        if (contentRoot == null)
        {
            Debug.LogError(
                $"{name}: PhoneHomeGrid는 " +
                "RectTransform 오브젝트에 붙어 있어야 합니다.",
                this);

            return false;
        }

        return true;
    }

    private void EnsureEntryData()
    {
        if (entries == null)
        {
            entries =
                new List<GridEntry>();
        }

        for (int i = 0;
             i < entries.Count;
             i++)
        {
            GridEntry entry =
                entries[i];

            if (entry == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.Id))
            {
                entry.Id =
                    Guid.NewGuid()
                        .ToString("N");
            }

            entry.Span =
                new Vector2Int(
                    Mathf.Clamp(
                        entry.Span.x,
                        1,
                        columns),
                    Mathf.Clamp(
                        entry.Span.y,
                        1,
                        rows));

            entry.Cell =
                new Vector2Int(
                    Mathf.Clamp(
                        entry.Cell.x,
                        0,
                        Mathf.Max(
                            0,
                            columns -
                            entry.Span.x)),
                    Mathf.Clamp(
                        entry.Cell.y,
                        0,
                        Mathf.Max(
                            0,
                            rows -
                            entry.Span.y)));
        }
    }

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot == null)
        {
            Transform existing =
                contentRoot.Find(
                    GeneratedRootName);

            if (existing != null)
            {
                generatedRoot =
                    existing as RectTransform;
            }
        }

        if (generatedRoot == null)
        {
            GameObject rootObject =
                new GameObject(
                    GeneratedRootName,
                    typeof(RectTransform));

#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                Undo.RegisterCreatedObjectUndo(
                    rootObject,
                    "Create Phone Home Grid Root");
            }
#endif

            generatedRoot =
                rootObject.GetComponent<RectTransform>();

            generatedRoot.SetParent(
                contentRoot,
                false);
        }

        StretchToParent(
            generatedRoot);
    }

    private Dictionary<string, PhoneHomeGridGeneratedSlot>
        CollectExistingSlots()
    {
        var result =
            new Dictionary<
                string,
                PhoneHomeGridGeneratedSlot>();

        PhoneHomeGridGeneratedSlot[] slots =
            generatedRoot.GetComponentsInChildren<
                PhoneHomeGridGeneratedSlot>(
                    true);

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            PhoneHomeGridGeneratedSlot slot =
                slots[i];

            if (slot.transform.parent !=
                generatedRoot)
            {
                continue;
            }

            if (string.IsNullOrEmpty(
                slot.EntryId))
            {
                continue;
            }

            if (result.ContainsKey(
                slot.EntryId))
            {
                DestroySmart(
                    slot.gameObject);

                continue;
            }

            result.Add(
                slot.EntryId,
                slot);
        }

        return result;
    }

    private PhoneHomeGridGeneratedSlot GetOrCreateSlot(
        GridEntry entry,
        Dictionary<string, PhoneHomeGridGeneratedSlot>
            existingSlots)
    {
        if (existingSlots.TryGetValue(
            entry.Id,
            out PhoneHomeGridGeneratedSlot slot))
        {
            slot.name =
                GetSlotName(entry);

            return slot;
        }

        GameObject slotObject =
            new GameObject(
                GetSlotName(entry),
                typeof(RectTransform),
                typeof(PhoneHomeGridGeneratedSlot));

#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            Undo.RegisterCreatedObjectUndo(
                slotObject,
                "Create Phone Home Grid Slot");
        }
#endif

        RectTransform slotRect =
            slotObject.GetComponent<RectTransform>();

        slotRect.SetParent(
            generatedRoot,
            false);

        slot =
            slotObject.GetComponent<
                PhoneHomeGridGeneratedSlot>();

        slot.EntryId =
            entry.Id;

        existingSlots.Add(
            entry.Id,
            slot);

        return slot;
    }

    private void EnsurePrefabInstance(
        PhoneHomeGridGeneratedSlot slot,
        GridEntry entry)
    {
        bool needsNewInstance =
            slot.SourcePrefab != entry.Prefab ||
            slot.transform.childCount == 0;

        if (!needsNewInstance)
        {
            RectTransform existingChild =
                slot.transform.GetChild(0)
                    as RectTransform;

            if (existingChild != null)
            {
                StretchToParent(
                    existingChild);
            }

            return;
        }

        List<GameObject> oldChildren =
            new List<GameObject>();

        for (int i = 0;
             i < slot.transform.childCount;
             i++)
        {
            oldChildren.Add(
                slot.transform
                    .GetChild(i)
                    .gameObject);
        }

        for (int i = 0;
             i < oldChildren.Count;
             i++)
        {
            DestroySmart(
                oldChildren[i]);
        }

        GameObject instance = null;

#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(
                entry.Prefab))
            {
                Debug.LogWarning(
                    $"{name}: '{entry.GetVisibleName()}'은 " +
                    "Project의 Prefab Asset이 아닙니다.",
                    this);

                return;
            }

            instance =
                PrefabUtility.InstantiatePrefab(
                    entry.Prefab,
                    slot.transform)
                as GameObject;

            if (instance != null)
            {
                Undo.RegisterCreatedObjectUndo(
                    instance,
                    "Instantiate Phone Home Item");
            }
        }
        else
#endif
        {
            instance =
                Instantiate(
                    entry.Prefab,
                    slot.transform,
                    false);
        }

        if (instance == null)
        {
            return;
        }

        instance.name =
            entry.Prefab.name;

        RectTransform instanceRect =
            instance.transform
                as RectTransform;

        if (instanceRect == null)
        {
            Debug.LogError(
                $"{name}: '{entry.GetVisibleName()}' 프리팹의 " +
                "루트에 RectTransform이 없습니다.",
                instance);

            return;
        }

        StretchToParent(
            instanceRect);

        slot.SourcePrefab =
            entry.Prefab;
    }

    private void ApplySlotLayout(
        RectTransform slotRect,
        GridEntry entry)
    {
        Vector2 cellSize =
            CalculateCellSize();

        float width =
            cellSize.x *
            entry.Span.x +
            spacing.x *
            (entry.Span.x - 1);

        float height =
            cellSize.y *
            entry.Span.y +
            spacing.y *
            (entry.Span.y - 1);

        float x =
            padding.left +
            entry.Cell.x *
            (cellSize.x +
             spacing.x);

        float y =
            padding.top +
            entry.Cell.y *
            (cellSize.y +
             spacing.y);

        slotRect.anchorMin =
            new Vector2(0f, 1f);

        slotRect.anchorMax =
            new Vector2(0f, 1f);

        slotRect.pivot =
            new Vector2(0f, 1f);

        slotRect.anchoredPosition =
            new Vector2(
                x,
                -y);

        slotRect.sizeDelta =
            new Vector2(
                width,
                height);

        slotRect.localScale =
            Vector3.one;

        slotRect.localRotation =
            Quaternion.identity;
    }

    private Vector2 CalculateCellSize()
    {
        float horizontalSpacing =
            spacing.x *
            Mathf.Max(
                0,
                columns - 1);

        float verticalSpacing =
            spacing.y *
            Mathf.Max(
                0,
                rows - 1);

        float availableWidth =
            contentRoot.rect.width -
            padding.left -
            padding.right -
            horizontalSpacing;

        float availableHeight =
            contentRoot.rect.height -
            padding.top -
            padding.bottom -
            verticalSpacing;

        return new Vector2(
            Mathf.Max(
                0.01f,
                availableWidth /
                columns),
            Mathf.Max(
                0.01f,
                availableHeight /
                rows));
    }

    private bool IsEntryInsideGrid(
        GridEntry entry)
    {
        return
            entry.Cell.x >= 0 &&
            entry.Cell.y >= 0 &&
            entry.Span.x >= 1 &&
            entry.Span.y >= 1 &&
            entry.Cell.x +
            entry.Span.x <=
            columns &&
            entry.Cell.y +
            entry.Span.y <=
            rows;
    }

    private static bool OverlapsOccupiedCell(
        GridEntry entry,
        bool[,] occupied)
    {
        for (int x = entry.Cell.x;
             x < entry.Cell.x +
             entry.Span.x;
             x++)
        {
            for (int y = entry.Cell.y;
                 y < entry.Cell.y +
                 entry.Span.y;
                 y++)
            {
                if (occupied[x, y])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void MarkOccupied(
        GridEntry entry,
        bool[,] occupied)
    {
        for (int x = entry.Cell.x;
             x < entry.Cell.x +
             entry.Span.x;
             x++)
        {
            for (int y = entry.Cell.y;
                 y < entry.Cell.y +
                 entry.Span.y;
                 y++)
            {
                occupied[x, y] =
                    true;
            }
        }
    }

    private void RemoveUnusedSlots(
        Dictionary<string, PhoneHomeGridGeneratedSlot>
            existingSlots,
        HashSet<string> usedEntryIds)
    {
        List<GameObject> objectsToRemove =
            new List<GameObject>();

        foreach (
            KeyValuePair<
                string,
                PhoneHomeGridGeneratedSlot>
            pair in existingSlots)
        {
            if (!usedEntryIds.Contains(
                pair.Key))
            {
                objectsToRemove.Add(
                    pair.Value.gameObject);
            }
        }

        for (int i = 0;
             i < objectsToRemove.Count;
             i++)
        {
            DestroySmart(
                objectsToRemove[i]);
        }
    }

    private static void StretchToParent(
        RectTransform target)
    {
        target.anchorMin =
            Vector2.zero;

        target.anchorMax =
            Vector2.one;

        target.pivot =
            new Vector2(
                0.5f,
                0.5f);

        target.offsetMin =
            Vector2.zero;

        target.offsetMax =
            Vector2.zero;

        target.localScale =
            Vector3.one;

        target.localRotation =
            Quaternion.identity;
    }

    private static string GetSlotName(
        GridEntry entry)
    {
        return
            $"GridSlot_{entry.GetVisibleName()}";
    }

    private static void DestroySmart(
        UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.DestroyObjectImmediate(
                target);

            return;
        }
#endif

        Destroy(target);
    }
}
