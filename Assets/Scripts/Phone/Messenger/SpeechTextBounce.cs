using System;
using TMPro;
using UnityEngine;

/// <summary>
/// The JODAL DialogueTextBounce reveal: the same 0.24s scale curve and font-relative hop.
/// Caches TMP's clean geometry so resizing and fallback font atlases do not accumulate distortion.
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
[DefaultExecutionOrder(10000)]
public sealed class SpeechTextBounce : MonoBehaviour
{
    [SerializeField, Min(.05f)] private float duration = .24f;
    [SerializeField, Range(0f, .3f)] private float scaleAmount = .12f;
    [SerializeField, Range(0f, 12f)] private float bounceHeight = 4f;
    [SerializeField] private AnimationCurve scaleCurve = new AnimationCurve(
        new Keyframe(0, -.65f), new Keyframe(.28f, 1),
        new Keyframe(.62f, -.2f), new Keyframe(1, 0));

    private TMP_Text label;
    private Vector3[][] restVertices = Array.Empty<Vector3[]>();
    private float[] revealedAt = Array.Empty<float>();
    private string revealingText;
    private int lastVisible;
    private bool revealing;
    public int ActiveGlyphCount { get; private set; }
    public float AnimationTime { get; private set; }

    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        label.OnPreRenderText += OnTextMeshRebuilt;
    }

    private void OnDisable()
    {
        FinishInstantly();
        if (label != null) label.OnPreRenderText -= OnTextMeshRebuilt;
    }

    public void BeginReveal()
    {
        if (!isActiveAndEnabled) return;
        revealingText = label.text;
        revealedAt = new float[label.textInfo.characterCount];
        for (int i = 0; i < revealedAt.Length; i++) revealedAt[i] = float.NegativeInfinity;
        lastVisible = 0;
        AnimationTime = 0;
        ActiveGlyphCount = 0;
        revealing = true;
        label.SetVerticesDirty();
    }

    public void FinishInstantly()
    {
        revealing = false;
        ActiveGlyphCount = 0;
        if (label != null) { label.havePropertiesChanged = true; label.SetVerticesDirty(); }
    }

    private void LateUpdate()
    {
        if (!revealing) return;
        AnimationTime += Time.unscaledDeltaTime;
        if (label.havePropertiesChanged) label.ForceMeshUpdate();
        ObserveReveal(label.textInfo);
        Deform(label.textInfo);
        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        if (lastVisible >= revealedAt.Length && ActiveGlyphCount == 0) revealing = false;
    }

    private void OnTextMeshRebuilt(TMP_TextInfo info)
    {
        if (restVertices.Length != info.meshInfo.Length) restVertices = new Vector3[info.meshInfo.Length][];
        for (int i = 0; i < info.meshInfo.Length; i++)
        {
            info.meshInfo[i].ClearUnusedVertices();
            var source = info.meshInfo[i].vertices;
            if (restVertices[i] == null || restVertices[i].Length != source.Length)
                restVertices[i] = new Vector3[source.Length];
            Array.Copy(source, restVertices[i], source.Length);
        }
        if (!revealing) return;
        if (label.text != revealingText || info.characterCount != revealedAt.Length)
        { FinishInstantly(); return; }
        ObserveReveal(info);
        Deform(info);
    }

    private void ObserveReveal(TMP_TextInfo info)
    {
        int visible = Mathf.Min(label.maxVisibleCharacters, info.characterCount);
        for (int i = lastVisible; i < visible; i++)
            if (info.characterInfo[i].isVisible) revealedAt[i] = AnimationTime;
        lastVisible = visible;
    }

    private void Deform(TMP_TextInfo info)
    {
        ActiveGlyphCount = 0;
        for (int m = 0; m < info.materialCount && m < restVertices.Length; m++)
        {
            if (restVertices[m] != null) Array.Copy(restVertices[m], info.meshInfo[m].vertices,
                Mathf.Min(restVertices[m].Length, info.meshInfo[m].vertices.Length));
            info.meshInfo[m].ClearUnusedVertices();
        }
        int visible = Mathf.Min(label.maxVisibleCharacters, info.characterCount);
        for (int i = 0; i < visible; i++)
        {
            float age = i < revealedAt.Length ? AnimationTime - revealedAt[i] : duration;
            if (age < 0 || age >= duration) continue;
            var character = info.characterInfo[i];
            if (!character.isVisible) continue;
            int material = character.materialReferenceIndex, vertex = character.vertexIndex;
            if (material >= restVertices.Length || restVertices[material] == null || vertex + 3 >= restVertices[material].Length) continue;
            float t = age / Mathf.Max(.05f, duration);
            float scale = 1 + scaleAmount * scaleCurve.Evaluate(t);
            float rise = Mathf.Sin(t * Mathf.PI) * (1 - t) * bounceHeight * (label.fontSize / 24f);
            var source = restVertices[material];
            var target = info.meshInfo[material].vertices;
            Vector3 pivot = (source[vertex] + source[vertex + 3]) * .5f;
            for (int k = 0; k < 4; k++) target[vertex + k] = pivot + (source[vertex + k] - pivot) * scale + Vector3.up * rise;
            ActiveGlyphCount++;
        }
    }
}
