using UnityEngine;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Fits the book's design coordinates into its outer RectTransform, including in Prefab Mode.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Book/Book Root Scaler")]
    public sealed class BookRootScaler : MonoBehaviour
    {
        [Tooltip("The common parent of the cover, paper, binding, content, effects and navigation.")]
        [SerializeField] private RectTransform content;
        [Tooltip("Original design size. Resize the outer RectTransform's Width / Height to resize the book.")]
        [SerializeField] private Vector2 referenceSize = new Vector2(1180, 800);
        [Tooltip("Fit inside Width / Height without distorting the art or text. Disable to stretch independently.")]
        [SerializeField] private bool preserveAspect = true;
        private RectTransform viewport;
        private RectTransform trackedContent;
        private DrivenRectTransformTracker tracker;
        private bool applying;

        public RectTransform Content => content;
        public Vector2 ReferenceSize => referenceSize;

        private void OnEnable() => Refresh();
        private void OnRectTransformDimensionsChange() => Refresh();
        private void LateUpdate() => Refresh();

        private void OnDisable()
        {
            tracker.Clear();
            trackedContent = null;
        }

        public void Refresh()
        {
            if (applying || !isActiveAndEnabled || content == null || content.parent != transform) return;
            if (viewport == null) viewport = (RectTransform)transform;
            applying = true;
            try
            {
                if (trackedContent != content)
                {
                    tracker.Clear();
                    tracker.Add(this, content, DrivenTransformProperties.Anchors | DrivenTransformProperties.Pivot |
                        DrivenTransformProperties.AnchoredPosition3D | DrivenTransformProperties.SizeDelta |
                        DrivenTransformProperties.Scale | DrivenTransformProperties.Rotation);
                    trackedContent = content;
                }
                var design = new Vector2(Mathf.Max(1, referenceSize.x), Mathf.Max(1, referenceSize.y));
                var size = viewport.rect.size;
                float x = Mathf.Max(0, size.x) / design.x;
                float y = Mathf.Max(0, size.y) / design.y;
                if (preserveAspect) x = y = Mathf.Min(x, y);
                var scale = new Vector3(x, y, 1);
                var center = new Vector2(0.5f, 0.5f);
                // Only write changed values to avoid dirtying the editor and rebuilding UI every frame.
                if (content.anchorMin != center) content.anchorMin = center;
                if (content.anchorMax != center) content.anchorMax = center;
                if (content.pivot != center) content.pivot = center;
                if (content.anchoredPosition3D != Vector3.zero) content.anchoredPosition3D = Vector3.zero;
                if (content.localRotation != Quaternion.identity) content.localRotation = Quaternion.identity;
                if (content.sizeDelta != design) content.sizeDelta = design;
                if (content.localScale != scale) content.localScale = scale;
            }
            finally { applying = false; }
        }
    }
}
