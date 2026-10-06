using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WeGrowPeas.RecallBook
{
    [AddComponentMenu("UI/Recall Book/Page Turner")]
    public sealed class BookPageTurner : MonoBehaviour
    {
        [Tooltip("Only the two paper pages and their contents. Keep cover, rings and navigation outside.")]
        [SerializeField] private RectTransform pageRoot;
        [Tooltip("The actual paper Image RectTransforms. Size and position are read on every turn.")]
        [SerializeField] private RectTransform leftPage;
        [SerializeField] private RectTransform rightPage;
        [SerializeField] private BookPageSnapshot snapshot;
        [SerializeField] private RawImage stationaryPage;
        [SerializeField] private BookPageCurlGraphic turningPage;
        [SerializeField] private Image castShadow;
        [SerializeField] private CanvasGroup pageInteraction;
        [SerializeField] private Material pageMaterial;
        [SerializeField, Range(0.15f, 1.5f)] private float duration = 0.65f;
        [SerializeField] private bool reduceMotion;
        [SerializeField] private UnityEvent onTurnStarted = new UnityEvent();
        [SerializeField] private UnityEvent onTurnCompleted = new UnityEvent();
        private RenderTexture frontSnapshot, backSnapshot, stationarySnapshot;
        private Rect shadowSourceRect, shadowDestinationRect;
        private readonly Vector3[] pageCorners = new Vector3[4];
        private Coroutine animation;
        private bool previousInteractable;
        private bool locked;
        public bool IsTurning { get; private set; }
        public bool ReduceMotion { get => reduceMotion; set => reduceMotion = value; }

        /// <summary>Redraw synchronously, once. Destination data is committed even if capture is unavailable.</summary>
        public bool TryTurn(bool forward, Action redrawDestination)
        {
            if (IsTurning || !isActiveAndEnabled || redrawDestination == null) return false;
            ResolvePages();
            if (reduceMotion || pageRoot == null || leftPage == null || rightPage == null || snapshot == null || turningPage == null ||
                stationaryPage == null || pageMaterial == null)
            {
                redrawDestination();
                onTurnCompleted.Invoke();
                return true;
            }
            IsTurning = true;
            var sourcePage = forward ? rightPage : leftPage;
            var destinationPage = forward ? leftPage : rightPage;
            Rect sourceBounds;
            try
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(pageRoot);
                sourceBounds = PageBounds(sourcePage, turningPage.rectTransform.parent);
                // Capture pages separately: the paper may extend outside Pages, overlap at
                // the spine, or have transparent corners. Never crop it to a fixed half-spread.
                snapshot.Capture(destinationPage, ref stationarySnapshot);
                snapshot.Capture(sourcePage, ref frontSnapshot);
                FitRect(stationaryPage.rectTransform, PageBounds(destinationPage, stationaryPage.rectTransform.parent));
                if (castShadow != null) shadowSourceRect = PageBounds(sourcePage, castShadow.rectTransform.parent);
            }
            catch (Exception exception)
            {
                IsTurning = false;
                Debug.LogWarning("[Recall Book] Capture unavailable; showing the next spread immediately. " + exception.Message, this);
                redrawDestination();
                onTurnCompleted.Invoke();
                return true;
            }
            try
            {
                redrawDestination();
                snapshot.Capture(destinationPage, ref backSnapshot);
                var destinationBounds = PageBounds(destinationPage, turningPage.rectTransform.parent);
                var union = Rect.MinMaxRect(Mathf.Min(sourceBounds.xMin, destinationBounds.xMin),
                    Mathf.Min(sourceBounds.yMin, destinationBounds.yMin), Mathf.Max(sourceBounds.xMax, destinationBounds.xMax),
                    Mathf.Max(sourceBounds.yMax, destinationBounds.yMax));
                var sheetRect = turningPage.rectTransform;
                FitRect(sheetRect, union);
                // FitRect uses a centered pivot and identity rotation/scale in this plane.
                var localSource = new Rect(sourceBounds.position - union.center, sourceBounds.size);
                var localDestination = new Rect(destinationBounds.position - union.center, destinationBounds.size);
                if (castShadow != null) shadowDestinationRect = PageBounds(destinationPage, castShadow.rectTransform.parent);
                stationaryPage.texture = stationarySnapshot;
                stationaryPage.uvRect = new Rect(0, 0, 1, 1);
                stationaryPage.gameObject.SetActive(true);
                turningPage.gameObject.SetActive(true);
                turningPage.SetSheet(frontSnapshot, backSnapshot, forward, pageMaterial, localSource, localDestination);
                turningPage.SetProgress(0);
                if (pageInteraction != null)
                {
                    previousInteractable = pageInteraction.interactable;
                    pageInteraction.interactable = false;
                    locked = true;
                }
                if (castShadow != null) castShadow.gameObject.SetActive(true);
                onTurnStarted.Invoke();
                // A listener may close the book immediately.
                if (isActiveAndEnabled && IsTurning) animation = StartCoroutine(Animate(forward));
            }
            catch (Exception exception)
            {
                Finish();
                Debug.LogException(exception, this);
            }
            return true;
        }

        private IEnumerator Animate(bool forward)
        {
            float elapsed = 0;
            float seconds = Mathf.Max(0.05f, duration);
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / seconds));
                turningPage.SetProgress(t);
                if (castShadow != null)
                {
                    float strength = Mathf.Sin(t * Mathf.PI);
                    castShadow.color = new Color(0.34f, 0.20f, 0.12f, strength * 0.13f);
                    float width = Mathf.Lerp(shadowSourceRect.width, shadowDestinationRect.width, t);
                    float height = Mathf.Lerp(shadowSourceRect.height, shadowDestinationRect.height, t);
                    float spine = Mathf.Lerp(forward ? shadowSourceRect.xMin : shadowSourceRect.xMax,
                        forward ? shadowDestinationRect.xMax : shadowDestinationRect.xMin, t);
                    float edge = spine + Mathf.Cos(t * Mathf.PI) * width * (forward ? 1 : -1);
                    float shadowWidth = width * 0.18f * strength;
                    float centerY = Mathf.Lerp(shadowSourceRect.center.y, shadowDestinationRect.center.y, t);
                    FitRect(castShadow.rectTransform, new Rect(edge - shadowWidth * 0.5f, centerY - height * 0.5f, shadowWidth, height));
                }
                yield return null;
            }
            animation = null;
            Finish();
        }

        private void ResolvePages()
        {
            // Existing prefab instances created before these fields were added still work.
            if (pageRoot == null) return;
            if (leftPage == null) leftPage = pageRoot.Find("LeftPage") as RectTransform;
            if (rightPage == null) rightPage = pageRoot.Find("RightPage") as RectTransform;
        }

        private Rect PageBounds(RectTransform page, Transform relativeTo)
        {
            page.GetWorldCorners(pageCorners);
            Vector2 min = relativeTo.InverseTransformPoint(pageCorners[0]);
            Vector2 max = min;
            for (int i = 1; i < pageCorners.Length; i++)
            {
                Vector2 point = relativeTo.InverseTransformPoint(pageCorners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void FitRect(RectTransform target, Rect bounds)
        {
            target.anchorMin = target.anchorMax = target.pivot = new Vector2(0.5f, 0.5f);
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
            target.sizeDelta = bounds.size;
            target.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0);
        }

        private void Finish()
        {
            bool notify = IsTurning;
            IsTurning = false;
            if (stationaryPage != null) stationaryPage.gameObject.SetActive(false);
            if (turningPage != null) turningPage.gameObject.SetActive(false);
            if (castShadow != null) castShadow.gameObject.SetActive(false);
            if (locked && pageInteraction != null) pageInteraction.interactable = previousInteractable;
            locked = false;
            if (notify) onTurnCompleted.Invoke();
        }

        private void OnDisable()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            Finish();
            if (stationaryPage != null) stationaryPage.texture = null;
            BookPageSnapshot.Release(ref frontSnapshot);
            BookPageSnapshot.Release(ref backSnapshot);
            BookPageSnapshot.Release(ref stationarySnapshot);
        }
    }
}
