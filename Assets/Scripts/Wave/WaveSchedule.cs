using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 타이밍 단일 기준에 대한 런타임 접근자.
/// 우선순위: Resources의 <see cref="WaveScheduleConfig"/> 에셋 → (없으면) 코드 내장 기본값.
/// 내장 기본값은 에디터 메뉴가 에셋을 생성할 때 쓰는 시드이자, 에셋 누락 시의 안전 폴백이다.
/// </summary>
public static class WaveSchedule
{
    public const string ResourcePath = "Data/WaveScheduleConfig";

    private static bool _loaded;
    private static WaveScheduleConfig _config;
    private static int _seasonLength = 5;
    private static int _shopUnlockLeadStages = DefaultShopUnlockLeadStages;
    private static Dictionary<WaveType, WaveScheduleEntry> _byType;
    private static readonly Dictionary<WaveType, int> _firstAppearCache = new Dictionary<WaveType, int>();

    // ── 코드 내장 기본값(= 에셋 시드 + 폴백) ─────────────────────────────────
    // 계절 enum: Summer, Fall, Winter, Spring
    public static List<WaveScheduleEntry> BuildDefaultEntries()
    {
        // unlockStage 리터럴은 각 Wave 클래스의 UnlockStage 상수를 시드로 참조한다(중복 리터럴 방지).
        // 계절·트리거는 여기서 정의한다. 런타임 권위는 생성된 에셋이며, 이 표는 시드/폴백이다.
        return new List<WaveScheduleEntry>
        {
            New(WaveType.Aging,     AgingWave.UnlockStage,     null,                                  null),
            New(WaveType.Pest,      PestWave.UnlockStage,      null,                                  "PestUnlock"),
            New(WaveType.Wind,      WindWave.UnlockStage,      null,                                  "WindUnlock"),
            New(WaveType.Flood,     FloodWave.UnlockStage,     null,                                  "FloodUnlock"),
            New(WaveType.HeavyRain, HeavyRainWave.UnlockStage, new[]{ Season.Summer, Season.Fall },   "HeavyRainUnlock"),
            New(WaveType.Cold,      ColdWave.UnlockStage,      new[]{ Season.Fall,   Season.Winter }, "ColdUnlock"),
            New(WaveType.Drought,   DroughtWave.UnlockStage,   new[]{ Season.Spring, Season.Winter }, "DroughtUnlock"),
            New(WaveType.Heat,      HeatWave.UnlockStage,      new[]{ Season.Summer, Season.Spring },  "HeatUnlock"),
            New(WaveType.None,      NoneWave.UnlockStage,      null,                                  null),
        };
    }

    public const int DefaultSeasonLength = 5;
    public const int DefaultShopUnlockLeadStages = 2;

    private static WaveScheduleEntry New(WaveType t, int unlock, Season[] seasons, string trigger)
    {
        return new WaveScheduleEntry { waveType = t, unlockStage = unlock, allowedSeasons = seasons, unlockTriggerId = trigger };
    }

    // ── 로드 ────────────────────────────────────────────────────────────────
    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _config = Resources.Load<WaveScheduleConfig>(ResourcePath);
        _byType = new Dictionary<WaveType, WaveScheduleEntry>();

        List<WaveScheduleEntry> entries;
        if (_config != null && _config.waves != null && _config.waves.Count > 0)
        {
            entries = _config.waves;
            _seasonLength = _config.seasonLength > 0 ? _config.seasonLength : DefaultSeasonLength;
            _shopUnlockLeadStages = Mathf.Max(0, _config.shopUnlockLeadStages);
        }
        else
        {
            entries = BuildDefaultEntries();
            _seasonLength = DefaultSeasonLength;
            _shopUnlockLeadStages = DefaultShopUnlockLeadStages;
            Debug.LogWarning($"[WaveSchedule] Resources/{ResourcePath} 에셋이 없어 코드 내장 기본값을 사용합니다. " +
                             "Tools > Create Wave Schedule Config 로 에셋을 생성하세요.");
        }

        foreach (var e in entries)
            _byType[e.waveType] = e;

        _firstAppearCache.Clear();
    }

    /// <summary>에디터/테스트에서 캐시를 강제로 비울 때 사용.</summary>
    public static void Reload()
    {
        _loaded = false;
        _firstAppearCache.Clear();
        EnsureLoaded();
    }

    private static WaveScheduleEntry Get(WaveType type)
    {
        EnsureLoaded();
        return _byType.TryGetValue(type, out var e) ? e : null;
    }

    // ── 공개 API ─────────────────────────────────────────────────────────────
    public static int SeasonLength { get { EnsureLoaded(); return _seasonLength; } }
    public static int ShopUnlockLeadStages { get { EnsureLoaded(); return _shopUnlockLeadStages; } }
    // 벌레 엔티티 타이밍은 별도 단일 기준 BugSchedule 참조.

    /// <summary>
    /// 상점에서 해당 형질/웨이브 아이템이 처음 해금되는 스테이지.
    /// = 실제 첫 등장 - 리드타임. 기본 리드 2 → "곧 등장해요" 사전 경고(3,8,13…)와 동기화.
    /// </summary>
    public static int GetShopUnlockStage(WaveType type)
    {
        return Mathf.Max(1, GetFirstAppearStage(type) - ShopUnlockLeadStages);
    }

    public static Season GetSeasonByStage(int stage)
    {
        int len = SeasonLength <= 0 ? DefaultSeasonLength : SeasonLength;
        return (Season)(((stage - 1) / len) % 4);
    }

    public static int GetUnlockStage(WaveType type)
    {
        var e = Get(type);
        return e != null ? e.unlockStage : 999;
    }

    public static bool IsSeasonAllowed(WaveType type, Season season)
    {
        var e = Get(type);
        if (e == null || e.allowedSeasons == null || e.allowedSeasons.Length == 0)
            return true; // 제약 없음 = 전 계절
        for (int i = 0; i < e.allowedSeasons.Length; i++)
            if (e.allowedSeasons[i] == season) return true;
        return false;
    }

    public static string GetUnlockTriggerId(WaveType type)
    {
        var e = Get(type);
        return e != null ? e.unlockTriggerId : null;
    }

    /// <summary>
    /// 실제 첫 등장 스테이지. 가중치 해금(unlockStage-1) + 계절 제약 + 미리보기 1스테이지 선행을 반영.
    /// 비계절 웨이브는 unlockStage-1, 계절 웨이브는 그 이후 첫 제철.
    /// 결과: Pest5, Wind10, Flood15, HeavyRain20, Cold25, Drought30, Heat35.
    /// </summary>
    public static int GetFirstAppearStage(WaveType type)
    {
        EnsureLoaded();
        if (_firstAppearCache.TryGetValue(type, out var cached))
            return cached;

        var e = Get(type);
        int result = 999;
        if (e != null)
        {
            int start = Mathf.Max(1, e.unlockStage - 1);
            bool noSeason = e.allowedSeasons == null || e.allowedSeasons.Length == 0;
            for (int x = start; x <= start + 400; x++)
            {
                // 스테이지 X용 nextWave 픽은 stage X-1에서 일어나고, 그때의 계절 필터는 GetSeasonByStage(X+1)
                if (noSeason || IsSeasonAllowed(type, GetSeasonByStage(x + 1)))
                {
                    result = x;
                    break;
                }
            }
        }

        _firstAppearCache[type] = result;
        return result;
    }
}
