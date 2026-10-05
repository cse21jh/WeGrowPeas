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
        private RenderTexture before, after;
        private Coroutine animation;
        private bool previousInteractable;
        private bool locked;
        public bool IsTurning { get; private set; }
        public bool ReduceMotion { get => reduceMotion; set => reduceMotion = value; }

        /// <summary>Redraw synchronously, once. Destination data is committed even if capture is unavailable.</summary>
        public bool TryTurn(bool forward, Action redrawDestination)
        {
            if (IsTurning || !isActiveAndEnabled || redrawDestination == null) return false;
            if (reduceMotion || pageRoot == null || snapshot == null || turningPage == null ||
                stationaryPage == null || pageMaterial == null)
            {
                redrawDestination();
                onTurnCompleted.Invoke();
                return true;
            }
            IsTurning = true;
            try
            {
                snapshot.Capture(pageRoot, ref before);
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
                snapshot.Capture(pageRoot, ref after);
                stationaryPage.texture = before;
                stationaryPage.uvRect = new Rect(forward ? 0 : 0.5f, 0, 0.5f, 1);
                var rect = stationaryPage.rectTransform;
                rect.anchorMin = new Vector2(forward ? 0 : 0.5f, 0);
                rect.anchorMax = new Vector2(forward ? 0.5f : 1, 1);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                stationaryPage.gameObject.SetActive(true);
                turningPage.gameObject.SetActive(true);
                turningPage.SetSheet(before, after, forward, pageMaterial);
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
                    var rect = castShadow.rectTransform;
                    float edge = 0.5f + Mathf.Cos(t * Mathf.PI) * (forward ? 0.5f : -0.5f);
                    rect.anchorMin = new Vector2(Mathf.Max(0, edge - 0.045f * strength), 0);
                    rect.anchorMax = new Vector2(Mathf.Min(1, edge + 0.045f * strength), 1);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                yield return null;
            }
            animation = null;
            Finish();
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
            BookPageSnapshot.Release(ref before);
            BookPageSnapshot.Release(ref after);
        }
    }
}
