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
/// 실제 그림 영역(<see cref="SpriteArea"/>)으로 계산하고, 크기·기울기·반전(<see cref="DecorationPose"/>)이 있으면
/// 기울인 그림을 감싸는 사각형으로 계산한다.
///
/// 앞뒤 순서는 바위·통나무와 같은 방식을 따른다. 풀밭은 위에서 아래로 여러 줄(<see cref="depthRows"/>)로 나뉘어 있고
/// 줄마다 정렬 레이어·순서·z가 다르다. 장식품은 그림 아래쪽 끝이 걸린 줄과 같은 레이어·순서를 쓰고,
/// z를 그 줄보다 조금 앞(카메라 쪽)으로 두어 그 줄의 풀보다 앞, 아래 줄의 풀보다 뒤에 그려지게 한다
/// (아래쪽이 아래 줄 풀에 묻힌다). 모든 장식품이 같은 규칙을 쓰고, 봄 꽃·가을 단풍은 원래 배경 소품처럼 장식품 위에 겹칠 수 있다.
///
/// 겨울 눈 덮개(<see cref="snowOverlays"/>)는 화면 전체를 모든 풀 줄 위로 덮는 그림이라 정렬 순서로는 장식품을 그 위로 올릴 수 없다.
/// 대신 실제로 보이는 장식품 픽셀에만 눈을 빼고, 장식품 앞을 가리는 풀잎 위에는 눈이 그대로 덮이게 한다(<see cref="SyncSnowStencil"/>).
///   장식품마다 스텐실만 기록하는 그림(DecorationSnowStencil 셰이더)을 자식으로 붙여 장식품과 같은 정렬 순서로 그린다
///   → 앞줄 풀(GrassChunkSwayEffect)이 그 뒤에 그려지며 풀잎 자리의 기록을 지운다
///   → 눈 덮개는 "마스크 바깥만 보임"이라 기록이 남은 자리(보이는 장식품 픽셀)에만 그려지지 않는다.
/// SpriteMask는 URP 2D에서 정렬 순서와 상관없이 먼저 그려져 뒤쪽 풀에까지 지워지므로 쓰지 않는다.
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

    [Tooltip("장식품을 덮지 않게 할 겨울 눈 덮개(winter_snow/Snow). 보이는 장식품 픽셀만큼 이 그림을 빼고 그린다. " +
             "작은 눈 더미(Plant 레이어)는 완두콩·땅콩의 마스크에도 같이 빠져 버리므로 넣지 않는다.")]
    [SerializeField] private SpriteRenderer[] snowOverlays;

    [Tooltip("장식품 모양을 스텐실에 기록하는 머티리얼(Mat_DecorationSnowStencil, DecorationSnowStencil 셰이더). 화면에는 그리지 않는다.")]
    [SerializeField] private Material snowStencilMaterial;

    private const string SnowStencilName = "SnowStencil";

    private readonly List<DecorationView> _views = new List<DecorationView>();

    private bool _snowConfigured;
    private bool _hasSnow;

    public Material Material => material;
    public IReadOnlyList<DecorationView> Views => _views;

    private void Awake() => ConfigureSnowOverlays();

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

            DecorationView view;
            var fixture = FindFixture(item);
            if (fixture != null)
            {
                fixture.SetShown(true);
                view = fixture.GetComponent<DecorationView>();
                if (view == null) view = fixture.gameObject.AddComponent<DecorationView>();
                view.Init(placement, item, fixture.Renderer);
            }
            else
            {
                var go = new GameObject($"Decoration_{item.Id}");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = item.icon;
                if (material != null) sr.sharedMaterial = material;
                view = go.AddComponent<DecorationView>();
                view.Init(placement, item, sr);
            }

            ApplyPose(view, placement.Pose);
            _views.Add(view);
        }
    }

    /// <summary>
    /// 그 장식품을 그 위치·크기·기울기·반전으로 보이고, 앞뒤 순서와 겨울 눈 처리를 맞춘다.
    /// 저장된 배치를 그릴 때와, 치장 화면에서 끌어 옮기거나 돌리는 동안의 임시 모습(<see cref="DecorationPlacer"/>)을 모두 이것으로 그린다.
    ///
    /// 배경 소품(<see cref="DecorationFixture"/>)이 원래 자리·원래 모습이면 씬에 배치한 그대로 둔다.
    /// (원래 자리의 정렬 값도 이 규칙과 같다. 통나무만 GardenBG/0으로 박혀 있지만 풀 줄과의 앞뒤 관계는 같다)
    /// </summary>
    public void ApplyPose(DecorationView view, DecorationPose pose)
    {
        if (view == null || view.Item == null) return;
        var item = view.Item;
        var sr = view.Renderer;
        var fixture = view.GetComponent<DecorationFixture>();

        if (fixture != null && fixture.IsHome(pose))
        {
            fixture.RestoreHome();
        }
        else
        {
            float z = ApplyPlacementDepth(sr, item, pose);
            var position = new Vector3(pose.position.x, pose.position.y, z);
            float angle = AngleFor(item, pose);
            bool flip = FlipFor(item, pose);

            if (fixture != null)
            {
                fixture.ApplyTransform(position, angle, pose.scale, flip);
            }
            else
            {
                // 부모(GardenBackground)가 줄어 있어도 월드에서 원하는 크기가 되게 부모 배율을 상쇄한다.
                // (미리보기·클릭 영역·버튼 위치가 모두 월드 크기 기준이다.)
                float parentScale = transform.lossyScale.x;
                view.transform.localScale = Vector3.one * (ScaleFor(item) * pose.scale / (parentScale > 0f ? parentScale : 1f));
                view.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
                sr.flipX = flip;
            }
        }

        SyncSnowStencil(sr);
        view.SetPose(pose);
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
    /// 놓기 미리보기와 끌어 옮기기·돌리기 중에는 매 프레임 부른다. 풀 줄 9개의 y 비교와 캐시된 그림 영역 계산뿐이고
    /// 메모리 할당이 없어 부담이 거의 없다.
    /// </summary>
    public float ApplyPlacementDepth(SpriteRenderer sr, CustomizeItemData item, DecorationPose pose)
    {
        ApplyDepth(sr, RectAt(item, pose).yMin, out float z);
        return z;
    }

    // ── 겨울 눈 덮개 빼기 ────────────────────────────────────────────────────

    /// <summary>눈 덮개를 "마스크 바깥만 보임"으로 바꾼다. 한 번만. 눈 덮개나 스텐실 머티리얼이 없으면 false.</summary>
    private bool ConfigureSnowOverlays()
    {
        if (_snowConfigured) return _hasSnow;
        _snowConfigured = true;

        _hasSnow = false;
        if (snowStencilMaterial == null || snowOverlays == null) return false;
        foreach (var snow in snowOverlays)
        {
            if (snow == null) continue;
            snow.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            _hasSnow = true;
        }
        return _hasSnow;
    }

    /// <summary>
    /// 그 장식품(또는 놓기 미리보기) 그림 아래에 눈 덮개를 빼는 스텐실 기록용 그림을 붙이고, 그림·정렬 레이어·순서를 장식품과 맞춘다.
    /// 장식품의 정렬이 정해진 뒤에 부른다(미리보기는 정렬이 바뀔 때마다).
    /// 자식이라 위치·크기·기울기·좌우 반전을 그대로 따라가고, 장식품이 숨으면 같이 꺼진다.
    /// </summary>
    public void SyncSnowStencil(SpriteRenderer owner)
    {
        if (owner == null || !ConfigureSnowOverlays()) return;

        var child = owner.transform.Find(SnowStencilName);
        SpriteRenderer stencil;
        if (child != null)
        {
            stencil = child.GetComponent<SpriteRenderer>();
        }
        else
        {
            var go = new GameObject(SnowStencilName);
            go.transform.SetParent(owner.transform, false);
            stencil = go.AddComponent<SpriteRenderer>();
            stencil.sharedMaterial = snowStencilMaterial;
        }
        if (stencil == null) return;

        stencil.sprite = owner.sprite;
        stencil.flipX = owner.flipX;
        stencil.flipY = owner.flipY;
        stencil.sortingLayerID = owner.sortingLayerID;
        stencil.sortingOrder = owner.sortingOrder;
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

    /// <summary>크기를 줄였을 때 보이는 그림의 긴 변이 이보다 작아지지 않게 한다(월드 단위). 텃밭 한 칸이 약 1.1. 너무 작으면 누르기 어렵다.</summary>
    public const float MinVisibleSize = 0.6f;

    /// <summary>크기를 키웠을 때 보이는 그림의 긴 변이 이보다 커지지 않게 한다(월드 단위). 화면 높이가 약 9.8이라 그 절반 정도.</summary>
    public const float MaxVisibleSize = 5f;

    /// <summary>
    /// 그 장식품의 크기 배율 범위. 보이는 그림(기울이기 전)의 긴 변이 <see cref="MinVisibleSize"/>~<see cref="MaxVisibleSize"/> 안에 들게 하고,
    /// 배율 자체도 <see cref="DecorationPose.MinScale"/>~<see cref="DecorationPose.MaxScale"/>를 넘지 않는다.
    /// 장식품마다 원래 크기가 크게 달라서(작은 돌 약 0.9, 통나무 약 3.7) 배율 하나로 정하면 작은 것은 거의 못 키우고 큰 것은 화면을 덮는다.
    /// 기본 크기(1배)는 항상 범위 안에 든다.
    /// </summary>
    public static void ScaleRange(CustomizeItemData item, out float min, out float max)
    {
        min = DecorationPose.MinScale;
        max = DecorationPose.MaxScale;
        if (item == null || item.icon == null) return;

        var size = SpriteArea(item.icon).size * ScaleFor(item);
        float longSide = Mathf.Max(size.x, size.y);
        if (longSide <= 0f) return;

        min = Mathf.Min(1f, Mathf.Max(min, MinVisibleSize / longSide));
        max = Mathf.Max(1f, Mathf.Min(max, MaxVisibleSize / longSide));
    }

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

    // 기울기·반전은 아이콘 그림 그대로(0도, 뒤집지 않음)를 기준으로 한 배치 값이다(DecorationPose).
    // 좌우 반전은 "보이는 모습을 거울에 비춘 것"이 되게 한다. 그래서 반전하면 기울기 방향도 반대가 된다.

    /// <summary>보이는 기울기(월드 각도, 도).</summary>
    public static float AngleFor(CustomizeItemData item, DecorationPose pose)
        => pose.flipX ? -pose.rotation : pose.rotation;

    /// <summary>보이는 기울기가 <paramref name="angle"/>이 되게 하는 <see cref="DecorationPose.rotation"/>. (<see cref="AngleFor"/>의 반대)</summary>
    public static float PoseRotationFor(CustomizeItemData item, bool flipX, float angle)
        => flipX ? -angle : angle;

    /// <summary>그림을 좌우로 뒤집어 그리는가.</summary>
    public static bool FlipFor(CustomizeItemData item, DecorationPose pose) => pose.flipX;

    /// <summary>
    /// 그 장식품을 그 모습으로 놓았을 때 차지하는 영역(2D, 실제 그림 영역).
    /// 기울어 있으면 기울인 그림을 감싸는 사각형.
    /// </summary>
    public static Rect RectAt(CustomizeItemData item, DecorationPose pose)
    {
        float scale = ScaleFor(item) * pose.scale;
        var a = SpriteArea(item.icon);
        Vector2 half = a.size * scale * 0.5f;

        float rad = AngleFor(item, pose) * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(rad)), sin = Mathf.Abs(Mathf.Sin(rad));
        Vector2 extents = new Vector2(half.x * cos + half.y * sin, half.x * sin + half.y * cos);

        Vector2 center = pose.position + Rotate(LocalAreaCenter(item, pose) * scale, rad);
        return new Rect(center - extents, extents * 2f);
    }

    /// <summary>기본 모습(크기·기울기·반전 조절 없음)으로 피벗이 그 자리에 오게 놓았을 때의 영역.</summary>
    public static Rect RectAt(CustomizeItemData item, Vector2 pivotPosition) => RectAt(item, new DecorationPose(pivotPosition));

    /// <summary>
    /// 실제 그림 영역의 가운데가 <paramref name="center"/>에 오도록 할 때의 피벗 위치.
    /// <paramref name="pose"/>의 위치는 쓰지 않고 크기·기울기·반전만 쓴다.
    /// </summary>
    public static Vector2 PivotForCenter(CustomizeItemData item, Vector2 center, DecorationPose pose)
        => center - Rotate(LocalAreaCenter(item, pose) * (ScaleFor(item) * pose.scale), AngleFor(item, pose) * Mathf.Deg2Rad);

    /// <summary>기본 모습으로 놓을 때 실제 그림 영역의 가운데가 그 자리에 오게 하는 피벗 위치.</summary>
    public static Vector2 PivotForCenter(CustomizeItemData item, Vector2 center) => PivotForCenter(item, center, new DecorationPose(center));

    /// <summary>실제 그림 영역의 가운데(피벗 기준, 크기·기울기 적용 전). 좌우로 뒤집어 그리면 x도 뒤집힌다.</summary>
    private static Vector2 LocalAreaCenter(CustomizeItemData item, DecorationPose pose)
    {
        var c = SpriteArea(item.icon).center;
        if (FlipFor(item, pose)) c.x = -c.x;
        return c;
    }

    private static Vector2 Rotate(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
