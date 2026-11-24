using UnityEngine;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Figma's rounded fill masks a seamless, independently scrolling stripe tile.</summary>
    [DisallowMultipleComponent]
    public sealed class ArchiveProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform rail;
        [SerializeField] private Image fill;
        [SerializeField] private RawImage stripes;
        [SerializeField] private BookPageTurner pageTurner;
        [SerializeField, Min(1)] private float stripeWidth = 18.75f;
        [SerializeField, Tooltip("줄무늬 이동 속도 (UI 단위/초)")] private float scrollSpeed = 24;
        private float value;
        private float offset;

        public float Value => value;

        public void SetProgress(float progress)
        {
            value = Mathf.Clamp01(progress);
            if (fill == null || rail == null || stripes == null) return;
            fill.gameObject.SetActive(value > 0);
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rail.rect.width * value);
            UpdateUV();
        }

        private void Update()
        {
            if (value <= 0 || stripes == null) return;
            if (pageTurner == null || !pageTurner.ReduceMotion)
                offset = Mathf.Repeat(offset - Time.unscaledDeltaTime * scrollSpeed / stripeWidth, 1);
            UpdateUV();
        }

        private void UpdateUV()
        {
            // UV width follows the filled length, so progress never stretches the stripe pitch.
            stripes.uvRect = new Rect(offset, 0, rail.rect.width * value / stripeWidth, 1);
        }
    }
}
