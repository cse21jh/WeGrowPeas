using System;
using DG.Tweening;
using UnityEngine;

namespace WeGrowPeas.UI
{
    /// <summary>One reversible, unscaled clock drives the complete modal's fade and vertical movement.</summary>
    public sealed class SettingsDialogMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform window;
        [SerializeField] private CanvasGroup fadeGroup;
        [Header("열기 · 닫기")]
        [SerializeField, Min(0)] private float duration = 0.28f;
        [SerializeField, Min(0)] private float slideDistance = 90f;
        [Tooltip("X: 열림 정도 0~1, Y: 위로 이동한 비율. 닫을 때는 같은 커브를 거꾸로 재생합니다.")]
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("X: 열림 정도 0~1, Y: 창 전체의 알파.")]
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private Vector2 home;
        private float progress;
        private bool initialized, targetOpen;
        private Tween animation;
        public float Progress => progress;
        public bool IsAnimating => animation != null && animation.IsActive();
        public bool IsClosing => IsAnimating && !targetOpen;

        private void Initialize()
        {
            if (initialized) return;
            home = window.anchoredPosition; initialized = true;
            Apply(0);
        }
        public void Open() => Play(true, null);
        public void Close(Action completed) => Play(false, completed);
        private void Play(bool opening, Action completed)
        {
            Initialize();
            if (IsAnimating && targetOpen == opening) return;
            animation?.Kill(); animation = null; targetOpen = opening;
            float target = opening ? 1 : 0;
            fadeGroup.interactable = false;
            // The transparent dimmer must continue to block the game until closing finishes.
            fadeGroup.blocksRaycasts = true;
            float seconds = Mathf.Max(0, duration) * Mathf.Abs(target - progress);
            if (seconds <= 0.001f || !isActiveAndEnabled)
            { Apply(target); fadeGroup.interactable = opening; completed?.Invoke(); return; }
            animation = DOTween.To(() => progress, Apply, target, seconds)
                .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this)
                .OnComplete(() => { animation = null; fadeGroup.interactable = opening; completed?.Invoke(); });
        }
        private void Apply(float value)
        {
            progress = Mathf.Clamp01(value);
            float move = moveCurve != null && moveCurve.length > 0 ? moveCurve.Evaluate(progress) : progress;
            float alpha = alphaCurve != null && alphaCurve.length > 0 ? alphaCurve.Evaluate(progress) : progress;
            window.anchoredPosition = home + Vector2.down * slideDistance * (1 - Mathf.Clamp01(move));
            fadeGroup.alpha = Mathf.Clamp01(alpha);
        }
        public void ResetClosed()
        {
            animation?.Kill(); animation = null;
            if (initialized) { Apply(0); fadeGroup.interactable = false; }
        }
        private void OnDisable() => ResetClosed();
    }
}
