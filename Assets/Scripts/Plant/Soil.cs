using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soil : MonoBehaviour
{
    [SerializeField] private int _gridIndex;

    public int GridIndex => _gridIndex;

    public void Init(int gridIndex)
    {
        _gridIndex = gridIndex;
    }

    private GameObject highlightObj;
    // 최적화를 위한 공유 메테리얼 (타일 40개가 단 1개의 메테리얼을 돌려쓰도록 캐싱)
    private static Material sharedOutlineMaterial;

    private void Awake()
    {
        GenerateHighlightObject();
    }

    private void GenerateHighlightObject()
    {
        // 부모(자신)에 있는 콜라이더와 자식에 있는 외곽선용 콜라이더가 겹치지 않게, 자식 오브젝트의 콜라이더를 1순위로 찾습니다.
        PolygonCollider2D[] allPolyCols = GetComponentsInChildren<PolygonCollider2D>();
        PolygonCollider2D polyCol = null;
        
        foreach (var col in allPolyCols)
        {
            if (col.gameObject != this.gameObject) // 자기 자신이 아닌 자식 오브젝트라면
            {
                polyCol = col;
                break;
            }
        }
        
        // 만약 자식에 없다면 부모 것 사용
        if (polyCol == null && allPolyCols.Length > 0) polyCol = allPolyCols[0];
        
        if (polyCol == null) return;

        highlightObj = new GameObject("HighlightFX");
        highlightObj.transform.SetParent(polyCol.transform);
        highlightObj.transform.localPosition = Vector3.zero;
        highlightObj.transform.localRotation = Quaternion.identity;
        highlightObj.transform.localScale = Vector3.one;

        Vector2[] points = polyCol.points;
        Vector2 offset = polyCol.offset;

        // 1. Mesh for Stripes (유니티 CreateMesh 대신 점 좌표를 100% 동일하게 가져와 수동 생성)
        MeshFilter mf = highlightObj.AddComponent<MeshFilter>();
        MeshRenderer mr = highlightObj.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[points.Length];
        Vector2[] uvs = new Vector2[points.Length];
        
        for (int i = 0; i < points.Length; i++)
        {
            // LineRenderer와 완벽하게 동일한 좌표계(offset 포함) 사용
            vertices[i] = new Vector3(points[i].x + offset.x, points[i].y + offset.y, 0f);
            uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        
        // 다각형(사다리꼴 등) 표면 색칠을 위한 삼각 분할 (Fan Triangulation)
        int[] triangles = new int[(points.Length - 2) * 3];
        for (int i = 0; i < points.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        mf.mesh = mesh;
        mr.sortingOrder = 32766;
        mr.sortingLayerName = "GardenBG"; // 사용자 요청 레이어

        // 2. Line Renderer for Border
        LineRenderer lr = highlightObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        
        // 싱글톤 패턴처럼 공유 메테리얼이 없으면 최초 1회만 생성
        if (sharedOutlineMaterial == null)
        {
            sharedOutlineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }
        lr.material = sharedOutlineMaterial; // 40개의 타일이 이 1개의 메테리얼을 같이 바라봄 (배칭 최적화)
        
        lr.startColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
        lr.endColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
        lr.sortingOrder = 32767;
        lr.sortingLayerName = "GardenBG"; // 사용자 요청 레이어

        lr.positionCount = points.Length + 1;
        for (int i = 0; i < points.Length; i++)
        {
            lr.SetPosition(i, vertices[i]); // 위에서 만든 정점 좌표를 그대로 재사용 (오차 0%)
        }
        lr.SetPosition(points.Length, vertices[0]); // loop

        highlightObj.SetActive(false);
    }

    public void SetHighlight(bool isActive, Material stripeMaterial = null, UnityEngine.Color? outlineColor = null)
    {
        if (highlightObj != null)
        {
            if (isActive)
            {
                if (stripeMaterial != null)
                {
                    MeshRenderer mr = highlightObj.GetComponent<MeshRenderer>();
                    if (mr.sharedMaterial != stripeMaterial)
                    {
                        mr.sharedMaterial = stripeMaterial;
                    }
                }

                if (outlineColor.HasValue)
                {
                    LineRenderer lr = highlightObj.GetComponent<LineRenderer>();
                    lr.startColor = outlineColor.Value;
                    lr.endColor = outlineColor.Value;
                }
            }
            highlightObj.SetActive(isActive);
        }
    }
}