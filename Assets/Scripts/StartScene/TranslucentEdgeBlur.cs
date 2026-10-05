using LeTai.Asset.TranslucentImage;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Fades a full-screen Translucent Image quad from clear center to blurred edges.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TranslucentImage))]
public sealed class TranslucentEdgeBlur : BaseMeshEffect
{
    [Tooltip("선명하게 유지할 중심 위치. (0.5, 0.5)는 화면 중앙입니다.")]
    [SerializeField] private Vector2 center = new Vector2(0.5f, 0.5f);

    [Tooltip("블러 없이 선명한 영역의 반경. 커질수록 중앙의 선명한 영역이 넓어집니다.")]
    [SerializeField, Range(0f, 1f)] private float clearRadius = 0.35f;

    [Tooltip("가장자리 블러가 최대가 되는 반경. 중앙 기준 1이면 화면 상하좌우 끝입니다.")]
    [SerializeField, Range(0.01f, 1.5f)] private float blurRadius = 1f;

    [Tooltip("가장자리에서 블러 이미지를 보여주는 비율. 실제 블러 강도는 Source의 Blur Config에서 조절합니다.")]
    [SerializeField, Range(0f, 1f)] private float edgeOpacity = 1f;

    [Tooltip("그라데이션을 위한 UI 메시 분할 수. 기본값으로 충분히 부드럽게 표시됩니다.")]
    [SerializeField, Range(8, 64)] private int subdivisions = 32;

    public override void ModifyMesh(VertexHelper vh)
    {
        // This effect is intended for a Simple, rectangular image (no sliced/tiled sprite).
        if (!IsActive() || vh.currentVertCount != 4) return;

        UIVertex bottomLeft = default;
        UIVertex topLeft = default;
        UIVertex topRight = default;
        UIVertex bottomRight = default;
        vh.PopulateUIVertex(ref bottomLeft, 0);
        vh.PopulateUIVertex(ref topLeft, 1);
        vh.PopulateUIVertex(ref topRight, 2);
        vh.PopulateUIVertex(ref bottomRight, 3);
        vh.Clear();

        int steps = Mathf.Clamp(subdivisions, 8, 64);
        float inner = Mathf.Max(0f, clearRadius);
        float outer = Mathf.Max(inner + 0.001f, blurRadius);

        for (int y = 0; y <= steps; y++)
        {
            float v = y / (float)steps;
            UIVertex left = Interpolate(bottomLeft, topLeft, v);
            UIVertex right = Interpolate(bottomRight, topRight, v);
            for (int x = 0; x <= steps; x++)
            {
                float u = x / (float)steps;
                UIVertex vertex = Interpolate(left, right, u);
                // Normalized axes form an ellipse that adapts to the image's aspect ratio.
                float distance = ((new Vector2(u, v) - center) * 2f).magnitude;
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, distance));
                vertex.color.a = (byte)Mathf.RoundToInt(vertex.color.a * blend * Mathf.Clamp01(edgeOpacity));
                vh.AddVert(vertex);
            }
        }

        int stride = steps + 1;
        for (int y = 0; y < steps; y++)
        {
            for (int x = 0; x < steps; x++)
            {
                int i = y * stride + x;
                vh.AddTriangle(i, i + stride, i + stride + 1);
                vh.AddTriangle(i + stride + 1, i + 1, i);
            }
        }
    }

    private static UIVertex Interpolate(UIVertex a, UIVertex b, float t)
    {
        // Keep Translucent Image's packed shader data in uv1/uv2/uv3 unchanged.
        UIVertex result = a;
        result.position = Vector3.LerpUnclamped(a.position, b.position, t);
        result.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t);
        result.color = Color32.Lerp(a.color, b.color, t);
        return result;
    }
}
