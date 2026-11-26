using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Only the speech popup fades around the mouse; navigation stays readable and clickable.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class SpeechBubblePointerFade : MonoBehaviour, ICanvasRaycastFilter
{
    [Header("Mouse Transparency")]
    [Tooltip("Canvas 단위의 투명 영역 반경. 해상도가 달라져도 UI에 대한 비율을 유지합니다.")]
    [SerializeField, Min(0f)] private float radius = 48f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.12f;
    [Tooltip("투명해진 본문을 통해 뒤쪽 게임 오브젝트를 클릭할 수 있게 합니다. 이동 버튼은 항상 클릭 가능합니다.")]
    [SerializeField] private bool passPointerThrough = true;

    [Header("Opaque Navigation")]
    [SerializeField] private RectTransform[] protectedButtons;
    [SerializeField, Min(0f)] private float buttonPadding = 4f;
    [Tooltip("1이면 이동 버튼 주변을 캡슐 모양으로 보호합니다.")]
    [SerializeField, Range(0f, 1f)] private float protectionRoundness = 1f;

    [Header("Shaders")]
    [SerializeField] private Shader spriteShader;
    [SerializeField] private Shader textShader;

    private readonly List<SpeechBubbleFadeGraphic> graphics = new List<SpeechBubbleFadeGraphic>();
    private readonly Vector3[] corners = new Vector3[4];
    private readonly Vector4[] protectedRects = new Vector4[3];
    private Canvas popupCanvas;
    private MandatoryMessagePopupController popup;
    private Vector2 pointerPosition;
    private float fadeAmount;
    private float pixelScale = 1f;
    private bool pointerAvailable;
    private float MinimumOpacity => 1f - Mathf.Clamp01(DeviceSettings.Data.speechBubblePointerTransparency);

    private static readonly int PointerId = Shader.PropertyToID("_SpeechPointer");
    private static readonly int AmountId = Shader.PropertyToID("_SpeechFadeAmount");
    private static readonly int OpacityId = Shader.PropertyToID("_SpeechMinimumOpacity");
    private static readonly int RoundnessId = Shader.PropertyToID("_SpeechProtectionRoundness");
    private static readonly int[] RectIds = {
        Shader.PropertyToID("_SpeechProtectedRect0"),
        Shader.PropertyToID("_SpeechProtectedRect1"),
        Shader.PropertyToID("_SpeechProtectedRect2")
    };

    private void OnEnable()
    {
        popupCanvas = GetComponent<Canvas>();
        popup = GetComponent<MandatoryMessagePopupController>();
        fadeAmount = 0f;
        pointerAvailable = false;
        EnsureGraphics();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        Canvas.willRenderCanvases += UpdateMaterials;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        Canvas.willRenderCanvases -= UpdateMaterials;
        fadeAmount = 0f;
        pointerAvailable = false;
        foreach (var graphic in graphics)
            if (graphic != null) graphic.enabled = false;
    }

    private void LateUpdate()
    {
        // A touch must not leave a permanent mouse-shaped hole on mobile.
        Vector2 mouse = Input.mousePosition;
        bool available = Application.isFocused && Input.mousePresent && Input.touchCount == 0 &&
            mouse.x >= 0 && mouse.y >= 0 && mouse.x <= Screen.width && mouse.y <= Screen.height;
        UpdatePointer(mouse, available);
    }

    private void UpdatePointer(Vector2 position, bool available)
    {
        pointerPosition = position;
        pointerAvailable = available && radius > 0f && MinimumOpacity < 1f;
        float target = pointerAvailable ? 1f : 0f;
        fadeAmount = fadeDuration <= 0f ? target : Mathf.MoveTowards(
            fadeAmount, target, Time.unscaledDeltaTime / fadeDuration);
        UpdateMaterials();
    }

    private void OnTextChanged(Object changed)
    {
        if (changed is TMP_Text text && text.transform.IsChildOf(transform))
            EnsureGraphics(); // Includes fallback-font / inline-sprite submeshes created by TMP.
    }

    private bool IsProtected(Transform child)
    {
        if (protectedButtons == null) return false;
        foreach (var button in protectedButtons)
            if (button != null && (child == button || child.IsChildOf(button))) return true;
        return false;
    }

    private void EnsureGraphics()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (IsProtected(graphic.transform)) continue;
            bool isText = graphic is TMP_Text ||
                (graphic is TMP_SubMeshUI subMesh && subMesh.spriteAsset == null);
            var shader = isText ? textShader : spriteShader;
            if (shader == null) continue;
            if (!graphic.TryGetComponent<SpeechBubbleFadeGraphic>(out var modifier))
            {
                modifier = graphic.gameObject.AddComponent<SpeechBubbleFadeGraphic>();
                modifier.hideFlags = HideFlags.DontSave;
            }
            modifier.Configure(this, shader);
            modifier.enabled = true;
            if (!graphics.Contains(modifier)) graphics.Add(modifier);
        }
    }

    private void UpdateMaterials()
    {
        if (!isActiveAndEnabled || popupCanvas == null) return;
        var rootCanvas = popupCanvas.rootCanvas;
        pixelScale = Mathf.Max(0.001f, rootCanvas.scaleFactor);
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        for (int i = 0; i < protectedRects.Length; i++)
        {
            protectedRects[i] = Vector4.zero;
            if (protectedButtons == null || i >= protectedButtons.Length) continue;
            var button = protectedButtons[i];
            if (button == null || !button.gameObject.activeInHierarchy) continue;
            button.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = min;
            for (int j = 1; j < corners.Length; j++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[j]);
                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }
            float padding = buttonPadding * pixelScale;
            protectedRects[i] = new Vector4(min.x - padding, min.y - padding, max.x + padding, max.y + padding);
        }
        foreach (var graphic in graphics)
            if (graphic != null) graphic.RefreshProperties();
    }

    internal void ApplyProperties(Material material)
    {
        material.SetVector(PointerId, new Vector4(pointerPosition.x, pointerPosition.y,
            radius * pixelScale, 0f));
        material.SetFloat(AmountId, fadeAmount);
        material.SetFloat(OpacityId, MinimumOpacity);
        material.SetFloat(RoundnessId, protectionRoundness);
        for (int i = 0; i < protectedRects.Length; i++) material.SetVector(RectIds[i], protectedRects[i]);
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // The first click belongs to the current line, even inside the transparent area.
        if (popup != null && popup.IsTyping) return true;
        if (!isActiveAndEnabled || !passPointerThrough || !pointerAvailable ||
            fadeAmount < 0.99f || MinimumOpacity >= 0.999f) return true;
        foreach (var rect in protectedRects)
            if (rect.z > rect.x && rect.w > rect.y && screenPoint.x >= rect.x &&
                screenPoint.y >= rect.y && screenPoint.x <= rect.z && screenPoint.y <= rect.w) return true;
        return Vector2.Distance(screenPoint, pointerPosition) > radius * pixelScale;
    }
}
