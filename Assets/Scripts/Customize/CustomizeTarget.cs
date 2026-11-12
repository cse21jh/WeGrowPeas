using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치장으로 그림이 바뀌는 부위 하나(<see cref="CustomizePart"/>)를 맡는다. 한 오브젝트에 여러 개 붙여도 된다.
/// 그 부위의 종류에 지금 장착된 아이템(<see cref="CustomizeState"/>)을 읽어, 아이템의 그 부위 그림으로 바꿔 끼운다.
///
/// 프리팹 원본에 붙여 두면 농장 씬과 치장 씬 양쪽에서 똑같이 동작한다.
///   - 시작할 때, 프로필을 읽었을 때, 장착이 바뀌었을 때 다시 적용한다.
///   - 아이템에 이 부위 그림이 없으면(기본 아이템 등) 프리팹에 들어 있던 원래 그림으로 돌려놓는다.
///
/// 바꾸는 방법
///   SpriteRenderer / Image → sprite를 바꾼다.
///   그 밖의 Renderer(풀 메시 등) → 머티리얼의 _MainTex를 그림의 텍스처로 바꾼다(MaterialPropertyBlock, 머티리얼 원본은 그대로).
/// </summary>
public class CustomizeTarget : MonoBehaviour
{
    [Tooltip("이 대상이 맡는 부위. 종류(토양·목재 등)는 부위에서 정해진다.")]
    [SerializeField] private CustomizePart part;

    [Tooltip("그림을 바꿀 SpriteRenderer들 (월드 오브젝트).")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Tooltip("그림을 바꿀 Image들 (UI).")]
    [SerializeField] private Image[] images;

    [Tooltip("텍스처를 바꿀 그 밖의 Renderer들 (풀 메시 등). 머티리얼의 _MainTex를 바꾼다.")]
    [SerializeField] private Renderer[] meshRenderers;

    [Tooltip("적용할 때마다 콘솔에 로그를 남긴다.")]
    [SerializeField] private bool logChanges = true;

    public CustomizePart Part => part;
    public CustomizeCategory Category => CustomizePartInfo.CategoryOf(part);

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static MaterialPropertyBlock _block;

    // 프리팹에 들어 있던 원래 그림. 아이템에 이 부위 그림이 없으면 이걸로 돌린다.
    private Sprite[] _srSprites;
    private Sprite[] _imgSprites;

    private CustomizeItemData _applied;
    private bool _hasApplied;

    private int TargetCount => (spriteRenderers?.Length ?? 0) + (images?.Length ?? 0) + (meshRenderers?.Length ?? 0);

    private void Awake()
    {
        _srSprites = new Sprite[spriteRenderers?.Length ?? 0];
        for (int i = 0; i < _srSprites.Length; i++)
            _srSprites[i] = spriteRenderers[i] != null ? spriteRenderers[i].sprite : null;

        _imgSprites = new Sprite[images?.Length ?? 0];
        for (int i = 0; i < _imgSprites.Length; i++)
            _imgSprites[i] = images[i] != null ? images[i].sprite : null;
    }

    private void OnEnable()
    {
        CustomizeState.EquippedChanged += OnEquippedChanged;
        CustomizeState.Loaded += OnStateLoaded;
    }

    private void OnDisable()
    {
        CustomizeState.EquippedChanged -= OnEquippedChanged;
        CustomizeState.Loaded -= OnStateLoaded;
    }

    private void Start() => Apply("시작");

    private void OnStateLoaded() => Apply("프로필 불러옴");

    private void OnEquippedChanged(CustomizeItemData item)
    {
        if (item != null && item.category == Category) Apply("장착 변경");
    }

    /// <summary>지금 장착된 아이템을 다시 반영한다. 이미 같은 아이템이 반영돼 있으면 아무것도 하지 않는다.</summary>
    public void Apply(string reason = "직접 호출")
    {
        var item = CustomizeState.GetEquipped(Category);
        if (_hasApplied && item == _applied) return;

        var previous = _applied;
        _applied = item;
        _hasApplied = true;

        var entry = item != null ? item.GetPart(part) : null;
        Sprite sprite = entry != null ? entry.sprite : null;

        if (logChanges)
        {
            string from = previous != null ? previous.Id : "없음";
            string to = item != null ? $"{item.displayName}({item.Id})" : "없음";
            string what = sprite != null ? sprite.name : "부위 그림 없음 → 원래 그림";
            Debug.Log($"[CustomizeTarget] {name} [{part}] {reason}: {from} → {to} | {what} | 대상 {TargetCount}개", this);
        }

        for (int i = 0; i < _srSprites.Length; i++)
            if (spriteRenderers[i] != null)
                spriteRenderers[i].sprite = sprite != null ? sprite : _srSprites[i];

        for (int i = 0; i < _imgSprites.Length; i++)
            if (images[i] != null)
                images[i].sprite = sprite != null ? sprite : _imgSprites[i];

        if (meshRenderers == null) return;
        _block ??= new MaterialPropertyBlock();
        foreach (var r in meshRenderers)
        {
            if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(MainTexId)) continue;
            r.GetPropertyBlock(_block); // 다른 스크립트가 넣어 둔 값(풀 색 등)은 유지
            _block.SetTexture(MainTexId, sprite != null ? sprite.texture : r.sharedMaterial.GetTexture(MainTexId));
            r.SetPropertyBlock(_block);
        }
    }
}
