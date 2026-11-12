using System.Collections.Generic;
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

    [Tooltip("게임 화면에서 부위별로 바꿔 끼울 그림. 목록에 없는 부위는 원래 그림 그대로. " +
             "이 아이템 종류의 부위만 넣는다(예: 목재 → WoodFence, WoodBorder).")]
    public List<CustomizePartSprite> parts = new List<CustomizePartSprite>();

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

    /// <summary>그 부위에 쓸 그림 정보. 이 아이템이 그 부위를 바꾸지 않으면 null.</summary>
    public CustomizePartSprite GetPart(CustomizePart part)
    {
        if (parts == null) return null;
        foreach (var p in parts)
            if (p != null && p.part == part) return p;
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (parts == null) return;
        var seen = new HashSet<CustomizePart>();
        foreach (var p in parts)
        {
            if (p == null) continue;
            if (CustomizePartInfo.CategoryOf(p.part) != category)
                Debug.LogWarning($"[Customize] '{name}'({category})에 다른 종류의 부위 {p.part}({CustomizePartInfo.CategoryOf(p.part)})가 들어 있습니다. 적용되지 않습니다.", this);
            if (!seen.Add(p.part))
                Debug.LogWarning($"[Customize] '{name}'에 부위 {p.part}가 두 번 들어 있습니다. 첫 번째 것만 씁니다.", this);
        }
    }
#endif
}
