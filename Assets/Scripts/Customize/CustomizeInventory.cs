using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>구매 시도 결과.</summary>
public enum CustomizePurchaseResult
{
    Success,
    AlreadyOwned,
    /// <summary>토큰으로 살 수 없는 아이템(장식품).</summary>
    NotPurchasable,
    NotEnoughTokens,
}

/// <summary>
/// 지금 프로필의 치장 토큰과 보유(해금)한 치장 아이템. 프로필마다 따로 저장된다(<see cref="CustomizeProfile"/>).
///
/// 보유 판정
///   기본 아이템(isDefault)          → 항상 보유
///   토양·목재·풀밭·케이스(토큰)       → 토큰으로 구매하면 보유 목록에 들어간다
///   장식품(업적)                     → 지정한 업적을 달성했으면 보유. 업적 기록은 AchievementSystem이
///                                      프로필에 저장하므로 여기선 따로 저장하지 않는다(예전에 달성한 업적도 바로 반영).
///
/// 저장 흐름은 다른 프로필 시스템과 같다.
///   SaveManager가 프로필을 읽을 때 LoadFrom, 저장할 때 SaveTo를 부른다.
///   값이 바뀌면 SaveManager.RequestProfileSave()로 저장을 요청한다.
/// </summary>
public static class CustomizeInventory
{
    /// <summary>새 프로필이 처음 가지고 시작하는 치장 토큰 수. (임시 값)</summary>
    public const int DefaultTokens = 300;

    private static HashSet<string> _owned = new HashSet<string>();
    private static int _tokens = DefaultTokens;

    /// <summary>보유가 바뀌었다(새로 얻은 아이템).</summary>
    public static event Action<CustomizeItemData> OwnedChanged;

    /// <summary>토큰 수가 바뀌었다(바뀐 뒤 값).</summary>
    public static event Action<int> TokensChanged;

    // 도메인 리로드를 끈 상태에서 이전 Play의 값이 남지 않게 한다.
    // 실제 값은 그 뒤 SaveManager가 프로필을 읽으면서 채운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _owned = new HashSet<string>();
        _tokens = DefaultTokens;
        OwnedChanged = null;
        TokensChanged = null;
    }

    // ── 프로필 저장 ───────────────────────────────────────────────────────────

    /// <summary>프로필에 담는다. <see cref="LoadFrom"/>과 짝.</summary>
    public static void SaveTo(CustomizeProfile profile)
    {
        profile.ownedIds = new List<string>(_owned);
        profile.tokens = _tokens;
    }

    /// <summary>프로필에서 읽는다. null이면 새 프로필(기본 아이템만 보유, 토큰 <see cref="DefaultTokens"/>개).</summary>
    public static void LoadFrom(CustomizeProfile profile)
    {
        _owned = profile?.ownedIds != null ? new HashSet<string>(profile.ownedIds) : new HashSet<string>();
        _tokens = profile != null ? Mathf.Max(0, profile.tokens) : DefaultTokens;
    }

    // ── 보유 ──────────────────────────────────────────────────────────────────

    /// <summary>보유(해금) 중인가.</summary>
    public static bool IsOwned(CustomizeItemData item)
    {
        if (item == null) return false;
        if (item.isDefault || _owned.Contains(item.Id)) return true;

        return item.UnlockMethod == CustomizeUnlockMethod.Achievement
            && item.requiredAchievement != null
            && AchievementSystem.IsUnlocked(item.requiredAchievement.id);
    }

    /// <summary>
    /// 조건과 상관없이 보유 목록에 넣는다(디버그 해금, 이벤트 보상 등).
    /// 토큰으로 사는 건 <see cref="TryPurchase"/>를 쓴다.
    /// </summary>
    public static void Grant(CustomizeItemData item)
    {
        if (item == null || IsOwned(item)) return;
        _owned.Add(item.Id);
        SaveManager.RequestProfileSave();
        Debug.Log($"[Customize] 획득: {item.Id}");
        OwnedChanged?.Invoke(item);
    }

    // ── 토큰 ──────────────────────────────────────────────────────────────────

    /// <summary>지금 가진 치장 토큰 수.</summary>
    public static int Tokens => _tokens;

    /// <summary>토큰을 준다(보상 등). 0 이하는 무시한다.</summary>
    public static void AddTokens(int amount)
    {
        if (amount <= 0) return;
        _tokens += amount;
        SaveManager.RequestProfileSave();
        TokensChanged?.Invoke(_tokens);
    }

    /// <summary>토큰을 내고 아이템을 산다. 성공하면 토큰이 줄고 보유 목록에 들어간다.</summary>
    public static CustomizePurchaseResult TryPurchase(CustomizeItemData item)
    {
        if (item == null || item.UnlockMethod != CustomizeUnlockMethod.Token) return CustomizePurchaseResult.NotPurchasable;
        if (IsOwned(item)) return CustomizePurchaseResult.AlreadyOwned;
        if (_tokens < item.tokenPrice) return CustomizePurchaseResult.NotEnoughTokens;

        _tokens -= item.tokenPrice;
        TokensChanged?.Invoke(_tokens);
        Grant(item); // 저장 요청도 여기서 함께 된다
        return CustomizePurchaseResult.Success;
    }
}
