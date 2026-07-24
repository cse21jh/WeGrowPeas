using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 새벽 모드(승천) 런타임 접근자. 단계별 데이터/누적 제약/해금·선택 상태를 제공.
/// - 설정: Resources의 <see cref="DawnStageConfig"/> 에셋.
/// - 해금(MaxUnlockedDawnStage): 메타 진행 → PlayerPrefs 영구 저장.
/// - 선택(SelectedDawnStage): 이번 런에 고른 단계(게임 시작 시 UI가 설정).
/// </summary>
public static class DawnSystem
{
    public const string ResourcePath = "Data/DawnStageConfig";
    private const string PrefKeyMaxUnlocked = "Dawn_MaxUnlockedStage";

    private static DawnStageConfig _config;
    private static DawnStageConfig Config
    {
        get { if (_config == null) _config = Resources.Load<DawnStageConfig>(ResourcePath); return _config; }
    }

    public static void Reload() { _config = null; }

    // ── 단계 데이터 ───────────────────────────────────────────────────────────
    public static IReadOnlyList<DawnStageData> AllStages()
        => Config != null ? (IReadOnlyList<DawnStageData>)Config.stages : new List<DawnStageData>();

    public static DawnStageData GetStage(int stage) => Config != null ? Config.Get(stage) : null;
    public static int StageCount => Config != null ? Config.stages.Count : 0;

    public static float GetGeneticsMultiplier(int stage)
    {
        var d = GetStage(stage);
        return d != null ? d.geneticsMultiplier : 1f;
    }

    // ── 식물 ──────────────────────────────────────────────────────────────────
    /// <summary>새벽 진행도를 따로 관리하는 식물 목록.</summary>
    public static readonly string[] Plants = { "완두콩", "땅콩" };

    /// <summary>
    /// 진행도를 조회·기록할 기준 식물.
    /// 시작 화면(특성·새벽 선택)에서는 AbilityManager, 인게임에서는 GameManager가 기준이 된다.
    /// </summary>
    public static string CurrentPlant
    {
        get
        {
            if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.currentPlant))
                return GameManager.Instance.currentPlant;
            if (AbilityManager.Instance != null && !string.IsNullOrEmpty(AbilityManager.Instance.CurrentPlantName))
                return AbilityManager.Instance.CurrentPlantName;
            return Plants[0];
        }
    }

    // ── 해금(메타) ────────────────────────────────────────────────────────────
    // 새벽 진행도는 식물별로 따로 저장된다. ("Dawn_MaxUnlockedStage_완두콩" 등)
    private static string PrefKeyFor(string plant) => PrefKeyMaxUnlocked + "_" + plant;

    /// <summary>지정한 식물의 해금된 최대 새벽 단계. 0 = 그 식물로는 새벽 모드 미해금.</summary>
    public static int GetMaxUnlockedStage(string plant)
    {
        MigrateLegacyProgressIfNeeded();
        return PlayerPrefs.GetInt(PrefKeyFor(plant), 0);
    }

    public static void SetMaxUnlockedStage(string plant, int stage)
    {
        MigrateLegacyProgressIfNeeded();
        PlayerPrefs.SetInt(PrefKeyFor(plant), Mathf.Max(0, stage));
        PlayerPrefs.Save();
    }

    /// <summary>현재 식물 기준 해금된 최대 새벽 단계.</summary>
    public static int MaxUnlockedDawnStage
    {
        get => GetMaxUnlockedStage(CurrentPlant);
        set => SetMaxUnlockedStage(CurrentPlant, value);
    }

    /// <summary>현재 식물로 새벽 모드가 해금됐는가(1단계 이상 열림).</summary>
    public static bool IsDawnUnlocked => MaxUnlockedDawnStage >= 1;

    public static bool IsStageUnlocked(int stage) => stage >= 0 && stage <= MaxUnlockedDawnStage;

    public static void UnlockUpTo(int stage) => UnlockUpTo(stage, CurrentPlant);

    public static void UnlockUpTo(int stage, string plant)
    {
        if (stage > GetMaxUnlockedStage(plant)) SetMaxUnlockedStage(plant, stage);
    }

    /// <summary>
    /// 지정한 식물로 클리어한 최대 새벽 단계. 0 = 그 식물로 새벽 단계를 클리어한 적 없음.
    /// N단계를 클리어하면 N+1단계가 해금되므로 해금 단계 - 1이 곧 클리어 단계다.
    /// (아이템 해금 판정은 UnlockManager로 옮겨졌고, 이 값은 진행도 표시/디버그에 사용)
    /// </summary>
    public static int GetMaxClearedStage(string plant) => Mathf.Max(0, GetMaxUnlockedStage(plant) - 1);

    /// <summary>
    /// 이번 런을 엔딩까지 클리어했을 때 호출. 이번 런의 식물에 대해 다음 새벽 단계를 해금한다.
    /// (일반 모드 클리어 → 새벽 1단계 해금, 새벽 N단계 클리어 → N+1단계 해금)
    /// </summary>
    public static void RecordRunCleared()
    {
        string plant = CurrentPlant;
        UnlockUpTo(SelectedDawnStage + 1, plant);

        // 이번 런에서 클리어한 새벽 단계에 맞춰, 조건을 만족하는 상점/특수 아이템을 실제로 해금(기록).
        UnlockGrants.GrantDawnClearUnlocks(plant, SelectedDawnStage);

        Debug.Log($"[Dawn] {plant} 클리어 기록: {SelectedDawnStage}단계 → {GetMaxUnlockedStage(plant)}단계까지 해금");
    }

    // ── 레거시 마이그레이션 ───────────────────────────────────────────────────
    // 식물 구분이 없던 시절의 단일 키를 각 식물로 1회 이관한다(진행도 손실 방지).
    private const string PrefKeyMigrated = "Dawn_MaxUnlockedStage_Migrated";
    private static bool _migrationChecked;

    private static void MigrateLegacyProgressIfNeeded()
    {
        if (_migrationChecked) return;
        _migrationChecked = true;

        if (PlayerPrefs.GetInt(PrefKeyMigrated, 0) == 1) return;
        PlayerPrefs.SetInt(PrefKeyMigrated, 1);

        int legacy = PlayerPrefs.GetInt(PrefKeyMaxUnlocked, 0);
        if (legacy > 0)
        {
            foreach (var p in Plants)
                if (PlayerPrefs.GetInt(PrefKeyFor(p), 0) < legacy)
                    PlayerPrefs.SetInt(PrefKeyFor(p), legacy);
            Debug.Log($"[Dawn] 기존 새벽 진행도({legacy}단계)를 식물별로 이관했습니다.");
        }
        PlayerPrefs.Save();
    }

    /// <summary>테스트용: 모든 식물의 새벽 진행도 초기화.</summary>
    public static void ResetAllPlantProgress()
    {
        MigrateLegacyProgressIfNeeded();
        foreach (var p in Plants) PlayerPrefs.SetInt(PrefKeyFor(p), 0);
        PlayerPrefs.Save();
    }

    // ── 선택(이번 런) ─────────────────────────────────────────────────────────
    /// <summary>이번 런에 선택한 새벽 단계. 0 = 새벽 모드 아님(일반).</summary>
    public static int SelectedDawnStage { get; private set; } = 0;
    public static void SetSelectedStage(int stage) { SelectedDawnStage = stage; _currentValid = false; }
    public static void ClearSelection() { SelectedDawnStage = 0; _currentValid = false; }

    private static bool _currentValid;
    private static DawnCumulative _current;
    /// <summary>선택된 새벽 단계의 누적 제약(런타임 각 시스템이 참조). 저주는 여기서 처리하지 않음.</summary>
    public static DawnCumulative Current
    {
        get
        {
            if (!_currentValid) { _current = GetCumulative(SelectedDawnStage); _currentValid = true; }
            return _current;
        }
    }

    // ── 누적 제약 ─────────────────────────────────────────────────────────────
    /// <summary>1..stage 의 제약 설명을 줄바꿈으로 이어붙여 반환(UI 표시용).</summary>
    public static string GetCumulativeConstraintText(int stage)
    {
        if (Config == null) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var d in Config.stages)
        {
            if (d == null || d.stage < 1 || d.stage > stage) continue;
            if (string.IsNullOrWhiteSpace(d.constraintDescription)) continue;
            sb.AppendLine($"• [{d.stage}] {d.constraintDescription}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 선택 단계의 "누적" 제약을 의미별로 1줄씩(병합) 리치텍스트로 반환.
    /// 이번 단계에서 새로 생기거나 값이 바뀐 제약만 색으로 강조.
    /// (예: 12단계에서 매일 저항력 감소가 5%p→10%p면 "10%p" 한 줄만, 색 강조)
    /// </summary>
    public static string GetConstraintSummaryRich(int stage, string changeColorHex = "#FFD24F")
    {
        var cur = GetCumulative(stage);
        var prev = GetCumulative(Mathf.Max(0, stage - 1));
        var sb = new System.Text.StringBuilder();

        AddLine(sb, cur.curseLevel > 0, $"저주 {cur.curseLevel}단계",
            cur.curseLevel != prev.curseLevel, changeColorHex);
        AddLine(sb, cur.mutationChanceAddPercent > 0f, $"교배 시 변종 발생 확률 +{cur.mutationChanceAddPercent:0.#}%p",
            cur.mutationChanceAddPercent != prev.mutationChanceAddPercent, changeColorHex);
        AddLine(sb, cur.bugPriceReduction > 0 || cur.bugDelayReduction > 0f,
            $"벌레 기본 가격 -{cur.bugPriceReduction}, 등장 딜레이 -{cur.bugDelayReduction:0.#}초",
            cur.bugPriceReduction != prev.bugPriceReduction || cur.bugDelayReduction != prev.bugDelayReduction, changeColorHex);
        AddLine(sb, cur.shopPriceMultiplier > 1f, $"상점 아이템 가격 x{cur.shopPriceMultiplier:0.##}",
            cur.shopPriceMultiplier != prev.shopPriceMultiplier, changeColorHex);
        AddLine(sb, cur.dailyResistanceDecayAddPercent > 0f, $"매일 모든 저항력 -{cur.dailyResistanceDecayAddPercent:0.#}%p",
            cur.dailyResistanceDecayAddPercent != prev.dailyResistanceDecayAddPercent, changeColorHex);
        AddLine(sb, cur.taxMultiplier > 1f, $"세금 x{cur.taxMultiplier:0.##}",
            cur.taxMultiplier != prev.taxMultiplier, changeColorHex);
        AddLine(sb, cur.resistanceCapReductionPercent > 0f, $"저항력 상한 -{cur.resistanceCapReductionPercent:0.#}%p",
            cur.resistanceCapReductionPercent != prev.resistanceCapReductionPercent, changeColorHex);
        AddLine(sb, cur.rootChancePercent > 0f, $"식물 등장·이동 시 {cur.rootChancePercent:0.#}% 확률로 뿌리(이동 불가)",
            cur.rootChancePercent != prev.rootChancePercent, changeColorHex);

        return sb.ToString();
    }

    private static void AddLine(System.Text.StringBuilder sb, bool active, string text, bool changed, string colorHex)
    {
        if (!active) return;
        if (changed) sb.AppendLine($"<color={colorHex}>• {text}</color>");
        else sb.AppendLine($"• {text}");
    }

    /// <summary>1..stage 의 제약 수치를 누적(가산은 합, 배수는 곱)해서 반환(향후 적용용).</summary>
    public static DawnCumulative GetCumulative(int stage)
    {
        var c = new DawnCumulative();
        c.shopPriceMultiplier = 1f; // struct 기본값 0 → 곱셈 위해 1로
        c.taxMultiplier = 1f;
        if (Config == null) return c;
        foreach (var d in Config.stages)
        {
            if (d == null || d.stage < 1 || d.stage > stage) continue;
            c.curseLevel = Mathf.Max(c.curseLevel, d.curseLevel);
            c.mutationChanceAddPercent += d.mutationChanceAddPercent;
            c.bugPriceReduction += d.bugPriceReduction;
            c.bugDelayReduction += d.bugDelayReduction;
            c.shopPriceMultiplier *= (d.shopPriceMultiplier <= 0 ? 1f : d.shopPriceMultiplier);
            c.dailyResistanceDecayAddPercent += d.dailyResistanceDecayAddPercent;
            c.taxMultiplier *= (d.taxMultiplier <= 0 ? 1f : d.taxMultiplier);
            c.resistanceCapReductionPercent += d.resistanceCapReductionPercent;
            c.rootChancePercent += d.rootChancePercent;
        }
        return c;
    }
}

/// <summary>선택 단계까지 누적된 제약 수치(향후 각 시스템이 참조).</summary>
public struct DawnCumulative
{
    public int curseLevel;
    public float mutationChanceAddPercent;
    public int bugPriceReduction;
    public float bugDelayReduction;
    public float shopPriceMultiplier;
    public float dailyResistanceDecayAddPercent;
    public float taxMultiplier;
    public float resistanceCapReductionPercent;
    public float rootChancePercent;
}
