using UnityEngine;
using UnityEngine.UI;

/// <summary>Per-graphic runtime material, created after uGUI's stencil modifier.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class SpeechBubbleFadeGraphic : MonoBehaviour, IMaterialModifier
{
    private SpeechBubblePointerFade owner;
    private Shader fadeShader;
    private Graphic graphic;
    private Material modifiedMaterial;

    internal void Configure(SpeechBubblePointerFade popup, Shader shader)
    {
        bool changed = owner != popup || fadeShader != shader;
        owner = popup;
        fadeShader = shader;
        if (graphic == null) graphic = GetComponent<Graphic>();
        if (changed) graphic.SetMaterialDirty();
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!isActiveAndEnabled || owner == null || !owner.isActiveAndEnabled || fadeShader == null)
            return baseMaterial;
        if (modifiedMaterial == null)
            modifiedMaterial = new Material(fadeShader) { name = "Speech fade (runtime)", hideFlags = HideFlags.HideAndDontSave };
        // Preserve the original font atlas, user-edited outline, and stencil/clip properties.
        modifiedMaterial.CopyPropertiesFromMaterial(baseMaterial);
        modifiedMaterial.shader = fadeShader;
        owner.ApplyProperties(modifiedMaterial);
        return modifiedMaterial;
    }

    internal void RefreshProperties()
    {
        if (modifiedMaterial != null && owner != null) owner.ApplyProperties(modifiedMaterial);
    }

    private void OnEnable()
    {
        if (graphic == null) graphic = GetComponent<Graphic>();
        graphic.SetMaterialDirty();
    }

    private void OnDisable()
    {
        if (graphic != null) graphic.SetMaterialDirty();
        ReleaseMaterial();
    }

    private void OnDestroy() => ReleaseMaterial();

    private void ReleaseMaterial()
    {
        if (modifiedMaterial == null) return;
        if (Application.isPlaying) Destroy(modifiedMaterial);
        else DestroyImmediate(modifiedMaterial);
        modifiedMaterial = null;
    }
}
