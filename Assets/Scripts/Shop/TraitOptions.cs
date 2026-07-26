using System.Collections.Generic;

/// <summary>
/// 식물 모종의 형질 선택지. (상세 패널 드롭다운용)
/// 기존 형질 선택 UI와 같은 규칙을 쓴다:
/// - 해금 기준은 웨이브 첫 등장(WaveSchedule), 자연사는 항상 해금
/// - 추위/더위, 폭우/가뭄은 쌍으로 함께 부여
/// - 모든 형질은 genetics = 1
/// </summary>
public static class TraitOptions
{
    /// <summary>드롭다운 한 항목이 부여할 형질 묶음.</summary>
    private struct Option
    {
        public string name;
        public TraitType[] traits;
        public Option(string name, params TraitType[] traits) { this.name = name; this.traits = traits; }
    }

    private static List<Option> Build(int currentStage)
    {
        var list = new List<Option>();

        // 자연사는 항상 해금
        list.Add(new Option("자연사", TraitType.NaturalDeath));

        if (IsUnlocked(TraitType.Pest, currentStage)) list.Add(new Option("해충", TraitType.Pest));
        if (IsUnlocked(TraitType.Wind, currentStage)) list.Add(new Option("바람", TraitType.Wind));
        if (IsUnlocked(TraitType.Flood, currentStage)) list.Add(new Option("홍수", TraitType.Flood));

        // 쌍 형질: 둘 중 하나라도 해금되면 함께 제공 (기존 UI와 동일)
        if (IsUnlocked(TraitType.HeavyRain, currentStage) || IsUnlocked(TraitType.Drought, currentStage))
            list.Add(new Option("폭우/가뭄", TraitType.HeavyRain, TraitType.Drought));

        if (IsUnlocked(TraitType.Cold, currentStage) || IsUnlocked(TraitType.Heat, currentStage))
            list.Add(new Option("추위/더위", TraitType.Cold, TraitType.Heat));

        return list;
    }

    /// <summary>드롭다운에 표시할 이름 목록.</summary>
    public static string[] GetNames(int currentStage)
    {
        var opts = Build(currentStage);
        var names = new string[opts.Count];
        for (int i = 0; i < opts.Count; i++) names[i] = opts[i].name;
        return names;
    }

    /// <summary>선택 인덱스에 해당하는 형질 목록 생성. 범위를 벗어나면 기본(자연사).</summary>
    public static List<GeneticTrait> BuildTraits(int index, int currentStage)
    {
        var opts = Build(currentStage);
        var picked = (index >= 0 && index < opts.Count) ? opts[index] : opts[0];

        var result = new List<GeneticTrait>();
        foreach (var t in picked.traits)
            result.Add(new GeneticTrait(t, Plant.GetResistanceBasedOnGenetics(t, 1), 1, 0.0f));
        return result;
    }

    private static bool IsUnlocked(TraitType trait, int currentStage)
    {
        if (trait == TraitType.NaturalDeath) return true;
        return WaveSchedule.IsShopWaveUnlocked(ToWaveType(trait), currentStage);
    }

    private static WaveType ToWaveType(TraitType traitType)
    {
        switch (traitType)
        {
            case TraitType.Pest: return WaveType.Pest;
            case TraitType.Wind: return WaveType.Wind;
            case TraitType.Flood: return WaveType.Flood;
            case TraitType.HeavyRain: return WaveType.HeavyRain;
            case TraitType.Drought: return WaveType.Drought;
            case TraitType.Cold: return WaveType.Cold;
            case TraitType.Heat: return WaveType.Heat;
            default: return WaveType.None;
        }
    }
}
