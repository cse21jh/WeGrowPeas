using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.RecallBook
{
    /// <summary>A subdivided UI sheet. Its simulated depth is projected back into Canvas space.</summary>
    [AddComponentMenu("UI/Recall Book/Page Curl Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BookPageCurlGraphic : MaskableGraphic
    {
        [SerializeField, Range(16, 96)] private int columns = 48;
        [SerializeField, Range(2, 24)] private int rows = 8;
        [SerializeField, Range(0, 1.2f)] private float curl = 0.75f;
        [SerializeField, Range(0, 0.4f)] private float perspective = 0.16f;
        private Texture front;
        private Texture back;
        private Material instance;
        private float progress;
        private bool forward = true;

        public override Texture mainTexture => front != null ? front : Texture2D.whiteTexture;

        public void SetSheet(Texture frontSpread, Texture backSpread, bool turnForward, Material template)
        {
            front = frontSpread;
            back = backSpread;
            forward = turnForward;
            if (instance == null)
            {
                instance = new Material(template) { name = "Book Curl (Instance)", hideFlags = HideFlags.HideAndDontSave };
                material = instance;
            }
            instance.SetTexture("_BackTex", back);
            instance.SetVector("_FrontRect", forward ? new Vector4(0.5f, 0, 0.5f, 1) : new Vector4(0, 0, 0.5f, 1));
            instance.SetVector("_BackRect", forward ? new Vector4(0, 0, 0.5f, 1) : new Vector4(0.5f, 0, 0.5f, 1));
            if (canvas != null) canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            SetAllDirty();
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            SetVerticesDirty();
        }

        protected override void UpdateMaterial()
        {
            // A Mask can cache a stencil variant. Keep both page textures and crop rects in
            // sync when reusing that variant for a later turn in the opposite direction.
            if (instance != null)
            {
                var stencilMaterial = materialForRendering;
                stencilMaterial.SetTexture("_BackTex", back);
                stencilMaterial.SetVector("_FrontRect", instance.GetVector("_FrontRect"));
                stencilMaterial.SetVector("_BackRect", instance.GetVector("_BackRect"));
            }
            base.UpdateMaterial();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            float width = rect.width * 0.5f;
            if (width <= 0 || rect.height <= 0) return;
            int nx = Mathf.Clamp(columns, 16, 96);
            int ny = Mathf.Clamp(rows, 2, 24);
            float sign = forward ? 1 : -1;
            float x = 0, depth = 0;
            float bend = Mathf.Sin(progress * Mathf.PI) * curl;

            // Integrate the sheet tangent: constant strip length prevents rubber-like stretching.
            for (int col = 0; col < nx; col++)
            {
                float u0 = (float)col / nx;
                float u1 = (float)(col + 1) / nx;
                float angle = Mathf.Clamp(progress * Mathf.PI + bend * ((u0 + u1) * 0.5f - 0.5f), 0, Mathf.PI);
                float nextX = x + Mathf.Cos(angle) * width / nx;
                float nextDepth = depth + Mathf.Sin(angle) * width / nx;
                bool rear = angle > Mathf.PI * 0.5f;
                // The reverse side has its own UV direction so printed text is never mirrored.
                float texU0 = forward != rear ? u0 : 1 - u0;
                float texU1 = forward != rear ? u1 : 1 - u1;
                float light = Mathf.Clamp01(Mathf.Abs(Mathf.Cos(angle)) * 0.78f + 0.22f);
                for (int row = 0; row < ny; row++)
                {
                    float v0 = (float)row / ny, v1 = (float)(row + 1) / ny;
                    int start = vh.currentVertCount;
                    AddVertex(vh, rect, sign * x, depth, width, v0, texU0, rear, light);
                    AddVertex(vh, rect, sign * x, depth, width, v1, texU0, rear, light);
                    AddVertex(vh, rect, sign * nextX, nextDepth, width, v1, texU1, rear, light);
                    AddVertex(vh, rect, sign * nextX, nextDepth, width, v0, texU1, rear, light);
                    vh.AddTriangle(start, start + 1, start + 2);
                    vh.AddTriangle(start + 2, start + 3, start);
                }
                x = nextX;
                depth = nextDepth;
            }
        }

        private void AddVertex(VertexHelper vh, Rect rect, float x, float depth, float width,
            float v, float u, bool rear, float light)
        {
            float scale = 1f / (1f - depth / width * perspective);
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(rect.center.x + x * scale,
                rect.center.y + (v - 0.5f) * rect.height * scale, 0);
            vertex.color = color;
            vertex.uv0 = new Vector2(u, v);
            vertex.uv1 = new Vector4(rear ? 1 : 0, light, 0, 0);
            vh.AddVert(vertex);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (instance != null)
            {
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
            }
        }
    }
}
