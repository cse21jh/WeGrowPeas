using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Closes with a lift and drop, opens in reverse, and fades the surrounding UI colors.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    [AddComponentMenu("UI/Book/Book Close Motion")]
    public sealed class BookCloseMotion : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("움직일 책의 바깥 루트. BookRootScaler가 제어하는 BookContent는 지정하지 않습니다.")]
        [SerializeField] private RectTransform bookRoot;
        [SerializeField] private CanvasGroup interaction;

        [Header("Rise / 위로 튀어 오르기")]
        [Tooltip("부모 UI 좌표 기준 상승 높이입니다.")]
        [SerializeField, Min(0)] private float riseHeight = 50f;
        [SerializeField, Min(0)] private float riseDuration = 0.14f;
        [Tooltip("X: 상승 구간 시간 0~1, Y: 상승 거리 0~1. 시작 (0,0), 끝 (1,1).")]
        [SerializeField] private AnimationCurve riseCurve = new AnimationCurve(
            new Keyframe(0, 0, 2, 2), new Keyframe(1, 1, 0, 0));

        [Header("Drop / 아래로 내려가기")]
        [SerializeField, Min(0)] private float dropDuration = 0.34f;
        [Tooltip("X: 하강 구간 시간 0~1, Y: 하강 거리 0~1. 끝으로 갈수록 가파르면 가속합니다.")]
        [SerializeField] private AnimationCurve dropCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 3, 3));
        [Tooltip("책의 위쪽 끝이 화면 아래로 완전히 내려간 뒤 추가로 이동하는 여백입니다.")]
        [SerializeField, Min(0)] private float offscreenPadding = 40f;

        [Header("Fade / DOTween 색상 알파")]
        [SerializeField] private Graphic backgroundTint;
        [Tooltip("Title 아래의 글자와 기록 개수. 각 Graphic의 원래 RGB와 알파를 보존합니다.")]
        [SerializeField] private Graphic[] titleGraphics = Array.Empty<Graphic>();
        [Tooltip("X: 전체 열기/닫기 시간 0~1, Y: 배경 페이드 진행도 0~1.")]
        [SerializeField] private AnimationCurve tintFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("X: 전체 열기/닫기 시간 0~1, Y: Title 페이드 진행도 0~1.")]
        [SerializeField] private AnimationCurve titleFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 startPosition;
        private float exitOffset;
        private float elapsed;
        private float activeRiseDuration;
        private float activeDropDuration;
        private float activeRiseHeight;
        private bool captured;
        private bool previousInteractable;
        private bool previousBlocksRaycasts;
        private Action completed;
        private Sequence animation;
        private readonly List<Graphic> fadeTargets = new List<Graphic>();
        private readonly List<Color> originalColors = new List<Color>();
        private readonly Vector3[] corners = new Vector3[4];

        public bool IsPlaying { get; private set; }
        public bool IsOpening { get; private set; }
        public bool IsClosing => IsPlaying && !IsOpening;

        private void Reset()
        {
            bookRoot = (RectTransform)transform;
            interaction = GetComponent<CanvasGroup>();
        }

        public bool Play(Action onCompleted) => Begin(false, onCompleted);

        public bool PlayOpen(Action onCompleted = null) => Begin(true, onCompleted);

        private bool Begin(bool opening, Action onCompleted)
        {
            if (IsPlaying || !isActiveAndEnabled) return false;
            if (bookRoot == null) bookRoot = (RectTransform)transform;
            if (interaction == null) interaction = GetComponent<CanvasGroup>();
            Canvas.ForceUpdateCanvases();
            startPosition = bookRoot.anchoredPosition3D;
            exitOffset = CalculateExitOffset();
            activeRiseDuration = Mathf.Max(0, riseDuration);
            activeDropDuration = Mathf.Max(0, dropDuration);
            activeRiseHeight = riseHeight;
            float duration = activeRiseDuration + activeDropDuration;
            previousInteractable = interaction.interactable;
            previousBlocksRaycasts = interaction.blocksRaycasts;
            captured = true;
            interaction.interactable = false;
            interaction.blocksRaycasts = false;
            completed = onCompleted;
            elapsed = 0;
            IsPlaying = true;
            IsOpening = opening;
            CaptureColors();
            ApplyPosition(opening ? duration : 0);
            if (opening)
                foreach (var graphic in fadeTargets)
                {
                    var color = graphic.color;
                    color.a = 0;
                    graphic.color = color;
                }

            if (duration <= 0)
            {
                Complete();
                return true;
            }

            // One independent DOTween clock keeps motion and all color fades in sync, even while paused.
            animation = DOTween.Sequence().Pause().SetUpdate(true).SetEase(Ease.Linear).SetTarget(this);
            animation.Append(DOTween.To(() => elapsed, value =>
            {
                elapsed = value;
                ApplyPosition(opening ? duration - value : value);
            }, duration, duration).SetEase(Ease.Linear));
            for (int i = 0; i < fadeTargets.Count; i++)
            {
                var graphic = fadeTargets[i];
                var curve = graphic == backgroundTint ? tintFadeCurve : titleFadeCurve;
                var fade = graphic.DOFade(opening ? originalColors[i].a : 0, duration);
                if (curve != null && curve.length > 0) fade.SetEase(curve);
                else fade.SetEase(Ease.Linear);
                animation.Join(fade);
            }
            var current = animation;
            animation.OnComplete(Complete).OnKill(() =>
            {
                if (animation != current) return;
                animation = null;
                CancelAndRestore();
            });
            animation.Play();
            return true;
        }

        private void CaptureColors()
        {
            fadeTargets.Clear();
            originalColors.Clear();
            AddFadeTarget(backgroundTint);
            foreach (var graphic in titleGraphics) AddFadeTarget(graphic);
        }

        private void AddFadeTarget(Graphic graphic)
        {
            if (graphic == null || fadeTargets.Contains(graphic)) return;
            fadeTargets.Add(graphic);
            originalColors.Add(graphic.color);
        }

        private float CalculateExitOffset()
        {
            var canvas = bookRoot.GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            var parent = bookRoot.parent;
            if (canvasRect == null || parent == null)
                return -(Mathf.Abs(bookRoot.rect.height * bookRoot.localScale.y) + offscreenPadding);

            bookRoot.GetWorldCorners(corners);
            float bookTop = float.NegativeInfinity;
            foreach (var corner in corners) bookTop = Mathf.Max(bookTop, parent.InverseTransformPoint(corner).y);
            canvasRect.GetWorldCorners(corners);
            float canvasBottom = float.PositiveInfinity;
            foreach (var corner in corners) canvasBottom = Mathf.Min(canvasBottom, parent.InverseTransformPoint(corner).y);
            return Mathf.Min(-offscreenPadding, canvasBottom - bookTop - offscreenPadding);
        }

        private void ApplyPosition(float time)
        {
            float offset;
            if (time >= activeRiseDuration + activeDropDuration) offset = exitOffset;
            else if (time <= 0) offset = 0;
            else if (time < activeRiseDuration)
                offset = activeRiseHeight * Evaluate(riseCurve, time / activeRiseDuration);
            else offset = Mathf.LerpUnclamped(activeRiseHeight, exitOffset,
                Evaluate(dropCurve, (time - activeRiseDuration) / activeDropDuration));
            bookRoot.anchoredPosition3D = startPosition + Vector3.up * offset;
        }

        private void Complete()
        {
            bool opening = IsOpening;
            bookRoot.anchoredPosition3D = startPosition + (opening ? Vector3.zero : Vector3.up * exitOffset);
            animation = null;
            var callback = completed;
            completed = null;
            IsPlaying = false;
            IsOpening = false;
            // On close, hide the owner's panel before restoring its authored position and colors.
            try { callback?.Invoke(); }
            finally { Restore(); }
        }

        private static float Evaluate(AnimationCurve curve, float t) =>
            curve != null && curve.length > 0 ? curve.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);

        public void CancelAndRestore()
        {
            IsPlaying = false;
            IsOpening = false;
            completed = null;
            var current = animation;
            animation = null;
            current?.Kill(false);
            Restore();
        }

        private void Restore()
        {
            if (!captured) return;
            captured = false;
            if (bookRoot != null) bookRoot.anchoredPosition3D = startPosition;
            for (int i = 0; i < fadeTargets.Count; i++)
                if (fadeTargets[i] != null) fadeTargets[i].color = originalColors[i];
            fadeTargets.Clear();
            originalColors.Clear();
            if (interaction != null)
            {
                interaction.interactable = previousInteractable;
                interaction.blocksRaycasts = previousBlocksRaycasts;
            }
        }

        private void OnDisable() => CancelAndRestore();
    }
}
