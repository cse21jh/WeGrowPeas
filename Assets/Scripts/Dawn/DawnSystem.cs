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

    // ── 해금(메타) ────────────────────────────────────────────────────────────
    /// <summary>해금된 최대 새벽 단계. 0 = 새벽 모드 미해금(40스테이지 엔딩 전).</summary>
    public static int MaxUnlockedDawnStage
    {
        get => PlayerPrefs.GetInt(PrefKeyMaxUnlocked, 0); // 기본 0 = 미해금
        set { PlayerPrefs.SetInt(PrefKeyMaxUnlocked, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    /// <summary>새벽 모드 자체가 해금됐는가(1단계 이상 열림).</summary>
    public static bool IsDawnUnlocked => MaxUnlockedDawnStage >= 1;

    public static bool IsStageUnlocked(int stage) => stage >= 1 && stage <= MaxUnlockedDawnStage;

    public static void UnlockUpTo(int stage)
    {
        if (stage > MaxUnlockedDawnStage) MaxUnlockedDawnStage = stage;
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
