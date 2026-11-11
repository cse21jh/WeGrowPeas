using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 장착 중인 치장 아이템(종류마다 하나).
/// 아무것도 고르지 않은 종류는 그 종류의 기본 아이템(isDefault)이 장착된 것으로 본다.
/// 보유(<see cref="CustomizeInventory"/>)한 아이템만 장착할 수 있다.
/// 장착 상태는 아직 파일에 저장하지 않는다. 게임을 끄거나 Play를 다시 시작하면 기본 아이템으로 돌아간다.
/// </summary>
public static class CustomizeState
{
    private static readonly Dictionary<CustomizeCategory, CustomizeItemData> _equipped
        = new Dictionary<CustomizeCategory, CustomizeItemData>();

    /// <summary>장착이 바뀌었다(새로 장착한 아이템).</summary>
    public static event Action<CustomizeItemData> EquippedChanged;

    // 도메인 리로드를 끈 상태에서도 Play마다 처음 상태로 시작하게 한다. (CustomizeCatalog 참고)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _equipped.Clear();
        EquippedChanged = null;
    }

    /// <summary>
    /// 그 종류에 지금 장착된 아이템. 고른 적이 없거나, 고른 아이템을 지금 프로필이 보유하지 않으면
    /// (예: 다른 프로필로 바꾼 뒤) 기본 아이템. 아이템이 하나도 없으면 null.
    /// </summary>
    public static CustomizeItemData GetEquipped(CustomizeCategory category)
    {
        if (_equipped.TryGetValue(category, out var item) && CustomizeInventory.IsOwned(item)) return item;
        return CustomizeCatalog.GetDefault(category);
    }

    public static bool IsEquipped(CustomizeItemData item)
        => item != null && GetEquipped(item.category) == item;

    /// <summary>아이템을 장착한다. 같은 종류의 이전 아이템은 자동으로 빠진다. 보유하지 않은 아이템은 장착할 수 없다.</summary>
    public static void Equip(CustomizeItemData item)
    {
        if (item == null || IsEquipped(item)) return;
        if (!CustomizeInventory.IsOwned(item)) return;
        _equipped[item.category] = item;
        EquippedChanged?.Invoke(item);
    }
}
