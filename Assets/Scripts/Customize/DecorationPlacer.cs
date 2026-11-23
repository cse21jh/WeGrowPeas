using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 치장 씬에서 장식품을 놓고, 고치고, 치운다.
///
/// 놓기 (<see cref="BeginPlacing"/>: 치장 목록에서 보유한 장식품을 누르면 시작)
///   - 치장 패널을 닫고, 장식품 그림이 반투명하게 마우스를 따라다닌다. 텃밭(<see cref="DecorationBlockedArea"/>)은 어둡게 표시된다.
///   - 놓을 수 있는 곳에서는 실제로 놓았을 때와 같은 레이어·순서로 그려서, 풀에 묻히는 정도까지 놓은 뒤와 같게 보인다
///     (<see cref="DecorationLayer.ApplyPlacementDepth"/>).
///   - 텃밭과 겹치거나 화면 밖으로 나가면 맨 앞에 붉게 그리고, 놓을 수 있는 곳에서 좌클릭하면 그 자리에 놓는다.
///   - Esc / 패널을 다시 열면 취소한다. 놓거나 취소하면 패널을 다시 연다.
///   - 들고 있는 동안에도 우클릭 드래그로 화면을 옮길 수 있다(VcamManager). 놓을 수 있는 곳은 지금 화면에 보이는 땅이다.
///
/// 고치기·치우기 (놓기 중이 아닐 때)
///   - 놓여 있는 장식품을 누르면 선택되고, 그림 네 모서리에 버튼이 뜬다. 다른 곳을 누르면 선택이 풀린다.
///       오른쪽 위 X     : 치운다.
///       왼쪽 위 회전    : 누른 채로 끌면 그림 가운데를 중심으로 돌린다.
///       오른쪽 아래 크기 : 누른 채로 끌면 그림 가운데를 중심으로 키우고 줄인다(장식품마다 범위가 다르다, <see cref="DecorationLayer.ScaleRange"/>).
///       왼쪽 아래 반전  : 누르면 좌우를 뒤집는다.
///   - 선택된 장식품을 다시 누른 채로 끌면 옮긴다.
///   - 옮기기·돌리기·크기 조절 중에는 실제 모습(풀에 묻히는 정도 포함)으로 보이고, 놓을 수 없는 모습이면 붉게 바뀐다.
///     손을 뗐을 때 놓을 수 없는 모습이면 원래대로 돌아간다. Esc로도 취소한다.
///
/// 결과는 <see cref="DecorationLayout"/>의 "지금 배치"에만 들어가고, 치장 화면의 저장 버튼을 눌러야 저장된다.
/// </summary>
public class DecorationPlacer : MonoBehaviour
{
    [SerializeField] private DecorationLayer layer;
    [SerializeField] private CustomizePanel panel;

    [Header("미리보기")]
    [SerializeField] private Color validTint = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color invalidTint = new Color(1f, 0.25f, 0.25f, 0.6f);
    [Tooltip("놓을 수 없는 자리(텃밭과 겹침, 화면 밖)에서는 미리보기를 이 맨 앞 레이어에 붉게 그려 텃밭 그림에 가려지지 않게 한다. " +
             "놓을 수 있는 자리에서는 실제로 놓았을 때와 같은 레이어·순서로 그린다(풀에 묻히는 정도까지 같게).")]
    [SerializeField] private string previewSortingLayer = "FX";
    [SerializeField] private int previewSortingOrder = 1000;
    [Tooltip("놓을 수 없는 자리의 미리보기 머티리얼. 맨 앞 레이어는 조명이 달라 어둡게 보이므로 조명을 받지 않는 Sprites-Default를 쓴다. " +
             "놓을 수 있는 자리에서는 실제 장식품과 같은 머티리얼(DecorationLayer.Material)을 쓴다.")]
    [SerializeField] private Material previewMaterial;
    [Tooltip("화면 가장자리에서 이만큼(월드 단위) 안쪽까지만 놓을 수 있다.")]
    [SerializeField] private float screenMargin = 0.1f;

    [Header("선택한 장식품 버튼 (화면 UI)")]
    [Tooltip("오른쪽 위: 누르면 치운다.")]
    [SerializeField] private Button removeButton;
    [Tooltip("왼쪽 위: 누른 채로 끌면 돌린다.")]
    [SerializeField] private Button rotateButton;
    [Tooltip("오른쪽 아래: 누른 채로 끌면 크기를 바꾼다.")]
    [SerializeField] private Button scaleButton;
    [Tooltip("왼쪽 아래: 누르면 좌우를 뒤집는다.")]
    [SerializeField] private Button flipButton;
    [Tooltip("버튼을 장식품 그림 모서리에서 옮길 거리(픽셀, 캔버스 기준). 오른쪽 위(X 버튼) 기준 값이고, 다른 모서리는 좌우·위아래를 대칭으로 쓴다.")]
    [SerializeField] private Vector2 removeButtonOffset = new Vector2(-8f, -8f);

    [Header("옮기기")]
    [Tooltip("선택된 장식품을 누른 채 이만큼(화면 픽셀) 움직이면 옮기기를 시작한다. 그보다 작으면 그냥 누른 것으로 본다.")]
    [SerializeField] private float dragThreshold = 6f;

    /// <summary>장식품을 들고 놓을 자리를 고르는 중인가.</summary>
    public bool IsPlacing => _placingItem != null;

    private enum EditMode { None, PendingMove, Moving, Rotating, Scaling }

    // 놓기
    private CustomizeItemData _placingItem;
    private SpriteRenderer _preview;
    private Vector2 _previewPivot;
    private bool _previewValid;

    // 선택·고치기
    private DecorationView _selected;
    private DecorationPlacement _reselect;   // 배치가 바뀐 뒤 다시 고를 장식품
    private EditMode _edit;
    private DecorationPose _editStartPose;
    private DecorationPose _editPose;
    private bool _editValid;
    private Color _editOriginalColor;
    private Vector2 _pressScreen;
    private Vector2 _pressWorld;
    private Vector2 _editCenter;             // 돌리기·크기 조절의 중심(그림 영역 가운데, 월드)
    private float _lastPointerAngle;
    private float _editAngle;                // 돌리는 중인 보이는 각도(누적)
    private float _startDistance;

    private Camera _cam;
    private Canvas _canvas;

    private void Awake()
    {
        _cam = Camera.main;
        if (removeButton != null)
        {
            _canvas = removeButton.GetComponentInParent<Canvas>();
            removeButton.onClick.AddListener(RemoveSelected);
        }
        if (flipButton != null) flipButton.onClick.AddListener(FlipSelected);
        SetButtonsShown(false);

        var go = new GameObject("DecorationPreview");
        go.transform.SetParent(transform, false);
        _preview = go.AddComponent<SpriteRenderer>();
        _preview.sortingLayerName = previewSortingLayer;
        _preview.sortingOrder = previewSortingOrder;
        if (previewMaterial != null) _preview.sharedMaterial = previewMaterial;
        go.SetActive(false);
    }

    private void OnEnable() => DecorationLayout.Changed += OnLayoutChanged;

    private void OnDisable()
    {
        DecorationLayout.Changed -= OnLayoutChanged;
        DecorationBlockedArea.SetAllDimmed(false);
    }

    private void Update()
    {
        if (IsPlacing) UpdatePlacing();
        else if (_edit != EditMode.None) UpdateEditing();
        else UpdateSelecting();
    }

    // ── 놓기 ─────────────────────────────────────────────────────────────────

    /// <summary>그 장식품을 들고 놓을 자리를 고르기 시작한다.</summary>
    public void BeginPlacing(CustomizeItemData item)
    {
        if (item == null || item.icon == null || item.category != CustomizeCategory.Decoration) return;
        if (DecorationLayout.IsPlaced(item)) return; // 같은 장식품은 하나만

        Deselect();
        _placingItem = item;
        var pose = new DecorationPose(Vector2.zero);
        _preview.sprite = item.icon;
        _preview.flipX = DecorationLayer.FlipFor(item, pose);
        _preview.transform.localScale = Vector3.one * DecorationLayer.ScaleFor(item);
        _preview.transform.rotation = Quaternion.Euler(0f, 0f, DecorationLayer.AngleFor(item, pose));
        _preview.gameObject.SetActive(true);
        DecorationBlockedArea.SetAllDimmed(true);
        if (panel != null) panel.Close();
        UpdatePreview();
    }

    /// <summary>놓기를 그만둔다(놓지 않음).</summary>
    public void CancelPlacing() => EndPlacing();

    private void EndPlacing()
    {
        if (!IsPlacing) return;
        _placingItem = null;
        _preview.gameObject.SetActive(false);
        DecorationBlockedArea.SetAllDimmed(false);
        if (panel != null && !panel.IsOpen) panel.Open();
    }

    private void UpdatePlacing()
    {
        // 놓는 중에 패널 화살표로 패널을 다시 열었으면 취소로 본다.
        if (panel != null && panel.IsOpen) { EndPlacing(); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { EndPlacing(); return; }

        UpdatePreview();

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && _previewValid)
        {
            DecorationLayout.Place(_placingItem, _previewPivot);
            EndPlacing();
        }
    }

    private void UpdatePreview()
    {
        Vector2 mouse = MouseWorld();
        _previewPivot = DecorationLayer.PivotForCenter(_placingItem, mouse);
        var pose = new DecorationPose(_previewPivot);

        // UI 위에 있을 때는 놓을 수 없지만 붉게 하지는 않는다(버튼을 누르려는 것일 수 있다).
        _previewValid = CanPlaceAt(_placingItem, pose);
        _preview.color = _previewValid ? validTint : invalidTint;

        float z;
        if (_previewValid && layer != null)
        {
            // 놓을 수 있는 자리: 실제로 놓았을 때와 같은 계산으로 레이어·순서·z를 정한다(풀에 묻히는 정도가 같다).
            z = layer.ApplyPlacementDepth(_preview, _placingItem, pose);
            SetPreviewMaterial(layer.Material);
        }
        else
        {
            // 놓을 수 없는 자리: 텃밭 그림 등에 가려지지 않게 맨 앞에 붉게 그린다.
            _preview.sortingLayerName = previewSortingLayer;
            _preview.sortingOrder = previewSortingOrder;
            SetPreviewMaterial(previewMaterial);
            z = layer != null ? layer.transform.position.z : 0f;
        }
        _preview.transform.position = new Vector3(_previewPivot.x, _previewPivot.y, z);

        // 놓은 뒤처럼 겨울 눈 덮개에 덮이지 않게(앞줄 풀잎 위에는 눈이 남는다). 정렬이 바뀌었을 수 있으니 매번 맞춘다.
        if (layer != null) layer.SyncSnowStencil(_preview);
    }

    private void SetPreviewMaterial(Material material)
    {
        if (material != null && _preview.sharedMaterial != material) _preview.sharedMaterial = material;
    }

    /// <summary>그 장식품을 그 모습으로 놓을 수 있는가. 화면 안에 다 들어오고 텃밭과 겹치지 않아야 한다.</summary>
    public bool CanPlaceAt(CustomizeItemData item, DecorationPose pose)
    {
        var rect = DecorationLayer.RectAt(item, pose);
        return InsideView(rect) && !DecorationBlockedArea.OverlapsAny(rect);
    }

    /// <summary>기본 모습으로 그 자리에 놓을 수 있는가.</summary>
    public bool CanPlaceAt(CustomizeItemData item, Vector2 pivot) => CanPlaceAt(item, new DecorationPose(pivot));

    private bool InsideView(Rect rect)
    {
        if (_cam == null || !_cam.orthographic) return true;
        float h = _cam.orthographicSize - screenMargin;
        float w = _cam.orthographicSize * _cam.aspect - screenMargin;
        Vector2 c = _cam.transform.position;
        return rect.xMin >= c.x - w && rect.xMax <= c.x + w && rect.yMin >= c.y - h && rect.yMax <= c.y + h;
    }

    // ── 선택 ─────────────────────────────────────────────────────────────────

    private void UpdateSelecting()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // 회전·크기 버튼은 누른 채로 끌어야 해서 Button 클릭이 아니라 여기서 직접 판정한다.
        if (_selected != null)
        {
            if (IsPointerOver(rotateButton)) { BeginEdit(EditMode.Rotating); return; }
            if (IsPointerOver(scaleButton)) { BeginEdit(EditMode.Scaling); return; }
        }
        if (IsPointerOverUI()) return; // X·반전 버튼 등은 Button이 처리한다

        var hit = layer != null ? layer.HitTest(MouseWorld()) : null;
        if (hit != null && hit == _selected) BeginEdit(EditMode.PendingMove); // 선택된 것을 다시 누름 → 끌면 옮기기
        else if (hit != null) Select(hit);
        else Deselect();
    }

    private void Select(DecorationView view)
    {
        _selected = view;
        SetButtonsShown(view != null);
        PlaceButtons();
    }

    private void Deselect()
    {
        if (_edit != EditMode.None) CancelEdit();
        _selected = null;
        _reselect = null;
        SetButtonsShown(false);
    }

    private void RemoveSelected()
    {
        if (_selected == null) return;
        var placement = _selected.Placement;
        Deselect();
        DecorationLayout.Remove(placement);
    }

    /// <summary>좌우를 뒤집는다. 그림 가운데 자리는 그대로 두고, 뒤집은 모습을 놓을 수 없으면 하지 않는다.</summary>
    private void FlipSelected()
    {
        if (_selected == null || _edit != EditMode.None) return;
        var item = _selected.Item;
        var pose = _selected.Pose;
        Vector2 center = _selected.Rect.center;
        pose.flipX = !pose.flipX;
        pose.position = DecorationLayer.PivotForCenter(item, center, pose);
        if (CanPlaceAt(item, pose)) DecorationLayout.SetPose(_selected.Placement, pose);
    }

    // 배치가 바뀌면 장식품을 전부 다시 만든다. 고르고 있던 장식품이 남아 있으면 LateUpdate에서 새 그림으로 다시 고른다.
    // (같은 이벤트로 DecorationLayer가 다시 만드는 순서와 상관없게 한 프레임 안에서 미룬다)
    private void OnLayoutChanged()
    {
        if (_edit != EditMode.None) CancelEdit();
        _reselect = _selected != null ? _selected.Placement : null;
        _selected = null;
        SetButtonsShown(false);
    }

    // ── 옮기기·돌리기·크기 조절 ─────────────────────────────────────────────

    private void BeginEdit(EditMode mode)
    {
        if (_selected == null) return;
        _edit = mode;
        _editStartPose = _selected.Pose;
        _editPose = _editStartPose;
        _editValid = true;
        _editOriginalColor = _selected.Renderer.color;
        _pressScreen = Input.mousePosition;
        _pressWorld = MouseWorld();
        _editCenter = _selected.Rect.center;

        Vector2 fromCenter = _pressWorld - _editCenter;
        _lastPointerAngle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
        _editAngle = DecorationLayer.AngleFor(_selected.Item, _editStartPose);
        _startDistance = Mathf.Max(0.05f, fromCenter.magnitude);

        if (mode != EditMode.PendingMove) DecorationBlockedArea.SetAllDimmed(true);
    }

    private void UpdateEditing()
    {
        if (_selected == null) { _edit = EditMode.None; return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { CancelEdit(); return; }

        var item = _selected.Item;
        Vector2 mouse = MouseWorld();

        switch (_edit)
        {
            case EditMode.PendingMove:
                if (!Input.GetMouseButton(0)) { _edit = EditMode.None; return; } // 끌지 않고 뗐다: 그냥 누른 것
                if (((Vector2)Input.mousePosition - _pressScreen).magnitude < dragThreshold) return;
                _edit = EditMode.Moving;
                DecorationBlockedArea.SetAllDimmed(true);
                goto case EditMode.Moving;

            case EditMode.Moving:
                _editPose.position = _editStartPose.position + (mouse - _pressWorld);
                break;

            case EditMode.Rotating:
            {
                Vector2 fromCenter = mouse - _editCenter;
                if (fromCenter.sqrMagnitude > 0.0001f)
                {
                    float pointer = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
                    _editAngle += Mathf.DeltaAngle(_lastPointerAngle, pointer); // 한 바퀴를 넘겨도 끊기지 않게 누적
                    _lastPointerAngle = pointer;
                }
                _editPose.rotation = DecorationLayer.PoseRotationFor(item, _editPose.flipX, _editAngle);
                _editPose.position = DecorationLayer.PivotForCenter(item, _editCenter, _editPose);
                break;
            }

            case EditMode.Scaling:
            {
                float ratio = (mouse - _editCenter).magnitude / _startDistance;
                DecorationLayer.ScaleRange(item, out float minScale, out float maxScale);
                _editPose.scale = Mathf.Clamp(_editStartPose.scale * ratio, minScale, maxScale);
                _editPose.position = DecorationLayer.PivotForCenter(item, _editCenter, _editPose);
                break;
            }
        }

        ShowEditPose();
        if (!Input.GetMouseButton(0)) EndEdit();
    }

    /// <summary>고치는 중인 모습을 실제처럼(앞뒤 순서·겨울 눈 처리 포함) 보이고, 놓을 수 없는 모습이면 붉게 칠한다.</summary>
    private void ShowEditPose()
    {
        if (layer != null) layer.ApplyPose(_selected, _editPose);
        _editValid = CanPlaceAt(_selected.Item, _editPose);
        _selected.Renderer.color = _editValid ? _editOriginalColor : invalidTint;
    }

    /// <summary>손을 뗐다. 놓을 수 있는 모습이면 배치에 반영하고, 아니면 원래대로 돌린다.</summary>
    private void EndEdit()
    {
        var mode = _edit;
        var view = _selected;
        FinishEditVisuals();
        if (mode == EditMode.PendingMove || view == null) return;

        if (_editValid) DecorationLayout.SetPose(view.Placement, _editPose); // 배치가 바뀌면 다시 그리고 다시 고른다
        else if (layer != null) layer.ApplyPose(view, view.Placement.Pose);
    }

    private void CancelEdit()
    {
        var view = _selected;
        bool changed = _edit != EditMode.PendingMove;
        FinishEditVisuals();
        if (changed && view != null && layer != null) layer.ApplyPose(view, view.Placement.Pose);
    }

    private void FinishEditVisuals()
    {
        if (_selected != null && _edit != EditMode.None) _selected.Renderer.color = _editOriginalColor;
        _edit = EditMode.None;
        if (!IsPlacing) DecorationBlockedArea.SetAllDimmed(false);
    }

    // ── 버튼 ─────────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (_reselect != null && layer != null)
        {
            foreach (var view in layer.Views)
                if (view != null && view.Placement == _reselect) { Select(view); break; }
            _reselect = null;
        }

        if (_selected == null) return;
        PlaceButtons();
    }

    private void SetButtonsShown(bool shown)
    {
        foreach (var b in new[] { removeButton, rotateButton, scaleButton, flipButton })
            if (b != null && b.gameObject.activeSelf != shown) b.gameObject.SetActive(shown);
    }

    /// <summary>버튼들을 선택한 장식품 그림의 네 모서리에 붙인다.</summary>
    private void PlaceButtons()
    {
        if (_selected == null || _cam == null) return;
        var rect = _selected.Rect;
        var o = removeButtonOffset;
        PlaceAtCorner(removeButton, new Vector2(rect.xMax, rect.yMax), new Vector2(o.x, o.y));
        PlaceAtCorner(rotateButton, new Vector2(rect.xMin, rect.yMax), new Vector2(-o.x, o.y));
        PlaceAtCorner(scaleButton, new Vector2(rect.xMax, rect.yMin), new Vector2(o.x, -o.y));
        PlaceAtCorner(flipButton, new Vector2(rect.xMin, rect.yMin), new Vector2(-o.x, -o.y));
    }

    private void PlaceAtCorner(Button button, Vector2 worldCorner, Vector2 offset)
    {
        if (button == null) return;
        var rt = (RectTransform)button.transform;
        var parent = (RectTransform)rt.parent;
        Vector3 screen = _cam.WorldToScreenPoint(new Vector3(worldCorner.x, worldCorner.y, 0f));
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, UICamera, out var local))
            rt.localPosition = local + offset;
    }

    private Camera UICamera => _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;

    private bool IsPointerOver(Button button)
        => button != null && button.gameObject.activeInHierarchy
           && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, Input.mousePosition, UICamera);

    // ── 공용 ─────────────────────────────────────────────────────────────────

    private Vector2 MouseWorld()
    {
        if (_cam == null) _cam = Camera.main;
        return _cam != null ? (Vector2)_cam.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;
    }

    private static bool IsPointerOverUI()
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
