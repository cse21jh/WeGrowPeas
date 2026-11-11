using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 프로필이 보유(구매·획득)한 치장 아이템. 프로필마다 따로 저장된다(<see cref="CustomizeProfile"/>).
///
/// 아이템 상태는 세 단계다.
///   잠김:  requiresUnlock인데 아직 해금 안 됨 → 목록에 안 뜬다. (해금은 UnlockManager, 역시 프로필별)
///   미보유: 해금됐지만 아직 사지 않음 → 목록에 뜨지만 장착할 수 없다.
///   보유:  샀거나 기본 아이템 → 장착할 수 있다.
///
/// 저장 흐름은 다른 프로필 시스템(UnlockManager 등)과 같다.
///   SaveManager가 프로필을 읽을 때 LoadFrom, 저장할 때 SaveTo를 부른다.
///   보유가 바뀌면 SaveManager.RequestProfileSave()로 저장을 요청한다.
/// </summary>
public static class CustomizeInventory
{
    private static HashSet<string> _owned = new HashSet<string>();

    /// <summary>보유가 바뀌었다(새로 얻은 아이템).</summary>
    public static event Action<CustomizeItemData> OwnedChanged;

    // 도메인 리로드를 끈 상태에서 이전 Play의 보유 목록이 남지 않게 한다.
    // 실제 값은 그 뒤 SaveManager가 프로필을 읽으면서 채운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _owned = new HashSet<string>();
        OwnedChanged = null;
    }

    // ── 프로필 저장 ───────────────────────────────────────────────────────────

    /// <summary>프로필에 담는다. <see cref="LoadFrom"/>과 짝.</summary>
    public static void SaveTo(CustomizeProfile profile)
    {
        profile.ownedIds = new List<string>(_owned);
    }

    /// <summary>프로필에서 읽는다. null이면 새 프로필(기본 아이템만 보유).</summary>
    public static void LoadFrom(CustomizeProfile profile)
    {
        _owned = profile?.ownedIds != null ? new HashSet<string>(profile.ownedIds) : new HashSet<string>();
    }

    // ── 조회 ──────────────────────────────────────────────────────────────────

    /// <summary>보유 중인가. 기본 아이템은 항상 보유.</summary>
    public static bool IsOwned(CustomizeItemData item)
    {
        if (item == null) return false;
        return item.isDefault || _owned.Contains(item.Id);
    }

    // ── 획득 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 보유 목록에 넣는다(구매 완료, 보상 지급 등). 이미 있으면 아무 일도 없다.
    /// 가격 차감 같은 구매 절차는 부르는 쪽에서 먼저 처리한다.
    /// </summary>
    public static void Grant(CustomizeItemData item)
    {
        if (item == null || IsOwned(item)) return;
        _owned.Add(item.Id);
        SaveManager.RequestProfileSave();
        Debug.Log($"[Customize] 획득: {item.Id}");
        OwnedChanged?.Invoke(item);
    }

    /// <summary>테스트용: 보유 목록에서 뺀다. 기본 아이템은 빼도 계속 보유로 보인다.</summary>
    public static void Revoke(CustomizeItemData item)
    {
        if (item != null && _owned.Remove(item.Id)) SaveManager.RequestProfileSave();
    }
}
