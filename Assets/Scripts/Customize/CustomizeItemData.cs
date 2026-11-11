using UnityEngine;

/// <summary>치장 아이템을 얻는 방법. 아이템 종류(category)로 정해진다.</summary>
public enum CustomizeUnlockMethod
{
    /// <summary>치장 토큰을 내고 구매한다. (토양·목재·풀밭·케이스)</summary>
    Token,
    /// <summary>지정한 업적을 달성하면 자동으로 해금된다. (장식품)</summary>
    Achievement,
}

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

    [Tooltip("이 종류의 기본 아이템. 처음부터 보유한다. 종류마다 하나만 둔다.")]
    public bool isDefault;

    [Header("모습")]
    [Tooltip("목록 슬롯에 보이는 아이콘. 비우면 빈 칸으로 보인다.")]
    public Sprite icon;

    [Tooltip("게임 화면에 실제로 그려질 모습. 비우면 아이콘을 쓴다.")]
    public Sprite appearance;

    [Header("해금")]
    [Tooltip("토양·목재·풀밭·케이스: 구매에 드는 치장 토큰 수.")]
    [Min(0)] public int tokenPrice;

    [Tooltip("장식품: 이 업적을 달성하면 자동으로 해금된다.")]
    public AchievementData requiredAchievement;

    /// <summary>저장·비교에 쓰는 식별자. 에셋 이름.</summary>
    public string Id => name;

    /// <summary>얻는 방법. 장식품은 업적, 나머지는 토큰 구매.</summary>
    public CustomizeUnlockMethod UnlockMethod
        => category == CustomizeCategory.Decoration ? CustomizeUnlockMethod.Achievement : CustomizeUnlockMethod.Token;

    /// <summary>게임 화면에 그릴 스프라이트.</summary>
    public Sprite AppearanceOrIcon => appearance != null ? appearance : icon;
}
