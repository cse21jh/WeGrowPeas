using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws the old tutorial's dashed border around live UI or world targets.</summary>
internal sealed class TutorialHighlights
{
    private const int SortingOrder = 110;
    private const float Padding = 6f;

    private readonly Image prefab;
    private readonly RectTransform layer;
    private readonly List<Image> markers = new();
    private readonly Vector3[] uiCorners = new Vector3[4];
    private int used;

    public TutorialHighlights(Image prefab, RectTransform parent)
    {
        this.prefab = prefab;
        var root = new GameObject("TutorialHighlights", typeof(RectTransform), typeof(Canvas));
        layer = (RectTransform)root.transform;
        layer.SetParent(parent, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;

        var canvas = root.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
    }

    public void BeginFrame() => used = 0;

    public void EndFrame()
    {
        for (int i = used; i < markers.Count; i++)
            if (markers[i].enabled) markers[i].enabled = false;
    }

    public void Dispose()
    {
        if (layer == null) return;
        layer.gameObject.SetActive(false);
        Object.Destroy(layer.gameObject);
    }

    public void Show(RectTransform target)
    {
        if (TryGetUiBounds(target, out Rect bounds)) Draw(bounds);
    }

    public void Show(IReadOnlyList<RectTransform> targets)
    {
        bool found = false;
        Rect combined = default;
        for (int i = 0; i < targets.Count; i++)
        {
            if (!TryGetUiBounds(targets[i], out Rect bounds)) continue;
            combined = found ? Rect.MinMaxRect(
                Mathf.Min(combined.xMin, bounds.xMin), Mathf.Min(combined.yMin, bounds.yMin),
                Mathf.Max(combined.xMax, bounds.xMax), Mathf.Max(combined.yMax, bounds.yMax)) : bounds;
            found = true;
        }
        if (found) Draw(combined);
    }

    public void Show(Collider target)
    {
        var camera = Camera.main;
        if (camera == null || target == null || !target.enabled || !target.gameObject.activeInHierarchy) return;

        Bounds worldBounds = target.bounds;
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;
        Vector3 lower = camera.WorldToScreenPoint(new Vector3(min.x, min.y, worldBounds.center.z));
        Vector3 upper = camera.WorldToScreenPoint(new Vector3(max.x, max.y, worldBounds.center.z));
        if (lower.z <= 0f || upper.z <= 0f) return;

        Draw(Rect.MinMaxRect(Mathf.Min(lower.x, upper.x), Mathf.Min(lower.y, upper.y),
            Mathf.Max(lower.x, upper.x), Mathf.Max(lower.y, upper.y)));
    }

    private bool TryGetUiBounds(RectTransform target, out Rect bounds)
    {
        bounds = default;
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isActiveAndEnabled) return false;
        var root = canvas.rootCanvas;
        var camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

        target.GetWorldCorners(uiCorners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, uiCorners[0]);
        Vector2 max = min;
        for (int i = 1; i < uiCorners.Length; i++)
        {
            var point = RectTransformUtility.WorldToScreenPoint(camera, uiCorners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return bounds.width > 1f && bounds.height > 1f;
    }

    private void Draw(Rect screen)
    {
        if (!screen.Overlaps(new Rect(0f, 0f, Screen.width, Screen.height))) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen.min, null, out Vector2 min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen.max, null, out Vector2 max))
            return;

        if (used == markers.Count)
        {
            var marker = Object.Instantiate(prefab, layer);
            marker.name = "TutorialDashedBorder";
            marker.raycastTarget = false;
            marker.maskable = false;
            // This layer positions the marker, including for moving plants and animated UI.
            if (marker.TryGetComponent(out WhiteCircle legacyTracking)) legacyTracking.enabled = false;
            markers.Add(marker);
        }

        var image = markers[used++];
        var rect = image.rectTransform;
        Vector3 position = (min + max) * 0.5f;
        Vector2 size = max - min + Vector2.one * (2f * Padding);
        if (rect.localPosition != position) rect.localPosition = position;
        if (rect.sizeDelta != size) rect.sizeDelta = size;
        if (!image.enabled) image.enabled = true;
    }
}
