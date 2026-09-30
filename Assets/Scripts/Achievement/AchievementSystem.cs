using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 외부 플랫폼(Steam 등)에 업적 달성을 알리는 창구.
/// Steamworks를 넣으면 이걸 구현해 <see cref="AchievementSystem.Platform"/>에 넣는다.
///   예) SetAchievement(id) → SteamUserStats.SetAchievement(id); SteamUserStats.StoreStats();
/// </summary>
public interface IAchievementPlatform
{
    /// <summary>달성을 알린다. 이미 달성된 업적에 다시 불려도 안전해야 한다.</summary>
    void SetAchievement(string id);
}

/// <summary>
/// 업적 판정·기록. 정의는 <see cref="AchievementData"/> 에셋, 달성 기록은 프로필(<see cref="AchievementProfile"/>)에 있다.
///
/// 판정 경로
///   LifetimeStat: CodexProgress 통계가 바뀔 때(OnStatChanged) 그 키에 걸린 업적만 확인한다.
///   Manual:       코드에서 <see cref="Unlock"/>을 직접 부른다.
/// 프로필을 불러온 직후에도 전체를 한 번 확인해, 나중에 추가된 업적을 이미 조건을 채운 유저가 소급해 받는다.
/// </summary>
public static class AchievementSystem
{
    public const string ResourcePath = "Data/Achievements";

    /// <summary>플랫폼 연동(Steam 등). 없으면 로컬 기록만 한다.</summary>
    public static IAchievementPlatform Platform { get; set; }

    /// <summary>업적을 새로 달성했다. (알림 여부와 무관하게 발생)</summary>
    public static event Action<AchievementData> OnUnlocked;

    private static AchievementData[] _all;
    private static Dictionary<string, AchievementData> _byId;
    private static Dictionary<string, List<AchievementData>> _byStat;
    private static bool _hooked;

    // 현재 프로필의 달성 기록: id → 달성 시각(UTC 유닉스 초)
    private static readonly Dictionary<string, long> _unlocked = new();

    // ── 정의 ──────────────────────────────────────────────────────────────────

    private static void EnsureInit()
    {
        if (!_hooked)
        {
            CodexProgress.OnStatChanged += HandleStatChanged;
            _hooked = true;
        }
        if (_all != null) return;

        var loaded = Resources.LoadAll<AchievementData>(ResourcePath);
        Array.Sort(loaded, (a, b) => a.sortOrder != b.sortOrder
            ? a.sortOrder.CompareTo(b.sortOrder)
            : string.CompareOrdinal(a.id, b.id));

        _all = loaded;
        _byId = new Dictionary<string, AchievementData>();
        _byStat = new Dictionary<string, List<AchievementData>>();
        foreach (var a in _all)
        {
            if (a == null || string.IsNullOrEmpty(a.id)) continue;
            if (_byId.ContainsKey(a.id))
            {
                Debug.LogError($"[Achievement] id 중복: {a.id}", a);
                continue;
            }
            _byId[a.id] = a;

            if (a.condition == AchievementCondition.LifetimeStat && !string.IsNullOrEmpty(a.statKey))
            {
                if (!_byStat.TryGetValue(a.statKey, out var list)) _byStat[a.statKey] = list = new List<AchievementData>();
                list.Add(a);
            }
        }
    }

    /// <summary>정렬된 전체 업적.</summary>
    public static IReadOnlyList<AchievementData> All
    {
        get { EnsureInit(); return _all; }
    }

    // ── 조회 ──────────────────────────────────────────────────────────────────

    public static bool IsUnlocked(string id) => !string.IsNullOrEmpty(id) && _unlocked.ContainsKey(id);

    public static bool TryGetUnlockedAt(string id, out DateTime local)
    {
        local = default;
        if (string.IsNullOrEmpty(id) || !_unlocked.TryGetValue(id, out long unix)) return false;
        local = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
        return true;
    }

    /// <summary>진행도(0~target). Manual은 달성 여부만 있다.</summary>
    public static int GetProgress(AchievementData a)
    {
        if (a == null) return 0;
        if (IsUnlocked(a.id)) return a.target;
        if (a.condition == AchievementCondition.LifetimeStat)
            return Mathf.Clamp(CodexProgress.GetStat(a.statKey), 0, a.target);
        return 0;
    }

    // ── 달성 ──────────────────────────────────────────────────────────────────

    /// <summary>Manual 업적을 달성 처리한다. 이미 달성했으면 아무 일도 없다.</summary>
    public static void Unlock(string id)
    {
        EnsureInit();
        if (string.IsNullOrEmpty(id) || !_byId.TryGetValue(id, out var a))
        {
            Debug.LogWarning($"[Achievement] 없는 업적 id: {id}");
            return;
        }
        Grant(a, notify: true);
    }

    /// <summary>모든 LifetimeStat 업적을 확인한다. 프로필을 불러온 직후 소급 지급용.</summary>
    public static void EvaluateAll(bool notify)
    {
        EnsureInit();
        foreach (var a in _all)
            if (a != null && a.condition == AchievementCondition.LifetimeStat && CodexProgress.GetStat(a.statKey) >= a.target)
                Grant(a, notify);
    }

    private static void HandleStatChanged(string key, int value)
    {
        EnsureInit();
        if (!_byStat.TryGetValue(key, out var list)) return;
        foreach (var a in list)
            if (value >= a.target) Grant(a, notify: true);
    }

    private static void Grant(AchievementData a, bool notify)
    {
        if (_unlocked.ContainsKey(a.id)) return;

        _unlocked[a.id] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveManager.RequestProfileSave();
        Platform?.SetAchievement(a.id);
        Debug.Log($"[Achievement] 달성: {a.id} ({a.title})");

        if (notify && UIManager.Instance != null && UIManager.Instance.Popup != null)
            UIManager.Instance.Popup.ShowToastPopup("업적 달성", a.title, a.icon, 3f);

        OnUnlocked?.Invoke(a);
    }

    // ── 프로필 저장 ───────────────────────────────────────────────────────────

    /// <summary>프로필에 담는다. <see cref="LoadFrom"/>과 짝.</summary>
    public static void SaveTo(AchievementProfile profile)
    {
        profile.unlocked.Clear();
        profile.unlockedAtUnix.Clear();
        foreach (var kv in _unlocked)
        {
            profile.unlocked.Add(kv.Key);
            profile.unlockedAtUnix.Add(kv.Value);
        }
    }

    public static void LoadFrom(AchievementProfile profile)
    {
        _unlocked.Clear();
        if (profile?.unlocked == null) return;
        for (int i = 0; i < profile.unlocked.Count; i++)
        {
            string id = profile.unlocked[i];
            if (string.IsNullOrEmpty(id)) continue;
            long at = profile.unlockedAtUnix != null && i < profile.unlockedAtUnix.Count ? profile.unlockedAtUnix[i] : 0;
            _unlocked[id] = at;
        }
    }

    /// <summary>
    /// 프로필을 불러온 뒤 호출. 소급 지급(알림 없이)하고, 플랫폼에 기존 달성을 다시 알린다
    /// (플랫폼 연동 전에 달성한 업적이나 다른 PC에서 옮겨 온 세이브를 맞추기 위해).
    /// </summary>
    public static void OnProfileLoaded()
    {
        EvaluateAll(notify: false);
        if (Platform != null)
            foreach (var id in _unlocked.Keys) Platform.SetAchievement(id);
    }
}
