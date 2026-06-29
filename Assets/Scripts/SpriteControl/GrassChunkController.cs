using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GrassChunkController : MonoBehaviour
{
    private struct GrassInstance
    {
        public Vector2 basePosition;
        public float width;
        public float height;
        public float phase;
        public float strength;
        public bool flipX;
    }

    [Header("Material")]
    [SerializeField] private Material grassMaterial;

    [Header("Grass Density")]
    [Tooltip("Area 1당 생성할 풀 개수. 예: Area Size 10x2에서 Density 25면 500개 생성")]
    [SerializeField] private float grassDensity = 25f;

    [Header("Placement Area")]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 2f);

    [Header("Grass Size")]
    [SerializeField] private Vector2 widthRange = new Vector2(0.6f, 1.0f);
    [SerializeField] private Vector2 heightRange = new Vector2(0.6f, 1.2f);

    [Header("Mesh Subdivision")]
    [SerializeField] private int xSegments = 4;
    [SerializeField] private int ySegments = 6;

    [Header("Wind Random")]
    [SerializeField] private Vector2 strengthRange = new Vector2(0.85f, 1.15f);
    [SerializeField] private int seed = 1234;

    [Header("Renderer Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("Debug")]
    [SerializeField] private int calculatedGrassCount;

    private Mesh generatedMesh;

    private void Awake()
    {
        Build();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        grassDensity = Mathf.Max(0f, grassDensity);

        xSegments = Mathf.Max(1, xSegments);
        ySegments = Mathf.Max(1, ySegments);

        areaSize.x = Mathf.Max(0.1f, areaSize.x);
        areaSize.y = Mathf.Max(0.1f, areaSize.y);

        widthRange.x = Mathf.Max(0.01f, widthRange.x);
        widthRange.y = Mathf.Max(widthRange.x, widthRange.y);

        heightRange.x = Mathf.Max(0.01f, heightRange.x);
        heightRange.y = Mathf.Max(heightRange.x, heightRange.y);

        strengthRange.x = Mathf.Max(0f, strengthRange.x);
        strengthRange.y = Mathf.Max(strengthRange.x, strengthRange.y);

        calculatedGrassCount = CalculateGrassCount();
    }
#endif

    [ContextMenu("Build Grass Chunk")]
    public void Build()
    {
        int actualGrassCount = CalculateGrassCount();
        calculatedGrassCount = actualGrassCount;

        var rng = new System.Random(seed);

        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<Vector4> customData = new();
        List<int> triangles = new();

        List<GrassInstance> instances = new();

        for (int i = 0; i < actualGrassCount; i++)
        {
            Vector2 basePosition = new Vector2(
                Range(rng, -areaSize.x * 0.5f, areaSize.x * 0.5f),
                Range(rng, -areaSize.y * 0.5f, areaSize.y * 0.5f)
            );

            float width = Range(rng, widthRange.x, widthRange.y);
            float height = Range(rng, heightRange.x, heightRange.y);

            float phase = Range(rng, 0f, Mathf.PI * 2f);
            float strength = Range(rng, strengthRange.x, strengthRange.y);

            bool flipX = rng.NextDouble() < 0.5;

            instances.Add(new GrassInstance
            {
                basePosition = basePosition,
                width = width,
                height = height,
                phase = phase,
                strength = strength,
                flipX = flipX
            });
        }

        // y가 높은 풀을 먼저 그리고, y가 낮은 풀을 나중에 그린다.
        // 낮은 y일수록 앞에 보이게 된다.
        instances.Sort((a, b) => b.basePosition.y.CompareTo(a.basePosition.y));

        foreach (GrassInstance instance in instances)
        {
            AddGrassPlane(
                vertices,
                uvs,
                customData,
                triangles,
                instance.basePosition,
                instance.width,
                instance.height,
                instance.phase,
                instance.strength,
                instance.flipX
            );
        }

        if (generatedMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "Grass Chunk Sway Mesh";

        if (vertices.Count > 65000)
        {
            generatedMesh.indexFormat = IndexFormat.UInt32;
        }

        generatedMesh.SetVertices(vertices);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetUVs(1, customData);
        generatedMesh.SetTriangles(triangles, 0);

        generatedMesh.RecalculateBounds();

        // Vertex shader에서 좌우로 흔들리기 때문에 bounds를 조금 키운다.
        Bounds bounds = generatedMesh.bounds;
        bounds.Expand(new Vector3(2f, 0.5f, 0f));
        generatedMesh.bounds = bounds;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = generatedMesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (grassMaterial != null)
        {
            meshRenderer.sharedMaterial = grassMaterial;
        }

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    private int CalculateGrassCount()
    {
        float area = areaSize.x * areaSize.y;
        return Mathf.Max(1, Mathf.RoundToInt(area * grassDensity));
    }

    private void AddGrassPlane(
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector4> customData,
        List<int> triangles,
        Vector2 basePosition,
        float width,
        float height,
        float phase,
        float strength,
        bool flipX
    )
    {
        int vertexStart = vertices.Count;

        for (int y = 0; y <= ySegments; y++)
        {
            float v = y / (float)ySegments;

            for (int x = 0; x <= xSegments; x++)
            {
                float u = x / (float)xSegments;

                float localX = (u - 0.5f) * width;
                float localY = v * height;

                vertices.Add(new Vector3(
                    basePosition.x + localX,
                    basePosition.y + localY,
                    0f
                ));

                float textureU = flipX ? 1f - u : u;
                float textureV = v;

                uvs.Add(new Vector2(textureU, textureV));

                // x = phase
                // y = strength
                // z = local height, bottom 0 ~ top 1
                // w = local x, left 0 ~ right 1
                customData.Add(new Vector4(phase, strength, v, u));
            }
        }

        int row = xSegments + 1;

        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i0 = vertexStart + y * row + x;
                int i1 = vertexStart + y * row + x + 1;
                int i2 = vertexStart + (y + 1) * row + x;
                int i3 = vertexStart + (y + 1) * row + x + 1;

                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                triangles.Add(i2);
                triangles.Add(i3);
                triangles.Add(i1);
            }
        }
    }

    private float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(areaSize.x, areaSize.y, 0f)
        );
    }
}
