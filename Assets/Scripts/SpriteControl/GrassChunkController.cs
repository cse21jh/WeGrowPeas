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
        public Rect uvRect;
    }


    [Header("Texture Atlas")]
        [SerializeField] private int atlasColumns = 3;
        [SerializeField] private int atlasRows = 1;
        [SerializeField] private int textureVariantCount = 3;

        [Tooltip("Atlas 경계에서 옆 텍스처가 번지는 것을 줄이기 위한 UV 안쪽 여백")]
        [SerializeField, Range(0f, 0.02f)] private float atlasUvPadding = 0.001f;

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

    [Header("Spawn Exclusion")]
        [SerializeField] private bool useExclusion = true;

        [Tooltip("이 Collider 안에는 풀/꽃을 생성하지 않음")]
        [SerializeField] private Transform exclusionRoot;
        [SerializeField] private List<Collider2D> exclusionColliders = new();

        [Tooltip("한 개체를 배치하려고 재시도할 최대 횟수")]
        [SerializeField] private int maxPlacementAttemptsPerInstance = 20;

        [Tooltip("넓은 풀 이미지가 마스크에 살짝 걸치는 것까지 막기 위한 검사 반경")]
        [SerializeField] private float exclusionCheckRadius = 0.15f;

    [Header("Debug")]
        [SerializeField] private int calculatedGrassCount;
        [SerializeField] private bool drawSpawnPoints = true;
        private readonly List<Vector2> debugSpawnPoints = new();

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

        atlasColumns = Mathf.Max(1, atlasColumns);
        atlasRows = Mathf.Max(1, atlasRows);

        int maxVariantCount = atlasColumns * atlasRows;
        textureVariantCount = Mathf.Clamp(textureVariantCount, 1, maxVariantCount);

        calculatedGrassCount = CalculateGrassCount();
    }
#endif

    [ContextMenu("Build Grass Chunk")]
    public void Build()
    {
        RefreshExclusionColliders();
        debugSpawnPoints.Clear();

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
            if (TryCreateGrassInstance(rng, out GrassInstance instance))
            {
                instances.Add(instance);
            }
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
                instance.flipX,
                instance.uvRect
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
        bool flipX,
        Rect uvRect
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

                float textureU01 = flipX ? 1f - u : u;
                float textureV01 = v;

                float textureU = Mathf.Lerp(uvRect.xMin, uvRect.xMax, textureU01);
                float textureV = Mathf.Lerp(uvRect.yMin, uvRect.yMax, textureV01);

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

    private bool TryCreateGrassInstance(System.Random rng, out GrassInstance instance)
    {
        for (int attempt = 0; attempt < maxPlacementAttemptsPerInstance; attempt++)
        {
            Vector2 basePosition = new Vector2(
                Range(rng, -areaSize.x * 0.5f, areaSize.x * 0.5f),
                Range(rng, -areaSize.y * 0.5f, areaSize.y * 0.5f)
            );

            float width = Range(rng, widthRange.x, widthRange.y);
            float height = Range(rng, heightRange.x, heightRange.y);

            if (IsBlockedByExclusion(basePosition, width, height))
            {
                continue;
            }

            instance = new GrassInstance
            {
                basePosition = basePosition,
                width = width,
                height = height,
                phase = Range(rng, 0f, Mathf.PI * 2f),
                strength = Range(rng, strengthRange.x, strengthRange.y),
                flipX = rng.NextDouble() < 0.5,
                uvRect = GetRandomAtlasRect(rng)
            };

            return true;
        }

        instance = default;
        return false;
    }

    private bool IsBlockedByExclusion(Vector2 basePosition, float width, float height)
    {
        if (!useExclusion || exclusionColliders == null || exclusionColliders.Count == 0)
        {
            return false;
        }

        // 풀/꽃의 기준점은 로컬 좌표이므로 월드 좌표로 변환
        Vector3 worldBase = transform.TransformPoint(new Vector3(basePosition.x, basePosition.y, 0f));

        // 너무 중심점만 검사하면 넓은 풀 이미지가 돌에 걸칠 수 있으므로
        // 주변 몇 점도 같이 검사한다.
        float halfWidth = width * 0.5f;
        float radius = Mathf.Max(exclusionCheckRadius, halfWidth * 0.35f);

        Vector2[] checkPoints =
        {
        worldBase,
        worldBase + Vector3.left * radius,
        worldBase + Vector3.right * radius,
        worldBase + Vector3.up * (height * 0.35f),
        worldBase + Vector3.left * radius + Vector3.up * (height * 0.25f),
        worldBase + Vector3.right * radius + Vector3.up * (height * 0.25f),
    };

        foreach (Collider2D collider in exclusionColliders)
        {
            if (collider == null)
            {
                continue;
            }

            foreach (Vector2 point in checkPoints)
            {
                if (collider.OverlapPoint(point))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RefreshExclusionColliders()
    {
        if (exclusionRoot == null)
        {
            return;
        }

        exclusionColliders.Clear();
        exclusionRoot.GetComponentsInChildren(true, exclusionColliders);
    }

    private float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(areaSize.x, areaSize.y, 0f)
        );

        if (drawSpawnPoints)
        {
            Gizmos.color = Color.red;

            foreach (Vector2 point in debugSpawnPoints)
            {
                Gizmos.DrawSphere(
                    new Vector3(point.x, point.y, 0f),
                    0.08f
                );
            }
        }

        Gizmos.matrix = oldMatrix;
    }

    private Rect GetRandomAtlasRect(System.Random rng)
    {
        int index = rng.Next(textureVariantCount);

        int column = index % atlasColumns;
        int row = index / atlasColumns;

        float cellWidth = 1f / atlasColumns;
        float cellHeight = 1f / atlasRows;

        float xMin = column * cellWidth;
        float yMin = 1f - ((row + 1) * cellHeight);

        Rect rect = new Rect(xMin, yMin, cellWidth, cellHeight);

        return ShrinkRect(rect, atlasUvPadding);
    }

    private Rect ShrinkRect(Rect rect, float padding)
    {
        if (padding <= 0f)
        {
            return rect;
        }

        float xMin = rect.xMin + padding;
        float xMax = rect.xMax - padding;
        float yMin = rect.yMin + padding;
        float yMax = rect.yMax - padding;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    
}
