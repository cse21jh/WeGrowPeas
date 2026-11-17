using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 농장에 놓인 장식품(<see cref="DecorationLayout"/>)을 씬에 그린다. 장식품마다 아이콘 그림 그대로의 SpriteRenderer를 하나씩 만든다.
///
/// GardenBackground 프리팹 안에 있어서 농장 씬(Garden_GrassUpdate)과 치장 씬 양쪽에 같은 자리로 보인다.
/// 배치가 바뀔 때마다(놓기, 치우기, 되돌리기, 프로필 읽기) 전부 다시 만든다.
///
/// 앞뒤 순서는 바위·통나무와 같은 방식을 따른다. 풀밭은 위에서 아래로 여러 줄(<see cref="depthRows"/>)로 나뉘어 있고
/// 줄마다 정렬 레이어·순서·z가 다르다. 장식품은 그림 아래쪽 끝이 걸린 줄과 같은 레이어·순서를 쓰고,
/// z를 그 줄보다 조금 앞(카메라 쪽)으로 두어 그 줄의 풀보다 앞, 아래 줄의 풀보다 뒤에 그려지게 한다.
/// 콜라이더는 붙이지 않는다(농장 씬의 다른 클릭을 가리지 않게). 치장 씬에서 클릭 판정은 <see cref="HitTest"/>로 한다.
/// </summary>
public class DecorationLayer : MonoBehaviour
{
    [Tooltip("풀밭 줄들(GrassProps의 Grass_ (0)~(8)), 위쪽 줄부터 순서대로. 장식품의 앞뒤 순서를 이 줄들에 맞춘다.")]
    [SerializeField] private Renderer[] depthRows;

    [Tooltip("풀밭 줄을 지정하지 않았을 때 쓸 정렬 레이어.")]
    [SerializeField] private string fallbackSortingLayer = "BG";

    [Tooltip("장식품에 쓸 머티리얼. 비우면 SpriteRenderer 기본값. 다른 배경 그림과 같은 조명을 받게 Sprite-Lit-Default를 넣는다.")]
    [SerializeField] private Material material;

    private readonly List<DecorationView> _views = new List<DecorationView>();

    public Material Material => material;
    public IReadOnlyList<DecorationView> Views => _views;

    private void OnEnable()
    {
        DecorationLayout.Changed += Rebuild;
        Rebuild();
    }

    private void OnDisable()
    {
        DecorationLayout.Changed -= Rebuild;
    }

    /// <summary>지금 배치대로 장식품을 다시 만든다.</summary>
    public void Rebuild()
    {
        foreach (var view in _views)
            if (view != null) Destroy(view.gameObject);
        _views.Clear();

        foreach (var placement in DecorationLayout.Current)
        {
            var item = CustomizeCatalog.Find(placement.itemId);
            // 지워진 아이템이나 이 프로필이 보유하지 않은 장식품은 그리지 않는다(저장 목록에는 남겨 둔다).
            if (item == null || item.icon == null || !CustomizeInventory.IsOwned(item)) continue;

            var go = new GameObject($"Decoration_{item.Id}");
            go.transform.SetParent(transform, false);
            // 부모(GardenBackground)가 줄어 있어도 월드에서 ScaleFor 크기가 되게 부모 배율을 상쇄한다.
            // (미리보기·클릭 영역·X 버튼 위치가 모두 월드 크기 ScaleFor 기준이다.)
            float parentScale = transform.lossyScale.x;
            go.transform.localScale = Vector3.one * (ScaleFor(item) / (parentScale > 0f ? parentScale : 1f));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = item.icon;
            if (material != null) sr.sharedMaterial = material;
            float baseY = RectAt(item, placement.Position).yMin;
            ApplyDepth(sr, baseY, out float z);
            go.transform.position = new Vector3(placement.x, placement.y, z);

            var view = go.AddComponent<DecorationView>();
            view.Init(placement, item, sr);
            _views.Add(view);
        }
    }

    /// <summary>그 점을 덮고 있는 장식품 중 맨 앞에 그려진 것. 없으면 null.</summary>
    public DecorationView HitTest(Vector2 worldPoint)
    {
        DecorationView best = null;
        foreach (var view in _views)
        {
            if (view == null || !view.Contains(worldPoint)) continue;
            // 앞에 그려진 것 = 아래쪽 끝이 더 낮은 것(더 아래 줄이거나, 같은 줄에서 z가 더 앞)
            if (best == null || view.Rect.yMin < best.Rect.yMin) best = view;
        }
        return best;
    }

    /// <summary>
    /// 아래쪽 끝이 <paramref name="baseY"/>인 장식품의 정렬 레이어·순서를 정하고 z를 돌려준다.
    /// 걸린 줄 = 위치(y)가 baseY 이상인 줄 중 가장 아래 줄. 바위·통나무도 이 규칙에 맞게 놓여 있다.
    /// </summary>
    private void ApplyDepth(SpriteRenderer sr, float baseY, out float z)
    {
        z = transform.position.z;
        int row = -1;
        if (depthRows != null)
            for (int i = 0; i < depthRows.Length; i++)
                if (depthRows[i] != null && depthRows[i].transform.position.y >= baseY) row = i;

        if (row < 0)
        {
            // 줄이 없거나 맨 위 줄보다도 위(화면 밖): 맨 뒤에 그린다.
            sr.sortingLayerName = fallbackSortingLayer;
            sr.sortingOrder = 0;
            if (depthRows != null && depthRows.Length > 0 && depthRows[0] != null)
            {
                sr.sortingLayerID = depthRows[0].sortingLayerID;
                sr.sortingOrder = depthRows[0].sortingOrder;
                z = depthRows[0].transform.position.z + 0.5f;
            }
            return;
        }

        var r = depthRows[row];
        sr.sortingLayerID = r.sortingLayerID;
        sr.sortingOrder = r.sortingOrder;

        // 그 줄과 다음 줄 사이에서, 아래쪽에 놓인 것일수록 앞(z가 작게)에 둔다. 줄 사이 z 간격은 약 1.
        float rowY = r.transform.position.y;
        float nextY = row + 1 < depthRows.Length && depthRows[row + 1] != null ? depthRows[row + 1].transform.position.y : rowY - 2f;
        float t = rowY > nextY ? Mathf.Clamp01((rowY - baseY) / (rowY - nextY)) : 0f;
        z = r.transform.position.z - 0.1f - 0.8f * t;
    }

    // ── 크기·영역 계산 (배치 미리보기와 공용) ─────────────────────────────────

    /// <summary>아이콘 그림을 <see cref="CustomizeItemData.placedHeight"/> 높이로 맞추는 배율.</summary>
    public static float ScaleFor(CustomizeItemData item)
    {
        if (item == null || item.icon == null) return 1f;
        float h = item.icon.bounds.size.y;
        return h > 0f ? item.placedHeight / h : 1f;
    }

    /// <summary>그 장식품을 피벗이 <paramref name="pivotPosition"/>에 오게 놓았을 때 차지하는 영역(2D).</summary>
    public static Rect RectAt(CustomizeItemData item, Vector2 pivotPosition)
    {
        float scale = ScaleFor(item);
        var b = item.icon.bounds;
        Vector2 size = (Vector2)b.size * scale;
        Vector2 center = pivotPosition + (Vector2)b.center * scale;
        return new Rect(center - size * 0.5f, size);
    }

    /// <summary>그림의 가운데가 <paramref name="center"/>에 오도록 할 때의 피벗 위치.</summary>
    public static Vector2 PivotForCenter(CustomizeItemData item, Vector2 center)
        => center - (Vector2)item.icon.bounds.center * ScaleFor(item);
}
