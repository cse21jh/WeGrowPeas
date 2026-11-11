using UnityEngine;

/// <summary>
/// 치장 아이템 하나. Resources/Data/Customize 아래에 두면 <see cref="CustomizeCatalog"/>가 읽는다.
/// 식별자는 에셋 이름이다(<see cref="Id"/>).
/// </summary>
[CreateAssetMenu(fileName = "CustomizeItem", menuName = "WeGrowPeas/Customize Item")]
public class CustomizeItemData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("목록에 보이는 이름.")]
    public string displayName;

    public CustomizeCategory category;

    [Tooltip("같은 종류 안에서의 표시 순서. 작을수록 위. 같으면 에셋 이름 순.")]
    public int sortOrder;

    [Tooltip("이 종류의 기본 아이템. 종류마다 하나만 둔다.")]
    public bool isDefault;

    [Header("모습")]
    [Tooltip("목록 슬롯에 보이는 아이콘. 비우면 빈 칸으로 보인다.")]
    public Sprite icon;

    [Tooltip("게임 화면에 실제로 그려질 모습. 비우면 아이콘을 쓴다.")]
    public Sprite appearance;

    [Header("해금")]
    [Tooltip("체크하면 해금되기 전까지 목록에 뜨지 않는다. UnlockManager.Unlock(...)로 해금.")]
    public bool requiresUnlock;

    [Tooltip("해금 식별 id. 비우면 에셋 이름을 쓴다.")]
    public string unlockId;

    /// <summary>저장·비교에 쓰는 식별자. 에셋 이름.</summary>
    public string Id => name;

    public string UnlockId => string.IsNullOrEmpty(unlockId) ? name : unlockId;

    public bool IsUnlocked => !requiresUnlock || UnlockManager.IsUnlocked(UnlockId);

    /// <summary>게임 화면에 그릴 스프라이트.</summary>
    public Sprite AppearanceOrIcon => appearance != null ? appearance : icon;
}
