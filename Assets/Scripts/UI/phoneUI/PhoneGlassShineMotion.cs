using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Per-Image curve animation for UI/Phone Glass Shine, including masked UI.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/Phone Glass Shine Motion")]
public sealed class PhoneGlassShineMotion : UIBehaviour, IMaterialModifier
{
    [Header("Slide Motion / 광택 이동")]
    [Tooltip("광택이 한 번 순환하는 시간(초). 커브 사용 중에는 머터리얼의 Travel Speed 대신 이 값을 사용합니다.")]
    [SerializeField, Min(0.01f)] private float cycleDuration = 10f;

    [Tooltip("X: 시간 0~1, Y: 이동 진행도 0~1. 기울기가 가파르면 빠르게, 평평하면 멈춥니다. " +
             "자연스럽게 반복하려면 시작 (0,0), 끝 (1,1)을 유지하세요.")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("게임이 일시정지(Time.timeScale = 0)되어도 광택을 움직입니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("Play 모드 밖에서도 커브를 미리 봅니다. 공유 머터리얼 에셋은 변경하지 않습니다.")]
    [SerializeField] private bool previewInEditMode = true;

    private static readonly int UseCurveId = Shader.PropertyToID("_UseCurve");
    private static readonly int CurvePhaseId = Shader.PropertyToID("_CurvePhase");
    private Image image;
    private Material renderMaterial;
    private double elapsed;
#if UNITY_EDITOR
    private double lastEditorTime;
#endif

    private bool ShouldAnimate => IsActive() && (Application.IsPlaying(gameObject) || previewInEditMode);

    protected override void OnEnable()
    {
        base.OnEnable();
        image = GetComponent<Image>();
        elapsed = 0;
#if UNITY_EDITOR
        lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
        UnityEditor.EditorApplication.update -= TickEditor;
        UnityEditor.EditorApplication.update += TickEditor;
#endif
        image.SetMaterialDirty();
    }

    private void Update()
    {
        if (!ShouldAnimate || !Application.IsPlaying(gameObject)) return;
        Advance(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
    }

#if UNITY_EDITOR
    private void TickEditor()
    {
        if (!ShouldAnimate || Application.IsPlaying(gameObject)) return;
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        double delta = now - lastEditorTime;
        if (delta < 1d / 30d) return;
        lastEditorTime = now;
        Advance(delta);
        // ExecuteAlways.Update alone only ticks when the editor scene changes.
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    private void Advance(double delta)
    {
        double duration = Mathf.Max(0.01f, cycleDuration);
        elapsed = (elapsed + System.Math.Max(0, delta)) % duration;
        ApplyProgress();
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!ShouldAnimate || baseMaterial == null || !baseMaterial.HasProperty(UseCurveId))
        {
            ReleaseMaterial();
            return baseMaterial;
        }

        if (renderMaterial == null || renderMaterial.shader != baseMaterial.shader)
        {
            ReleaseMaterial();
            renderMaterial = new Material(baseMaterial)
            {
                name = baseMaterial.name + " (Curve)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        // Image's stencil modifier runs first. Copy its final properties so
        // Mask nesting and other Images sharing the asset remain independent.
        renderMaterial.CopyPropertiesFromMaterial(baseMaterial);
        ApplyProgress();
        return renderMaterial;
    }

    private void ApplyProgress()
    {
        if (renderMaterial == null) return;
        float t = (float)(elapsed / Mathf.Max(0.01f, cycleDuration));
        float progress = movementCurve != null && movementCurve.length > 0 ? movementCurve.Evaluate(t) : t;
        renderMaterial.SetFloat(UseCurveId, 1);
        renderMaterial.SetFloat(CurvePhaseId, progress);
    }

    protected override void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= TickEditor;
#endif
        if (image != null) image.SetMaterialDirty();
        ReleaseMaterial();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        ReleaseMaterial();
        base.OnDestroy();
    }

    private void ReleaseMaterial()
    {
        if (renderMaterial == null) return;
        if (Application.isPlaying) Destroy(renderMaterial);
        else DestroyImmediate(renderMaterial);
        renderMaterial = null;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        cycleDuration = Mathf.Max(0.01f, cycleDuration);
        elapsed %= cycleDuration;
        lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.SetMaterialDirty();
    }
#endif
}
