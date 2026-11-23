using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 치장 씬에서 장식품을 놓고 치운다.
///
/// 놓기 (<see cref="BeginPlacing"/>: 치장 목록에서 보유한 장식품을 누르면 시작)
///   - 치장 패널을 닫고, 장식품 그림이 반투명하게 마우스를 따라다닌다. 텃밭(<see cref="DecorationBlockedArea"/>)은 어둡게 표시된다.
///   - 놓을 수 있는 곳에서는 실제로 놓았을 때와 같은 레이어·순서로 그려서, 풀에 묻히는 정도까지 놓은 뒤와 같게 보인다
///     (<see cref="DecorationLayer.ApplyPlacementDepth"/>).
///   - 텃밭과 겹치거나 화면 밖으로 나가면 맨 앞에 붉게 그리고, 놓을 수 있는 곳에서 좌클릭하면 그 자리에 놓는다.
///   - Esc / 패널을 다시 열면 취소한다. 놓거나 취소하면 패널을 다시 연다.
///   - 들고 있는 동안에도 우클릭 드래그로 화면을 옮길 수 있다(VcamManager). 놓을 수 있는 곳은 지금 화면에 보이는 땅이다.
///
/// 치우기 (놓기 중이 아닐 때)
///   - 놓여 있는 장식품을 누르면 오른쪽 위에 X 버튼이 뜨고, X를 누르면 치운다. 다른 곳을 누르면 X가 사라진다.
///
/// 놓고 치운 결과는 <see cref="DecorationLayout"/>의 "지금 배치"에만 들어가고, 치장 화면의 저장 버튼을 눌러야 저장된다.
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

    [Header("치우기")]
    [Tooltip("선택한 장식품 오른쪽 위에 띄울 X 버튼(화면 UI).")]
    [SerializeField] private Button removeButton;
    [Tooltip("X 버튼을 장식품 그림의 오른쪽 위 모서리에서 옮길 거리(픽셀, 캔버스 기준).")]
    [SerializeField] private Vector2 removeButtonOffset = new Vector2(-8f, -8f);

    /// <summary>장식품을 들고 놓을 자리를 고르는 중인가.</summary>
    public bool IsPlacing => _placingItem != null;

    private CustomizeItemData _placingItem;
    private SpriteRenderer _preview;
    private Vector2 _previewPivot;
    private bool _previewValid;

    private DecorationView _selected;
    private Camera _cam;
    private RectTransform _removeRect;
    private Canvas _canvas;

    private void Awake()
    {
        _cam = Camera.main;
        if (removeButton != null)
        {
            _removeRect = (RectTransform)removeButton.transform;
            _canvas = removeButton.GetComponentInParent<Canvas>();
            removeButton.onClick.AddListener(RemoveSelected);
            removeButton.gameObject.SetActive(false);
        }

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

    // ── 놓기 ─────────────────────────────────────────────────────────────────

    /// <summary>그 장식품을 들고 놓을 자리를 고르기 시작한다.</summary>
    public void BeginPlacing(CustomizeItemData item)
    {
        if (item == null || item.icon == null || item.category != CustomizeCategory.Decoration) return;
        if (DecorationLayout.IsPlaced(item)) return; // 같은 장식품은 하나만

        Deselect();
        _placingItem = item;
        _preview.sprite = item.icon;
        _preview.transform.localScale = Vector3.one * DecorationLayer.ScaleFor(item);
        _preview.transform.rotation = DecorationLayer.RotationFor(item);        _preview.gameObject.SetActive(true);
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

    private void Update()
    {
        if (IsPlacing) UpdatePlacing();
        else UpdateSelecting();
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

        // UI 위에 있을 때는 놓을 수 없지만 붉게 하지는 않는다(버튼을 누르려는 것일 수 있다).
        _previewValid = CanPlaceAt(_placingItem, _previewPivot);
        _preview.color = _previewValid ? validTint : invalidTint;

        float z;
        if (_previewValid && layer != null)
        {
            // 놓을 수 있는 자리: 실제로 놓았을 때와 같은 계산으로 레이어·순서·z를 정한다(풀에 묻히는 정도가 같다).
            z = layer.ApplyPlacementDepth(_preview, _placingItem, _previewPivot);
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

    /// <summary>그 장식품을 그 자리에 놓을 수 있는가. 화면 안에 다 들어오고 텃밭과 겹치지 않아야 한다.</summary>
    public bool CanPlaceAt(CustomizeItemData item, Vector2 pivot)
    {
        var rect = DecorationLayer.RectAt(item, pivot);
        return InsideView(rect) && !DecorationBlockedArea.OverlapsAny(rect);
    }

    private bool InsideView(Rect rect)
    {
        if (_cam == null || !_cam.orthographic) return true;
        float h = _cam.orthographicSize - screenMargin;
        float w = _cam.orthographicSize * _cam.aspect - screenMargin;
        Vector2 c = _cam.transform.position;
        return rect.xMin >= c.x - w && rect.xMax <= c.x + w && rect.yMin >= c.y - h && rect.yMax <= c.y + h;
    }

    // ── 선택·치우기 ──────────────────────────────────────────────────────────

    private void UpdateSelecting()
    {
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUI()) return;
        var hit = layer != null ? layer.HitTest(MouseWorld()) : null;
        if (hit != null) Select(hit);
        else Deselect();
    }

    private void Select(DecorationView view)
    {
        _selected = view;
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(true);
            PlaceRemoveButton();
        }
    }

    private void Deselect()
    {
        _selected = null;
        if (removeButton != null) removeButton.gameObject.SetActive(false);
    }

    private void RemoveSelected()
    {
        if (_selected == null) return;
        var placement = _selected.Placement;
        Deselect();
        DecorationLayout.Remove(placement);
    }

    // 배치가 바뀌면 장식품을 전부 다시 만들므로, 들고 있던 선택은 버린다.
    private void OnLayoutChanged() => Deselect();

    private void LateUpdate()
    {
        if (_selected == null) return;
        PlaceRemoveButton();
    }

    private void PlaceRemoveButton()
    {
        if (_removeRect == null || _selected == null || _cam == null) return;

        var rect = _selected.Rect;
        Vector3 screen = _cam.WorldToScreenPoint(new Vector3(rect.xMax, rect.yMax, 0f));
        var parent = (RectTransform)_removeRect.parent;
        var uiCam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out var local))
            _removeRect.localPosition = local + removeButtonOffset;
    }

    // ── 공용 ─────────────────────────────────────────────────────────────────

    private Vector2 MouseWorld()
    {
        if (_cam == null) _cam = Camera.main;
        return _cam != null ? (Vector2)_cam.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;
    }

    private static bool IsPointerOverUI()
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
