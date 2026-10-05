using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Combines a random body and accessory beneath the animated face.</summary>
[DisallowMultipleComponent]
public sealed class StartPeaAppearanceController : MonoBehaviour
{
    [System.Serializable]
    private sealed class Appearance
    {
        public string name;
        public Sprite body;
        public Sprite accessory;
        public Color accessoryTint = Color.white;
        public Vector2 faceOffset;
        [Min(0.01f)] public float faceScale = 3f;
        public Color faceTint = Color.white;
        public GameObject effects;
    }

    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer accessoryRenderer;
    [SerializeField] private SpriteRenderer faceRenderer;
    [Tooltip("얼굴 애니메이션이 위치를 초기화해도 종류별 배치가 유지되도록 하는 부모")]
    [SerializeField] private Transform faceLayout;
    [Tooltip("높이에 따른 크기 전용 부모. Rigidbody와 SquashAxis의 스케일은 변경하지 않습니다.")]
    [SerializeField] private Transform depthScaleRoot;
    [SerializeField] private Appearance[] appearances;

    private Vector3 originalDepthScale;
    private Color originalBodyTint = Color.white;
    private static readonly int DepthAddColorId = Shader.PropertyToID("_DepthAddColor");
    private Color depthAdditive = Color.clear;
    private MaterialPropertyBlock depthPropertyBlock;
    private SortingGroup sortingGroup;
    private Vector2[][] bodyOutline;
    private Vector2[][] accessoryOutline;
    private Bounds occluderBounds;
    private Bounds faceBounds;
    private readonly Vector3[] faceSamples = new Vector3[9];
    private static readonly Dictionary<Sprite, Vector2[][]> OutlineCache = new();
    public int AppearanceIndex { get; private set; } = -1;
    public int TraitCount => appearances == null ? 0 : Mathf.Max(0, appearances.Length - 1);
    public string AppearanceName => AppearanceIndex >= 0 ? appearances[AppearanceIndex].name : string.Empty;
    public bool HasTrait => AppearanceIndex > 0;
    public SortingGroup SortingGroup => sortingGroup;
    public Vector3 FaceCenter => faceBounds.center;

    private void Awake()
    {
        if (depthScaleRoot != null) originalDepthScale = depthScaleRoot.localScale;
        if (bodyRenderer != null) originalBodyTint = bodyRenderer.color;
        sortingGroup = GetComponent<SortingGroup>();
    }

    private void OnEnable()
    {
        if (appearances != null && appearances.Length > 0)
            SetAppearance(0);
        SetFaceVisible(true);
    }

    public void SetRandomTrait()
    {
        if (appearances != null && appearances.Length > 1)
            SetAppearance(Random.Range(1, appearances.Length));
    }

    public void SetAppearance(int index)
    {
        if (appearances == null || index < 0 || index >= appearances.Length ||
            bodyRenderer == null || accessoryRenderer == null || faceRenderer == null || faceLayout == null)
            return;

        Appearance appearance = appearances[index];
        if (appearance == null || appearance.body == null) return;
        AppearanceIndex = index;
        bodyRenderer.sprite = appearance.body;
        accessoryRenderer.sprite = appearance.accessory;
        accessoryRenderer.enabled = appearance.accessory != null;
        faceLayout.localPosition = new Vector3(appearance.faceOffset.x, appearance.faceOffset.y, 0f);
        faceLayout.localScale = new Vector3(appearance.faceScale, appearance.faceScale, 1f);
        ApplyDepthAdditive();
        bodyOutline = GetOutline(appearance.body);
        accessoryOutline = GetOutline(appearance.accessory);

        foreach (Appearance item in appearances)
        {
            if (item != null && item.effects != null)
                item.effects.SetActive(item.effects == appearance.effects);
        }
    }

    public void SetDepthScale(float multiplier)
    {
        if (depthScaleRoot == null) return;
        multiplier = Mathf.Max(0.01f, multiplier);
        depthScaleRoot.localScale = new Vector3(
            originalDepthScale.x * multiplier, originalDepthScale.y * multiplier, originalDepthScale.z);
    }

    public void SetDepthAdditive(Color color, float strength)
    {
        float amount = Mathf.Clamp01(strength);
        Color additive = new Color(Mathf.Max(0f, color.r) * amount,
            Mathf.Max(0f, color.g) * amount, Mathf.Max(0f, color.b) * amount, 0f);
        if (depthAdditive == additive) return;
        depthAdditive = additive;
        ApplyDepthAdditive();
    }

    private void ApplyDepthAdditive()
    {
        ApplyDepthAdditive(bodyRenderer, originalBodyTint);
        if (appearances == null || AppearanceIndex < 0 || AppearanceIndex >= appearances.Length) return;
        Appearance appearance = appearances[AppearanceIndex];
        if (appearance == null) return;
        ApplyDepthAdditive(accessoryRenderer, appearance.accessoryTint);
        ApplyDepthAdditive(faceRenderer, appearance.faceTint);
    }

    private void ApplyDepthAdditive(SpriteRenderer renderer, Color originalTint)
    {
        if (renderer == null) return;
        // Keep authored RGB/alpha intact; the shader adds light after sampling and lighting.
        renderer.color = originalTint;
        if (depthPropertyBlock == null) depthPropertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(depthPropertyBlock);
        depthPropertyBlock.SetVector(DepthAddColorId, (Vector4)depthAdditive);
        renderer.SetPropertyBlock(depthPropertyBlock);
    }

    public void SetFaceVisible(bool visible)
    {
        // Keep the Animator and chosen expression intact while the face is hidden.
        if (faceRenderer != null) faceRenderer.enabled = visible || HasTrait;
    }

    public void PrepareFaceOcclusion()
    {
        if (bodyRenderer == null || faceRenderer == null || faceRenderer.sprite == null) return;
        occluderBounds = bodyRenderer.bounds;
        if (accessoryRenderer != null && accessoryRenderer.enabled && accessoryRenderer.sprite != null)
            occluderBounds.Encapsulate(accessoryRenderer.bounds);

        // The facial features occupy the middle half of the padded animation frames.
        // Compute these from the Transform even when the renderer is disabled.
        Bounds spriteBounds = faceRenderer.sprite.bounds;
        int index = 0;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3 point = spriteBounds.center + new Vector3(
                    x * spriteBounds.extents.x * 0.5f, y * spriteBounds.extents.y * 0.5f, 0f);
                faceSamples[index++] = faceRenderer.transform.TransformPoint(point);
            }
        }
        faceBounds = new Bounds(faceSamples[0], Vector3.zero);
        for (int i = 1; i < faceSamples.Length; i++) faceBounds.Encapsulate(faceSamples[i]);
    }

    public void UpdateFaceVisibility(IReadOnlyList<StartPeaAppearanceController> peas, bool allowPartialFace = false)
    {
        if (HasTrait)
        {
            SetFaceVisible(true);
            return;
        }
        if (allowPartialFace)
        {
            // Regional representatives may be naturally clipped at the edge, but not at the center.
            SetFaceVisible(CountVisibleFaceSamples(peas) >= 5);
            return;
        }
        bool visible = true;
        foreach (StartPeaAppearanceController other in peas)
        {
            if (other == null || other == this || !other.isActiveAndEnabled ||
                !other.IsInFrontOf(this) || !other.CoversFaceOf(this)) continue;
            visible = false;
            break;
        }
        SetFaceVisible(visible);
    }

    public int CountVisibleFaceSamples(IReadOnlyList<StartPeaAppearanceController> peas)
    {
        if (IsFacePointCovered(faceSamples[4], peas)) return 0;
        int visible = 1;
        for (int i = 0; i < faceSamples.Length; i++)
        {
            if (i != 4 && !IsFacePointCovered(faceSamples[i], peas)) visible++;
        }
        return visible;
    }

    private bool IsFacePointCovered(Vector3 point, IReadOnlyList<StartPeaAppearanceController> peas)
    {
        foreach (StartPeaAppearanceController other in peas)
        {
            if (other == null || other == this || !other.isActiveAndEnabled || !other.IsInFrontOf(this)) continue;
            if (point.x < other.occluderBounds.min.x || point.x > other.occluderBounds.max.x ||
                point.y < other.occluderBounds.min.y || point.y > other.occluderBounds.max.y) continue;
            if (ContainsPoint(other.bodyRenderer, other.bodyOutline, point) ||
                ContainsPoint(other.accessoryRenderer, other.accessoryOutline, point)) return true;
        }
        return false;
    }

    public bool CoversFaceOf(StartPeaAppearanceController other)
    {
        if (other == null || other == this || !isActiveAndEnabled ||
            !OverlapsXY(occluderBounds, other.faceBounds)) return false;
        foreach (Vector3 point in other.faceSamples)
        {
            if (ContainsPoint(bodyRenderer, bodyOutline, point) ||
                ContainsPoint(accessoryRenderer, accessoryOutline, point)) return true;
        }
        return false;
    }

    public bool ContainsVisualPoint(Vector2 worldPoint)
    {
        return isActiveAndEnabled &&
            (ContainsPoint(bodyRenderer, bodyOutline, worldPoint) ||
             ContainsPoint(accessoryRenderer, accessoryOutline, worldPoint));
    }

    private bool IsInFrontOf(StartPeaAppearanceController other)
    {
        if (sortingGroup == null || other.sortingGroup == null) return false;
        if (sortingGroup.sortingLayerID != other.sortingGroup.sortingLayerID)
            return SortingLayer.GetLayerValueFromID(sortingGroup.sortingLayerID) >
                SortingLayer.GetLayerValueFromID(other.sortingGroup.sortingLayerID);
        return sortingGroup.sortingOrder > other.sortingGroup.sortingOrder;
    }

    private static bool OverlapsXY(Bounds a, Bounds b)
    {
        return a.min.x <= b.max.x && a.max.x >= b.min.x &&
            a.min.y <= b.max.y && a.max.y >= b.min.y;
    }

    private static Vector2[][] GetOutline(Sprite sprite)
    {
        if (sprite == null) return null;
        if (OutlineCache.TryGetValue(sprite, out Vector2[][] cached)) return cached;
        var shapes = new Vector2[sprite.GetPhysicsShapeCount()][];
        var points = new List<Vector2>();
        for (int i = 0; i < shapes.Length; i++)
        {
            points.Clear();
            sprite.GetPhysicsShape(i, points);
            shapes[i] = points.ToArray();
        }
        OutlineCache[sprite] = shapes;
        return shapes;
    }

    private static bool ContainsPoint(SpriteRenderer renderer, Vector2[][] outline, Vector3 worldPoint)
    {
        if (renderer == null || !renderer.enabled || renderer.sprite == null || outline == null) return false;
        Vector2 point = renderer.transform.InverseTransformPoint(worldPoint);
        if (renderer.flipX) point.x = -point.x;
        if (renderer.flipY) point.y = -point.y;
        Bounds bounds = renderer.sprite.bounds;
        if (point.x < bounds.min.x || point.x > bounds.max.x ||
            point.y < bounds.min.y || point.y > bounds.max.y) return false;
        if (outline.Length == 0) return true;

        foreach (Vector2[] polygon in outline)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            if (inside) return true;
        }
        return false;
    }
}
