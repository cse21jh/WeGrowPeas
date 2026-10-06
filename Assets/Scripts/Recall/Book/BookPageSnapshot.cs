using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace WeGrowPeas.RecallBook
{
    /// <summary>Captures a live UI page (or a supplied preview root), including TMP and UI masks.</summary>
    [AddComponentMenu("UI/Recall Book/Page Snapshot")]
    public sealed class BookPageSnapshot : MonoBehaviour
    {
        [Tooltip("Capture width per page, not the entire open book.")]
        [SerializeField, Range(512, 4096)] private int textureWidth = 1024;
        [SerializeField, Range(0, 31)] private int captureLayer = 5;
        private GameObject stage;
        private RectTransform stageRect;
        private Camera captureCamera;
        private Canvas stageCanvas;

        public void Capture(RectTransform spread, ref RenderTexture destination)
        {
            if (spread == null || !spread.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Book snapshot requires an active page root.");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(spread);
            var size = spread.rect.size;
            if (size.x <= 0 || size.y <= 0) throw new InvalidOperationException("Book page has zero size.");
            EnsureStage();
            int width = Mathf.Clamp(textureWidth, 512, SystemInfo.maxTextureSize);
            int height = Mathf.Clamp(Mathf.RoundToInt(width * size.y / size.x), 1, SystemInfo.maxTextureSize);
            if (destination == null || destination.width != width || destination.height != height)
            {
                Release(ref destination);
                destination = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Recall Book Snapshot", antiAliasing = 1, filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
                };
                destination.Create();
            }

            // Do not clone gameplay components: move the live UI for this synchronous render,
            // and restore all transform/layer state before Unity can draw the main frame.
            var parent = spread.parent;
            int sibling = spread.GetSiblingIndex();
            var anchorMin = spread.anchorMin;
            var anchorMax = spread.anchorMax;
            var pivot = spread.pivot;
            var sizeDelta = spread.sizeDelta;
            var position = spread.anchoredPosition3D;
            var rotation = spread.localRotation;
            var scale = spread.localScale;
            var transforms = spread.GetComponentsInChildren<Transform>(true);
            var layers = new int[transforms.Length];
            for (int i = 0; i < transforms.Length; i++) layers[i] = transforms[i].gameObject.layer;
            var ownerCanvas = spread.GetComponentInParent<Canvas>();
            stageCanvas.additionalShaderChannels = ownerCanvas != null ? ownerCanvas.additionalShaderChannels : AdditionalCanvasShaderChannels.None;
            stageRect.sizeDelta = size;
            // Canvas batches are culled using the root Canvas layer, not just Graphic layers.
            stage.layer = captureLayer;
            captureCamera.orthographicSize = size.y * 0.5f;
            captureCamera.aspect = size.x / size.y;
            captureCamera.cullingMask = 1 << captureLayer;

            try
            {
                spread.SetParent(stageRect, false);
                spread.anchorMin = spread.anchorMax = spread.pivot = new Vector2(0.5f, 0.5f);
                spread.sizeDelta = size;
                spread.anchoredPosition3D = Vector3.zero;
                spread.localRotation = Quaternion.identity;
                spread.localScale = Vector3.one;
                foreach (var t in transforms) t.gameObject.layer = captureLayer;
                Canvas.ForceUpdateCanvases();
                captureCamera.targetTexture = destination;
                if (GraphicsSettings.currentRenderPipeline == null) captureCamera.Render();
                else
                {
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = destination };
                    if (!RenderPipeline.SupportsRenderRequest(captureCamera, request))
                        throw new NotSupportedException("The active render pipeline does not support a book snapshot request.");
                    RenderPipeline.SubmitRenderRequest(captureCamera, request);
                }
            }
            finally
            {
                captureCamera.targetTexture = null;
                spread.SetParent(parent, false);
                spread.SetSiblingIndex(sibling);
                spread.anchorMin = anchorMin;
                spread.anchorMax = anchorMax;
                spread.pivot = pivot;
                spread.sizeDelta = sizeDelta;
                spread.anchoredPosition3D = position;
                spread.localRotation = rotation;
                spread.localScale = scale;
                for (int i = 0; i < transforms.Length; i++)
                    if (transforms[i] != null) transforms[i].gameObject.layer = layers[i];
                Canvas.ForceUpdateCanvases();
            }
        }

        private void EnsureStage()
        {
            if (stage != null) return;
            stage = new GameObject("Book Snapshot Stage", typeof(RectTransform), typeof(Canvas));
            // DontSave flags move objects to a hidden scene. Reparenting live UI into that
            // scene invokes OnDisable/OnEnable and can reset its controllers mid-capture.
            stage.hideFlags = HideFlags.HideInHierarchy;
            stageRect = (RectTransform)stage.transform;
            // Kept far outside gameplay cameras; this camera only runs on demand.
            stageRect.position = new Vector3(0, -30000, 0);
            stageCanvas = stage.GetComponent<Canvas>();
            stageCanvas.renderMode = RenderMode.WorldSpace;
            var cameraObject = new GameObject("Book Snapshot Camera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideInHierarchy;
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -100);
            captureCamera = cameraObject.GetComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.orthographic = true;
            captureCamera.nearClipPlane = 0.1f;
            captureCamera.farClipPlane = 200;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.clear;
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureCamera.useOcclusionCulling = false;
            var data = captureCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            stageCanvas.worldCamera = captureCamera;
        }

        public static void Release(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
            texture = null;
        }

        private void OnDestroy()
        {
            if (stage == null) return;
            if (Application.isPlaying) Destroy(stage);
            else DestroyImmediate(stage);
        }
    }
}
