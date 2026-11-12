using UnityEngine;

namespace WeGrowPeas.PeaMerge
{
    /// <summary>Fits the authored 1080 x 1920 layout and camera into the same safe rectangle.</summary>
    [ExecuteAlways]
    public sealed class PeaMergePortraitLayout : MonoBehaviour
    {
        public Camera gameCamera;
        public RectTransform portraitRoot;
        private ScreenOrientation previousOrientation;
        private bool changedOrientation;

        private void OnEnable()
        {
            if (Application.isPlaying && Application.isMobilePlatform)
            {
                previousOrientation = Screen.orientation;
                Screen.orientation = ScreenOrientation.Portrait;
                changedOrientation = true;
            }
            Apply();
        }

        private void Update() => Apply();

        public void Apply()
        {
            if (gameCamera == null || portraitRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0, 0, Screen.width, Screen.height);
            float scale = Mathf.Min(safe.width / 1080f, safe.height / 1920f);
            Vector2 size = new Vector2(1080f, 1920f) * scale;
            Vector2 center = safe.center;
            portraitRoot.anchorMin = portraitRoot.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRoot.sizeDelta = new Vector2(1080f, 1920f);
            portraitRoot.anchoredPosition = center - new Vector2(Screen.width, Screen.height) * 0.5f;
            portraitRoot.localScale = new Vector3(scale, scale, 1f);
            gameCamera.rect = new Rect((center.x - size.x * 0.5f) / Screen.width,
                (center.y - size.y * 0.5f) / Screen.height, size.x / Screen.width, size.y / Screen.height);
        }

        private void OnDisable()
        {
            if (changedOrientation) Screen.orientation = previousOrientation;
            changedOrientation = false;
        }
    }
}
