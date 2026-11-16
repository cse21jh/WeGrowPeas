using System;

/// <summary>
/// 치장 아이템 종류. 치장 화면 왼쪽 탭은 숫자가 작은 순서대로 늘어선다(<see cref="CustomizeCategoryInfo.TabOrder"/>).
/// 탭에 보이는 글자는 씬의 TMP 텍스트에 직접 적혀 있고, CategoryTabs의 자식 순서·CustomizePanel.tabs 순서도 이 순서와 같아야 한다.
///
/// 아이템 에셋에는 종류가 숫자로 저장된다. 이미 쓰고 있는 숫자를 바꾸면 그 아이템의 종류가 바뀌므로 바꾸지 않는다.
/// 새 종류는 비어 있는 5~10 중에서 골라 붙인다. 장식품은 항상 마지막 탭이 되도록 11로 떼어 두었다.
/// </summary>
public enum CustomizeCategory
{
    Soil = 0,        // 토양
    Wood = 1,        // 목재
    Grass = 2,       // 풀밭
    PhoneCase = 3,   // 케이스
    UI = 4,          // UI
    // 5~: 새 종류용으로 비워 둠

    Decoration = 99, // 장식품 (농장에 직접 배치, 업적으로 해금)
}

public static class CustomizeCategoryInfo
{
    private static CustomizeCategory[] _tabOrder;

    /// <summary>탭에 늘어서는 순서(숫자가 작은 순). 탭 i번째 = TabOrder[i].</summary>
    public static CustomizeCategory[] TabOrder
    {
        get
        {
            if (_tabOrder == null)
            {
                _tabOrder = (CustomizeCategory[])Enum.GetValues(typeof(CustomizeCategory));
                Array.Sort(_tabOrder);
            }
            return _tabOrder;
        }
    }
}
