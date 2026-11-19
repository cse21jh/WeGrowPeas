using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeGrowPeas.UI
{
    /// <summary>Animates only the artwork so rotation and enlargement never move the pointer hit area.</summary>
    public sealed class SettingsCloseButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image artwork;
        [SerializeField] private Sprite normalSprite, hoverSprite;
        [Header("마우스 오버 크기")]
        [SerializeField, Min(1)] private float hoverScale = 1.08f;
        [SerializeField, Min(0)] private float scaleDuration = 0.12f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Header("마우스 진입 · 이탈마다 한 바퀴")]
        [SerializeField, Min(0.01f)] private float spinDuration = 0.32f;
        [Tooltip("회전 속도 커브: X는 시간 0~1, Y는 상대 속도. 면적을 정규화해 항상 정확히 360도 회전합니다.")]
        [SerializeField] private AnimationCurve spinSpeed = new AnimationCurve(
            new Keyframe(0, 0, 0, 0), new Keyframe(0.5f, 1, 0, 0), new Keyframe(1, 0, 0, 0));
        private Tween scaleTween, spinTween;
        private bool pointerInside, hovered;
        private int pendingSpins;
        private float spinTime;
        private readonly float[] accumulatedSpeed = new float[129];
        public bool IsHovered => hovered;
        public bool IsSpinning => spinTween != null && spinTween.IsActive();

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(StopSpin);
        }
        public void OnPointerEnter(PointerEventData eventData) { pointerInside = true; UpdateHover(); }
        public void OnPointerExit(PointerEventData eventData) { pointerInside = false; UpdateHover(); }
        private void Update() => UpdateHover();
        private void UpdateHover()
        {
            bool interactable = button.IsInteractable();
            // Closing disables interaction; this is not a pointer exit that should start a spin.
            if (!interactable) StopSpin();
            bool next = pointerInside && interactable;
            if (hovered == next) return;
            hovered = next; artwork.sprite = hovered ? hoverSprite : normalSprite;
            scaleTween?.Kill();
            scaleTween = artwork.rectTransform.DOScale(hovered ? hoverScale : 1, scaleDuration).SetUpdate(true).SetEase(scaleCurve);
            if (!interactable) return;
            pendingSpins++;
            if (!IsSpinning) BeginSpin();
        }
        private void StopSpin()
        {
            spinTween?.Kill(); spinTween = null; pendingSpins = 0;
            if (artwork != null) artwork.rectTransform.localRotation = Quaternion.identity;
        }
        private void BeginSpin()
        {
            pendingSpins--; spinTime = 0;
            BuildSpeedIntegral();
            spinTween = DOTween.To(() => spinTime, t =>
            {
                spinTime = t;
                artwork.rectTransform.localRotation = Quaternion.Euler(0, 0, -360 * RotationProgress(t));
            }, 1, Mathf.Max(0.01f, spinDuration)).SetEase(Ease.Linear).SetUpdate(true)
                .OnComplete(() =>
                {
                    spinTween = null; artwork.rectTransform.localRotation = Quaternion.identity;
                    if (pendingSpins > 0) BeginSpin();
                });
        }
        private void BuildSpeedIntegral()
        {
            accumulatedSpeed[0] = 0;
            for (int i = 1; i < accumulatedSpeed.Length; i++)
            {
                float a = (i - 1f) / 128, b = i / 128f;
                float sa = spinSpeed != null ? Mathf.Max(0, spinSpeed.Evaluate(a)) : 1;
                float sb = spinSpeed != null ? Mathf.Max(0, spinSpeed.Evaluate(b)) : 1;
                accumulatedSpeed[i] = accumulatedSpeed[i - 1] + (sa + sb) * 0.5f / 128;
            }
            float total = accumulatedSpeed[128];
            for (int i = 0; i <= 128; i++) accumulatedSpeed[i] = total > 0.00001f ? accumulatedSpeed[i] / total : Mathf.SmoothStep(0, 1, i / 128f);
        }
        private float RotationProgress(float time)
        {
            float sample = Mathf.Clamp01(time) * 128;
            int index = Mathf.Min(127, Mathf.FloorToInt(sample));
            return Mathf.Lerp(accumulatedSpeed[index], accumulatedSpeed[index + 1], sample - index);
        }
        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(StopSpin);
            scaleTween?.Kill(); scaleTween = null; StopSpin();
            pointerInside = hovered = false;
            if (artwork == null) return;
            artwork.sprite = normalSprite; artwork.rectTransform.localScale = Vector3.one;
            artwork.rectTransform.localRotation = Quaternion.identity;
        }
    }
}
