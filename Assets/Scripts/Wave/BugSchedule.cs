using UnityEngine;

/// <summary>
/// 벌레 엔티티(기어다니는 벌레) 타이밍의 단일 기준 접근자. 해충 "웨이브"(날씨)와는 완전히 독립.
/// 설정은 <see cref="WaveScheduleConfig"/> 에셋의 Bug 섹션을 공유한다(없으면 코드 기본값).
/// 첫 등장 스폰, 변종 증가 주기, 단계 경고 메시지 시점이 모두 여기서 파생된다.
/// </summary>
public static class BugSchedule
{
    public const int DefaultAppearStage = 11;
    public const int DefaultVarietyStepStages = 5;
    public const int DefaultMessageLeadStages = 1;

    private static bool _loaded;
    private static int _appearStage = DefaultAppearStage;
    private static int _varietyStep = DefaultVarietyStepStages;
    private static int _messageLead = DefaultMessageLeadStages;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        var cfg = Resources.Load<WaveScheduleConfig>(WaveSchedule.ResourcePath);
        if (cfg != null)
        {
            _appearStage = cfg.bugAppearStage > 0 ? cfg.bugAppearStage : DefaultAppearStage;
            _varietyStep = cfg.bugVarietyStepStages > 0 ? cfg.bugVarietyStepStages : DefaultVarietyStepStages;
            _messageLead = Mathf.Max(0, cfg.bugMessageLeadStages);
        }
        // 에셋 없으면 코드 기본값 유지(경고는 WaveSchedule에서 이미 1회 출력)
    }

    public static void Reload()
    {
        _loaded = false;
        EnsureLoaded();
    }

    public static int AppearStage { get { EnsureLoaded(); return _appearStage; } }
    public static int VarietyStepStages { get { EnsureLoaded(); return _varietyStep; } }
    public static int MessageLeadStages { get { EnsureLoaded(); return _messageLead; } }

    /// <summary>해당 스테이지에서 스폰 가능한 벌레 종류 수. (AppearStage부터 1, step마다 +2)</summary>
    public static int GetVarietyCount(int stage)
    {
        EnsureLoaded();
        if (stage < _appearStage) return 0;
        int step = _varietyStep <= 0 ? DefaultVarietyStepStages : _varietyStep;
        return ((stage - _appearStage) / step) * 2 + 1;
    }

    /// <summary>
    /// 이 스테이지가 벌레 단계 경고 밤이면 트리거 id("Bug0","Bug1"...)를 반환, 아니면 null.
    /// 각 벌레 단계(AppearStage + tier*step)가 나타나기 messageLead 스테이지 전에 발송.
    /// </summary>
    public static string GetMessageTrigger(int stage)
    {
        EnsureLoaded();
        int step = _varietyStep <= 0 ? DefaultVarietyStepStages : _varietyStep;
        int firstWarn = _appearStage - _messageLead;
        int diff = stage - firstWarn;
        if (diff < 0 || diff % step != 0) return null;
        return "Bug" + (diff / step);
    }
}
