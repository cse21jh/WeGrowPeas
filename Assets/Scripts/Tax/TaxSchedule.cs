using UnityEngine;

/// <summary>
/// 세금 일정 단일 기준 접근자. Resources의 <see cref="TaxConfig"/> 에셋 → (없으면) 코드 기본값.
/// 어떤 스테이지가 세금일인지, 그 금액이 얼마인지를 여기서만 파생한다.
/// </summary>
public static class TaxSchedule
{
    public const string ResourcePath = "Data/TaxConfig";

    public const int DefaultInterval = 5;
    public static readonly int[] DefaultSchedule = { 1000, 3000, 8000, 22000, 60000, 150000 };
    public const float DefaultBeyondGrowth = 2f;
    public const float DefaultEndlessGrowth = 3f;

    private static bool _loaded;
    private static int _interval = DefaultInterval;
    private static int[] _schedule = DefaultSchedule;
    private static float _beyondGrowth = DefaultBeyondGrowth;
    private static float _endlessGrowth = DefaultEndlessGrowth;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var cfg = Resources.Load<TaxConfig>(ResourcePath);
        if (cfg != null)
        {
            _interval = cfg.interval > 0 ? cfg.interval : DefaultInterval;
            _schedule = (cfg.schedule != null && cfg.schedule.Length > 0) ? cfg.schedule : DefaultSchedule;
            _beyondGrowth = cfg.beyondTableGrowth > 0 ? cfg.beyondTableGrowth : DefaultBeyondGrowth;
            _endlessGrowth = cfg.endlessGrowth > 0 ? cfg.endlessGrowth : DefaultEndlessGrowth;
        }
        else
        {
            Debug.LogWarning($"[TaxSchedule] Resources/{ResourcePath} 에셋이 없어 코드 기본값을 사용합니다. " +
                             "Tools > Create Tax Config 로 에셋을 생성하세요.");
        }
    }

    public static void Reload()
    {
        _loaded = false;
        EnsureLoaded();
    }

    public static int Interval { get { EnsureLoaded(); return _interval; } }

    /// <summary>이 스테이지가 세금 마감일(interval 배수)인가.</summary>
    public static bool IsTaxStage(int stage)
    {
        EnsureLoaded();
        return stage > 0 && _interval > 0 && stage % _interval == 0;
    }

    /// <summary>현재 스테이지 "이후"의 첫 세금일.</summary>
    public static int GetNextTaxStage(int stage)
    {
        EnsureLoaded();
        if (_interval <= 0) return int.MaxValue;
        return (stage / _interval + 1) * _interval;
    }

    public static int GetTaxAmount(int taxStage)
    {
        EnsureLoaded();
        if (_interval <= 0 || taxStage < _interval) return 0;

        // 무한 모드(엔딩 뒤): 마지막으로 낸 세금(엔딩 전날 마감분)에서 납부 주기마다 endlessGrowth배씩 오른다.
        // 엔딩 날 세금은 걷지 않으므로 그날을 건너뛰고 이어서 계산한다.
        int endlessSteps = 0;
        int effectiveTaxStage = taxStage;
        if (taxStage > EndlessMode.StartStage)
        {
            endlessSteps = (taxStage - EndlessMode.StartStage + _interval - 1) / _interval;
            effectiveTaxStage = EndlessMode.StartStage - _interval;
        }

        int idx = effectiveTaxStage / _interval - 1; // 5→0, 10→1 …
        if (idx < 0) return 0;

        int baseAmount;
        if (idx < _schedule.Length)
        {
            baseAmount = _schedule[idx];
        }
        else
        {
            // 표 범위를 넘은 관문: 마지막 표값에서 지수적으로 계속 증가
            int over = idx - (_schedule.Length - 1);
            int last = _schedule[_schedule.Length - 1];
            float growth = _beyondGrowth > 0 ? _beyondGrowth : DefaultBeyondGrowth;
            baseAmount = Mathf.RoundToInt(last * Mathf.Pow(growth, over));
        }

        float amount = baseAmount;
        if (endlessSteps > 0)
            amount *= Mathf.Pow(_endlessGrowth > 0 ? _endlessGrowth : DefaultEndlessGrowth, endlessSteps);

        // 새벽 세금 배수 적용. 무한 모드 세금은 int 범위를 넘을 수 있으므로 상한에서 멈춘다.
        float dawnMul = GameRun.Dawn.taxMultiplier;
        amount *= dawnMul > 0f ? dawnMul : 1f;
        return amount >= int.MaxValue ? int.MaxValue : Mathf.RoundToInt(amount);
    }
}
