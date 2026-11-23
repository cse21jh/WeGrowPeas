using UnityEngine;

/// <summary>
/// 배경에 원래 박혀 있는 장식품 오브젝트(통나무, 그루터기, 돌멩이 등). 그 장식품 아이템(<see cref="item"/>)과 짝을 짓는다.
///
/// <see cref="DecorationLayer"/>는 이 아이템을 그릴 때 새 오브젝트를 만들지 않고 이 오브젝트를 그대로 쓴다(그래서 겹쳐 그려지지 않는다).
///   원래 자리에 놓여 있으면 → 원래 위치·정렬 레이어·순서·z 그대로(씬에 배치한 모습 그대로)
///   다른 자리로 옮겼으면   → 그 자리로 옮기고, 앞뒤 순서는 다른 장식품과 같은 규칙(y값 기준)으로 정한다
///   치웠으면               → 숨긴다
/// 아이템의 <see cref="CustomizeItemData.defaultPosition"/>은 이 오브젝트의 위치와 같아야 한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DecorationFixture : MonoBehaviour
{
    [Tooltip("이 오브젝트가 나타내는 장식품 아이템.")]
    [SerializeField] private CustomizeItemData item;

    /// <summary>이 거리(월드 단위) 안이면 원래 자리로 본다.</summary>
    private const float SamePlaceTolerance = 0.01f;

    private bool _captured;
    private Vector3 _homePosition;
    private int _homeLayerId;
    private int _homeOrder;

    public CustomizeItemData Item => item;
    public SpriteRenderer Renderer { get; private set; }

    private void Awake() => Capture();

    /// <summary>씬에 배치된 원래 모습을 기억한다. 처음 한 번만. (DecorationLayer가 이 오브젝트의 Awake보다 먼저 부를 수도 있다)</summary>
    public void Capture()
    {
        if (_captured) return;
        _captured = true;
        Renderer = GetComponent<SpriteRenderer>();
        _homePosition = transform.position;
        _homeLayerId = Renderer.sortingLayerID;
        _homeOrder = Renderer.sortingOrder;
    }

    /// <summary>그 위치가 원래 자리인가.</summary>
    public bool IsHome(Vector2 position) => ((Vector2)_homePosition - position).sqrMagnitude <= SamePlaceTolerance * SamePlaceTolerance;

    /// <summary>원래 자리·원래 앞뒤 순서로 되돌린다.</summary>
    public void RestoreHome()
    {
        transform.position = _homePosition;
        Renderer.sortingLayerID = _homeLayerId;
        Renderer.sortingOrder = _homeOrder;
    }

    public void SetShown(bool shown)
    {
        if (gameObject.activeSelf != shown) gameObject.SetActive(shown);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (item == null) return;
        if (item.category != CustomizeCategory.Decoration)
            Debug.LogWarning($"[Customize] '{name}'에 장식품이 아닌 아이템 '{item.name}'이 들어 있습니다.", this);
        else if (!Application.isPlaying && ((Vector2)transform.position - item.defaultPosition).sqrMagnitude > 0.0025f)
            Debug.LogWarning($"[Customize] '{name}'의 위치 {(Vector2)transform.position}와 '{item.name}'의 처음 자리 {item.defaultPosition}가 다릅니다.", this);
    }
#endif
}
