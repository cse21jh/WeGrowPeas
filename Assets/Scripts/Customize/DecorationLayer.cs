using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 농장에 놓인 장식품(<see cref="DecorationLayout"/>)을 씬에 그린다. 장식품마다 아이콘 그림 그대로의 SpriteRenderer를 하나씩 만든다.
///
/// GardenBackground 프리팹 안에 있어서 농장 씬(Garden_GrassUpdate)과 치장 씬 양쪽에 같은 자리로 보인다.
/// 배치가 바뀔 때마다(놓기, 치우기, 되돌리기, 프로필 읽기) 전부 다시 만든다.
/// 단, 배경에 원래 박혀 있는 장식품(<see cref="fixtures"/>: 통나무·그루터기·돌멩이)은 새로 만들지 않고
/// 그 오브젝트를 옮기거나 숨긴다(<see cref="DecorationFixture"/>). 원래 자리에 있으면 씬에 배치한 모습 그대로 둔다.
///
/// 영역(앞뒤 순서를 정하는 아래쪽 끝 y, 클릭 영역, 놓을 수 있는지 판정)은 아이콘의 투명한 여백을 뺀
/// 실제 그림 영역(<see cref="SpriteArea"/>)으로 계산하고, 기울기(<see cref="CustomizeItemData.placedRotation"/>)가 있으면
/// 기울인 그림을 감싸는 사각형으로 계산한다.
///
/// 앞뒤 순서는 바위·통나무와 같은 방식을 따른다. 풀밭은 위에서 아래로 여러 줄(<see cref="depthRows"/>)로 나뉘어 있고
/// 줄마다 정렬 레이어·순서·z가 다르다. 장식품은 그림 아래쪽 끝이 걸린 줄과 같은 레이어·순서를 쓰고,
/// z를 그 줄보다 조금 앞(카메라 쪽)으로 두어 그 줄의 풀보다 앞, 아래 줄의 풀보다 뒤에 그려지게 한다
/// (아래쪽이 아래 줄 풀에 묻힌다). 모든 장식품이 같은 규칙을 쓰고, 계절 효과(꽃, 단풍, 눈)도 원래 배경 소품처럼 장식품 위에 겹칠 수 있다.
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

    [Tooltip("배경에 원래 박혀 있는 장식품(통나무, 그루터기, 돌멩이 등). 이 아이템들은 새로 만들지 않고 이 오브젝트를 옮기거나 숨긴다.")]
    [SerializeField] private DecorationFixture[] fixtures;

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
            if (view != null && view.GetComponent<DecorationFixture>() == null) Destroy(view.gameObject);
        _views.Clear();

        // 배경 장식품은 일단 모두 숨기고, 놓여 있는 것만 아래에서 다시 보인다.
        if (fixtures != null)
            foreach (var fixture in fixtures)
                if (fixture != null && fixture.Item != null)
                {
                    fixture.Capture();
                    fixture.SetShown(false);
                }

        foreach (var placement in DecorationLayout.Current)
        {
            var item = CustomizeCatalog.Find(placement.itemId);
            // 지워진 아이템이나 이 프로필이 보유하지 않은 장식품은 그리지 않는다(저장 목록에는 남겨 둔다).
            if (item == null || item.icon == null || !CustomizeInventory.IsOwned(item)) continue;

            var fixture = FindFixture(item);
            if (fixture != null)
            {
                ShowFixture(fixture, placement);
                continue;
            }

            var go = new GameObject($"Decoration_{item.Id}");
            go.transform.SetParent(transform, false);
            // 부모(GardenBackground)가 줄어 있어도 월드에서 ScaleFor 크기가 되게 부모 배율을 상쇄한다.
            // (미리보기·클릭 영역·X 버튼 위치가 모두 월드 크기 ScaleFor 기준이다.)
            float parentScale = transform.lossyScale.x;
            go.transform.localScale = Vector3.one * (ScaleFor(item) / (parentScale > 0f ? parentScale : 1f));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = item.icon;
            if (material != null) sr.sharedMaterial = material;
            float z = ApplyPlacementDepth(sr, item, placement.Position);
            go.transform.SetPositionAndRotation(new Vector3(placement.x, placement.y, z), RotationFor(item));

            var view = go.AddComponent<DecorationView>();
            view.Init(placement, item, sr);
            _views.Add(view);
        }
    }

    private DecorationFixture FindFixture(CustomizeItemData item)
    {
        if (fixtures == null) return null;
        foreach (var fixture in fixtures)
            if (fixture != null && fixture.Item == item) return fixture;
        return null;
    }

    /// <summary>
    /// 그 장식품을 피벗이 <paramref name="pivot"/>에 오게 놓았을 때의 정렬 레이어·순서를 <paramref name="sr"/>에 넣고 z를 돌려준다.
    /// 실제 배치와 놓기 미리보기(<see cref="DecorationPlacer"/>)가 함께 써서, 놓기 전에 보이는 앞뒤(풀에 묻히는 정도)가 놓은 뒤와 같다.
    ///
    /// 아래쪽 끝 y가 걸린 풀밭 줄로 앞뒤를 정한다(<see cref="ApplyDepth"/>).
    /// 예: 걸린 줄이 4면 순서 4 → 4 이하 줄의 풀보다 앞, 5 줄의 풀보다 뒤(아래쪽이 5 줄 풀에 묻힌다).
    /// 모든 장식품(배경 소품, 허수아비, 앞으로 추가할 장식품)이 같은 규칙을 쓴다.
    ///
    /// 놓기 미리보기에서는 매 프레임 부른다. 풀 줄 9개의 y 비교와 캐시된 그림 영역 계산뿐이고 메모리 할당이 없어 부담이 거의 없다.
    /// </summary>
    public float ApplyPlacementDepth(SpriteRenderer sr, CustomizeItemData item, Vector2 pivot)
    {
        ApplyDepth(sr, RectAt(item, pivot).yMin, out float z);
        return z;
    }

    /// <summary>
    /// 배경 장식품을 그 배치대로 보인다. 원래 자리면 씬에 배치한 모습 그대로,
    /// 아니면 그 자리로 옮기고 다른 장식품과 같은 계산(<see cref="ApplyPlacementDepth"/>)으로 앞뒤를 정한다.
    /// (원래 자리의 정렬 값도 이 규칙과 같다. 통나무만 GardenBG/0으로 박혀 있지만 풀 줄과의 앞뒤 관계는 같다)
    /// </summary>
    private void ShowFixture(DecorationFixture fixture, DecorationPlacement placement)
    {
        fixture.SetShown(true);
        var sr = fixture.Renderer;
        if (fixture.IsHome(placement.Position))
        {
            fixture.RestoreHome();
        }
        else
        {
            float z = ApplyPlacementDepth(sr, fixture.Item, placement.Position);
            fixture.transform.position = new Vector3(placement.x, placement.y, z);
        }

        var view = fixture.GetComponent<DecorationView>();
        if (view == null) view = fixture.gameObject.AddComponent<DecorationView>();
        view.Init(placement, fixture.Item, sr);
        _views.Add(view);
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

    private static readonly Dictionary<Sprite, Rect> _spriteAreas = new Dictionary<Sprite, Rect>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _spriteAreas.Clear();

    /// <summary>
    /// 그림에서 투명한 여백을 뺀 실제 그림 영역(피벗 기준 로컬 좌표). 스프라이트 메시 정점으로 구하므로
    /// 메시 종류가 Tight면 그림에 딱 맞고, Full Rect면 그림 사각형 전체가 된다.
    /// 통나무·돌 그림은 512px 캔버스에 여백이 커서, 사각형 전체로 아래쪽 끝 y를 재면 풀밭 줄이 한 칸씩 어긋난다.
    /// </summary>
    public static Rect SpriteArea(Sprite sprite)
    {
        if (_spriteAreas.TryGetValue(sprite, out var area)) return area;

        var vertices = sprite.vertices;
        if (vertices == null || vertices.Length == 0)
        {
            var b = sprite.bounds;
            area = new Rect(b.min, b.size);
        }
        else
        {
            Vector2 min = vertices[0], max = vertices[0];
            foreach (var v in vertices)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            area = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        _spriteAreas[sprite] = area;
        return area;
    }

    /// <summary>놓았을 때의 회전.</summary>
    public static Quaternion RotationFor(CustomizeItemData item)
        => item != null ? Quaternion.Euler(0f, 0f, item.placedRotation) : Quaternion.identity;

    /// <summary>
    /// 그 장식품을 피벗이 <paramref name="pivotPosition"/>에 오게 놓았을 때 차지하는 영역(2D, 실제 그림 영역).
    /// 기울어 있으면 기울인 그림을 감싸는 사각형.
    /// </summary>
    public static Rect RectAt(CustomizeItemData item, Vector2 pivotPosition)
    {
        float scale = ScaleFor(item);
        var a = SpriteArea(item.icon);
        Vector2 half = a.size * scale * 0.5f;

        float rad = item.placedRotation * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(rad)), sin = Mathf.Abs(Mathf.Sin(rad));
        Vector2 extents = new Vector2(half.x * cos + half.y * sin, half.x * sin + half.y * cos);

        Vector2 center = pivotPosition + Rotate(a.center * scale, rad);
        return new Rect(center - extents, extents * 2f);
    }

    /// <summary>실제 그림 영역의 가운데가 <paramref name="center"/>에 오도록 할 때의 피벗 위치.</summary>
    public static Vector2 PivotForCenter(CustomizeItemData item, Vector2 center)
        => center - Rotate(SpriteArea(item.icon).center * ScaleFor(item), item.placedRotation * Mathf.Deg2Rad);

    private static Vector2 Rotate(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
