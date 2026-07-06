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

    private static bool _loaded;
    private static int _interval = DefaultInterval;
    private static int[] _schedule = DefaultSchedule;
    private static float _beyondGrowth = DefaultBeyondGrowth;

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

    /// <summary>해당 세금일(interval 배수)의 세금액. 표를 넘으면 지수적으로 확장.</summary>
    public static int GetTaxAmount(int taxStage)
    {
        EnsureLoaded();
        if (_interval <= 0 || taxStage < _interval) return 0;

        int idx = taxStage / _interval - 1; // 5→0, 10→1 …
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

        // 새벽 세금 배수 적용
        float dawnMul = DawnSystem.Current.taxMultiplier;
        return Mathf.RoundToInt(baseAmount * (dawnMul > 0f ? dawnMul : 1f));
    }
}
