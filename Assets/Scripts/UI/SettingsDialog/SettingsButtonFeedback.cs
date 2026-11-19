using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeGrowPeas.UI
{
    /// <summary>Combines hover and click scales on artwork, leaving the button hit area fixed.</summary>
    [DisallowMultipleComponent]
    public sealed class SettingsButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform artwork;
        [Header("마우스 오버 확대")]
        [SerializeField] private bool animateHover = true;
        [SerializeField, Min(1)] private float hoverScale = 1.045f;
        [SerializeField, Min(0)] private float hoverDuration = 0.12f;
        [SerializeField] private AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Header("클릭 펀치")]
        [SerializeField, Min(0.01f)] private float punchDuration = 0.2f;
        [Tooltip("X: 시간 0~1, Y: 배율. 1 → 0.94 → 1.045 → 1로 눌렸다가 복원됩니다.")]
        [SerializeField] private AnimationCurve punchCurve = new AnimationCurve(
            new Keyframe(0, 1, 0, 0), new Keyframe(0.22f, 0.94f, 0, 0),
            new Keyframe(0.6f, 1.045f, 0, 0), new Keyframe(1, 1, 0, 0));

        private Tween hoverTween, punchTween;
        private Vector3 restScale;
        private float hoverFactor = 1, punchFactor = 1;
        private bool pointerInside, hovered;

        private void OnEnable()
        {
            if (button == null || artwork == null) return;
            restScale = artwork.localScale;
            button.onClick.AddListener(Punch);
        }

        public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; UpdateHover(); }
        public void OnPointerExit(PointerEventData eventData) { pointerInside = false; UpdateHover(); }
        private void Update() => UpdateHover();

        private void UpdateHover()
        {
            if (button == null || artwork == null) return;
            bool next = animateHover && pointerInside && button.IsInteractable();
            if (hovered == next) return;
            hovered = next;
            hoverTween?.Kill();
            hoverTween = DOTween.To(() => hoverFactor, value =>
            {
                hoverFactor = value; ApplyScale();
            }, hovered ? hoverScale : 1, hoverDuration).SetEase(hoverCurve).SetUpdate(true);
        }

        private void Punch()
        {
            if (!isActiveAndEnabled || artwork == null) return;
            // onClick also covers keyboard submit. Closing can already have disabled interaction here.
            punchTween?.Kill();
            float start = punchFactor, progress = 0;
            float curveStart = punchCurve.Evaluate(0);
            punchTween = DOTween.To(() => progress, value =>
            {
                progress = value;
                // Restart a rapid click from the visible scale without accumulating enlargement.
                punchFactor = punchCurve.Evaluate(value) + (start - curveStart) * (1 - value);
                ApplyScale();
            }, 1, punchDuration).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
            {
                punchFactor = 1; ApplyScale(); punchTween = null;
            });
        }

        private void ApplyScale() => artwork.localScale = restScale * (hoverFactor * punchFactor);

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(Punch);
            hoverTween?.Kill(); punchTween?.Kill(); hoverTween = punchTween = null;
            pointerInside = hovered = false;
            hoverFactor = punchFactor = 1;
            if (artwork != null) artwork.localScale = restScale;
        }
    }
}
