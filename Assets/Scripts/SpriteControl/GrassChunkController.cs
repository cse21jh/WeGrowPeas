using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GrassChunkController : MonoBehaviour
{
    private enum PlacementMode
    {
        Random,
        JitteredGrid
    }

    private struct GrassInstance
    {
        public Vector2 basePosition;
        public float width;
        public float height;
        public float phase;
        public float strength;
        public bool flipX;
        public Rect uvRect;
        public float zOffset;
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
    [Tooltip("Area 1당 생성할 풀/꽃 개수. 예: Area Size 10x2에서 Density 25면 목표 500개 생성")]
    [SerializeField] private float grassDensity = 25f;

    [Header("Placement Area")]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 2f);

    [Header("Placement Distribution")]
    [SerializeField] private PlacementMode placementMode = PlacementMode.JitteredGrid;

    [Tooltip("0이면 칸 중앙에 정렬, 1이면 칸 안에서 최대한 랜덤하게 이동")]
    [SerializeField, Range(0f, 1f)] private float placementJitter = 0.75f;

    [Tooltip("행마다 반 칸씩 엇갈리게 배치해서 격자 느낌을 줄임")]
    [SerializeField] private bool staggerRows = true;

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

    [Tooltip("이 Root 아래의 Collider2D 안에는 풀/꽃을 생성하지 않음")]
    [SerializeField] private Transform exclusionRoot;

    [Tooltip("수동으로 지정한 제외 Collider. exclusionRoot가 있으면 Build 시 Root 아래 Collider로 자동 갱신됨")]
    [SerializeField] private List<Collider2D> exclusionColliders = new();

    [Tooltip("Random 배치에서 한 개체를 배치하려고 재시도할 최대 횟수")]
    [SerializeField] private int maxPlacementAttemptsPerInstance = 20;

    [Tooltip("넓은 풀 이미지가 마스크에 살짝 걸치는 것까지 막기 위한 검사 반경")]
    [SerializeField] private float exclusionCheckRadius = 0.15f;

    [Header("Depth / Culling Safety")]
    [Tooltip("카메라 흔들림이나 vertex sway 때문에 bounds 밖으로 잘리는 현상을 줄이기 위한 Bounds 확장값")]
    [SerializeField] private Vector3 boundsPadding = new Vector3(2f, 0.5f, 5f);

    [Tooltip("대부분의 경우 ZWrite Off/ZTest 설정으로 충분하므로 기본은 꺼둠")]
    [SerializeField] private bool usePerInstanceZOffset = false;

    [SerializeField] private Vector2 zOffsetRange = new Vector2(-0.002f, 0.002f);

    [Header("Debug")]
    [SerializeField] private int calculatedTargetCount;
    [SerializeField] private int generatedInstanceCount;
    [SerializeField] private bool drawSpawnPoints = true;
    [SerializeField] private float debugSpawnPointRadius = 0.08f;

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

        maxPlacementAttemptsPerInstance = Mathf.Max(1, maxPlacementAttemptsPerInstance);
        exclusionCheckRadius = Mathf.Max(0f, exclusionCheckRadius);

        boundsPadding.x = Mathf.Max(0f, boundsPadding.x);
        boundsPadding.y = Mathf.Max(0f, boundsPadding.y);
        boundsPadding.z = Mathf.Max(0f, boundsPadding.z);

        zOffsetRange.y = Mathf.Max(zOffsetRange.x, zOffsetRange.y);
        debugSpawnPointRadius = Mathf.Max(0.001f, debugSpawnPointRadius);

        calculatedTargetCount = CalculateTargetCount();
    }
#endif

    [ContextMenu("Build Grass Chunk")]
    public void Build()
    {
        RefreshExclusionColliders();

        debugSpawnPoints.Clear();

        int targetCount = CalculateTargetCount();
        calculatedTargetCount = targetCount;

        System.Random rng = new System.Random(seed);

        List<GrassInstance> instances = CreateGrassInstances(rng, targetCount);
        generatedInstanceCount = instances.Count;

        // y가 높은 풀을 먼저 그리고, y가 낮은 풀을 나중에 그린다.
        // 낮은 y일수록 앞에 보이게 된다.
        instances.Sort((a, b) => b.basePosition.y.CompareTo(a.basePosition.y));

        List<Vector3> vertices = new();
        List<Vector2> uvs = new();
        List<Vector4> customData = new();
        List<Color32> colors = new();
        List<Vector3> normals = new();
        List<Vector4> tangents = new();
        List<int> triangles = new();

        foreach (GrassInstance instance in instances)
        {
            AddGrassPlane(
                vertices,
                uvs,
                customData,
                colors,
                normals,
                tangents,
                triangles,
                instance
            );
        }

        ApplyMesh(vertices, uvs, customData, colors, normals, tangents, triangles);
        ApplyRendererSettings();
    }

    private int CalculateTargetCount()
    {
        float area = areaSize.x * areaSize.y;
        return Mathf.Max(1, Mathf.RoundToInt(area * grassDensity));
    }

    private List<GrassInstance> CreateGrassInstances(System.Random rng, int targetCount)
    {
        List<GrassInstance> instances = new(targetCount);

        switch (placementMode)
        {
            case PlacementMode.JitteredGrid:
                CreateJitteredGridInstances(rng, targetCount, instances);
                break;

            case PlacementMode.Random:
            default:
                CreateRandomInstances(rng, targetCount, instances);
                break;
        }

        return instances;
    }

    private void CreateRandomInstances(System.Random rng, int targetCount, List<GrassInstance> instances)
    {
        for (int i = 0; i < targetCount; i++)
        {
            if (TryCreateRandomGrassInstance(rng, out GrassInstance instance))
            {
                instances.Add(instance);
                debugSpawnPoints.Add(instance.basePosition);
            }
        }
    }

    private void CreateJitteredGridInstances(System.Random rng, int targetCount, List<GrassInstance> instances)
    {
        List<Vector2> positions = CreateJitteredGridPositions(rng, targetCount);

        foreach (Vector2 basePosition in positions)
        {
            if (TryCreateGrassInstanceAtPosition(rng, basePosition, out GrassInstance instance))
            {
                instances.Add(instance);
                debugSpawnPoints.Add(instance.basePosition);
            }
        }
    }

    private bool TryCreateRandomGrassInstance(System.Random rng, out GrassInstance instance)
    {
        for (int attempt = 0; attempt < maxPlacementAttemptsPerInstance; attempt++)
        {
            Vector2 basePosition = new Vector2(
                Range(rng, -areaSize.x * 0.5f, areaSize.x * 0.5f),
                Range(rng, -areaSize.y * 0.5f, areaSize.y * 0.5f)
            );

            if (TryCreateGrassInstanceAtPosition(rng, basePosition, out instance))
            {
                return true;
            }
        }

        instance = default;
        return false;
    }

    private bool TryCreateGrassInstanceAtPosition(System.Random rng, Vector2 basePosition, out GrassInstance instance)
    {
        float width = Range(rng, widthRange.x, widthRange.y);
        float height = Range(rng, heightRange.x, heightRange.y);

        if (IsBlockedByExclusion(basePosition, width, height))
        {
            instance = default;
            return false;
        }

        instance = new GrassInstance
        {
            basePosition = basePosition,
            width = width,
            height = height,
            phase = Range(rng, 0f, Mathf.PI * 2f),
            strength = Range(rng, strengthRange.x, strengthRange.y),
            flipX = rng.NextDouble() < 0.5,
            uvRect = GetRandomAtlasRect(rng),
            zOffset = usePerInstanceZOffset ? Range(rng, zOffsetRange.x, zOffsetRange.y) : 0f
        };

        return true;
    }

    private List<Vector2> CreateJitteredGridPositions(System.Random rng, int count)
    {
        List<Vector2> positions = new(count);

        float aspect = areaSize.x / areaSize.y;

        int columns = Mathf.CeilToInt(Mathf.Sqrt(count * aspect));
        int rows = Mathf.CeilToInt(count / (float)columns);

        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);

        float cellWidth = areaSize.x / columns;
        float cellHeight = areaSize.y / rows;

        float halfAreaX = areaSize.x * 0.5f;
        float halfAreaY = areaSize.y * 0.5f;

        for (int y = 0; y < rows; y++)
        {
            float rowOffset = staggerRows && (y % 2 == 1)
                ? cellWidth * 0.5f
                : 0f;

            for (int x = 0; x < columns; x++)
            {
                float baseX = -halfAreaX + (x + 0.5f) * cellWidth + rowOffset;
                float baseY = -halfAreaY + (y + 0.5f) * cellHeight;

                if (baseX > halfAreaX)
                {
                    baseX -= areaSize.x;
                }

                float jitterX = Range(rng, -cellWidth * 0.5f, cellWidth * 0.5f) * placementJitter;
                float jitterY = Range(rng, -cellHeight * 0.5f, cellHeight * 0.5f) * placementJitter;

                float finalX = Mathf.Clamp(baseX + jitterX, -halfAreaX, halfAreaX);
                float finalY = Mathf.Clamp(baseY + jitterY, -halfAreaY, halfAreaY);

                positions.Add(new Vector2(finalX, finalY));
            }
        }

        Shuffle(positions, rng);

        if (positions.Count > count)
        {
            positions.RemoveRange(count, positions.Count - count);
        }

        return positions;
    }

    private void AddGrassPlane(
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector4> customData,
        List<Color32> colors,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<int> triangles,
        GrassInstance instance
    )
    {
        int vertexStart = vertices.Count;

        for (int y = 0; y <= ySegments; y++)
        {
            float v = y / (float)ySegments;

            for (int x = 0; x <= xSegments; x++)
            {
                float u = x / (float)xSegments;

                float localX = (u - 0.5f) * instance.width;
                float localY = v * instance.height;

                vertices.Add(new Vector3(
                    instance.basePosition.x + localX,
                    instance.basePosition.y + localY,
                    instance.zOffset
                ));

                float textureU01 = instance.flipX ? 1f - u : u;
                float textureV01 = v;

                float textureU = Mathf.Lerp(instance.uvRect.xMin, instance.uvRect.xMax, textureU01);
                float textureV = Mathf.Lerp(instance.uvRect.yMin, instance.uvRect.yMax, textureV01);

                uvs.Add(new Vector2(textureU, textureV));

                // x = phase
                // y = strength
                // z = local height, bottom 0 ~ top 1
                // w = local x, left 0 ~ right 1
                customData.Add(new Vector4(instance.phase, instance.strength, v, u));

                colors.Add(new Color32(255, 255, 255, 255));
                normals.Add(Vector3.back);
                tangents.Add(new Vector4(1f, 0f, 0f, -1f));
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

    private void ApplyMesh(
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<Vector4> customData,
        List<Color32> colors,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<int> triangles
    )
    {
        DestroyGeneratedMesh();

        generatedMesh = new Mesh
        {
            name = "Grass Chunk Sway Mesh"
        };

        if (vertices.Count > 65000)
        {
            generatedMesh.indexFormat = IndexFormat.UInt32;
        }

        generatedMesh.SetVertices(vertices);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetUVs(1, customData);
        generatedMesh.SetColors(colors);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetTangents(tangents);
        generatedMesh.SetTriangles(triangles, 0);

        generatedMesh.RecalculateBounds();

        Bounds bounds = generatedMesh.bounds;
        bounds.Expand(boundsPadding);
        generatedMesh.bounds = bounds;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = generatedMesh;
    }

    private void ApplyRendererSettings()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (grassMaterial != null)
        {
            meshRenderer.sharedMaterial = grassMaterial;
        }

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    private void DestroyGeneratedMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        Mesh meshToDestroy = generatedMesh;

        if (meshToDestroy == null && meshFilter != null && meshFilter.sharedMesh != null)
        {
            if (meshFilter.sharedMesh.name == "Grass Chunk Sway Mesh")
            {
                meshToDestroy = meshFilter.sharedMesh;
            }
        }

        if (meshFilter != null && meshFilter.sharedMesh == meshToDestroy)
        {
            meshFilter.sharedMesh = null;
        }

        if (meshToDestroy != null)
        {
            if (Application.isPlaying)
            {
                Destroy(meshToDestroy);
            }
            else
            {
                DestroyImmediate(meshToDestroy);
            }
        }

        generatedMesh = null;
    }

    private bool IsBlockedByExclusion(Vector2 basePosition, float width, float height)
    {
        if (!useExclusion || exclusionColliders == null || exclusionColliders.Count == 0)
        {
            return false;
        }

        Vector3 worldBase3 = transform.TransformPoint(new Vector3(basePosition.x, basePosition.y, 0f));
        Vector2 worldBase = new Vector2(worldBase3.x, worldBase3.y);

        float halfWidth = width * 0.5f;
        float radius = Mathf.Max(exclusionCheckRadius, halfWidth * 0.35f);

        Vector2[] checkPoints =
        {
            worldBase,
            worldBase + Vector2.left * radius,
            worldBase + Vector2.right * radius,
            worldBase + Vector2.up * (height * 0.35f),
            worldBase + Vector2.left * radius + Vector2.up * (height * 0.25f),
            worldBase + Vector2.right * radius + Vector2.up * (height * 0.25f),
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

    private float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0f));

        if (drawSpawnPoints)
        {
            Gizmos.color = Color.red;

            foreach (Vector2 point in debugSpawnPoints)
            {
                Gizmos.DrawSphere(new Vector3(point.x, point.y, 0f), debugSpawnPointRadius);
            }
        }

        Gizmos.matrix = oldMatrix;
    }
}
