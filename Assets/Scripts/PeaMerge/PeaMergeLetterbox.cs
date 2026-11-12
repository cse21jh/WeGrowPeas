using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.PeaMerge
{
    /// <summary>Four plain panels cover pixels outside the portrait viewport, including safe-area insets.</summary>
    public sealed class PeaMergeLetterbox : MonoBehaviour
    {
        public RectTransform portraitRoot;
        public RectTransform barsRoot;
        private readonly RectTransform[] bars = new RectTransform[4];
        private readonly Vector3[] corners = new Vector3[4];

        private void Awake()
        {
            for (int i = 0; i < bars.Length; i++)
            {
                var image = new GameObject("Letterbox " + i, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(barsRoot, false);
                image.color = new Color(0.12f, 0.15f, 0.13f);
                image.raycastTarget = true;
                bars[i] = image.rectTransform;
            }
        }

        private void LateUpdate()
        {
            portraitRoot.GetWorldCorners(corners);
            float left = corners[0].x;
            float bottom = corners[0].y;
            float right = corners[2].x;
            float top = corners[2].y;
            Place(bars[0], new Rect(0, 0, left, Screen.height));
            Place(bars[1], new Rect(right, 0, Screen.width - right, Screen.height));
            Place(bars[2], new Rect(left, 0, right - left, bottom));
            Place(bars[3], new Rect(left, top, right - left, Screen.height - top));
        }

        private static void Place(RectTransform bar, Rect pixels)
        {
            bar.sizeDelta = new Vector2(Mathf.Max(0, pixels.width), Mathf.Max(0, pixels.height));
            bar.position = pixels.center;
        }
    }
}
