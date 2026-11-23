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
        public TraitType[] traits;
        public Option(params TraitType[] traits) { this.traits = traits; }

        public string Name => traits.Length == 1
            ? WavePalette.GetName(traits[0])
            : $"{WavePalette.GetName(traits[0])}/{WavePalette.GetName(traits[1])}";
    }

    private static Option Single(TraitType trait) => new Option(trait);

    private static Option Pair(TraitType a, TraitType b)
        => new Option(a, b);

    /// <summary>고를 수 있는 형질 묶음. (시뮬레이터용: 밤인지를 직접 넘긴다)</summary>
    public static List<TraitType[]> GetTraitSets(int currentStage, bool isNight)
        => Build(currentStage, isNight).ConvertAll(o => o.traits);

    private static List<Option> Build(int currentStage, bool isNight)
    {
        var list = new List<Option>();

        // 자연사는 항상 해금
        list.Add(Single(TraitType.NaturalDeath));

        if (IsUnlocked(TraitType.Pest, currentStage, isNight)) list.Add(Single(TraitType.Pest));
        if (IsUnlocked(TraitType.Wind, currentStage, isNight)) list.Add(Single(TraitType.Wind));
        if (IsUnlocked(TraitType.Flood, currentStage, isNight)) list.Add(Single(TraitType.Flood));

        // 쌍 형질: 둘 중 하나라도 해금되면 함께 제공 (기존 UI와 동일)
        if (IsUnlocked(TraitType.HeavyRain, currentStage, isNight) || IsUnlocked(TraitType.Drought, currentStage, isNight))
            list.Add(Pair(TraitType.HeavyRain, TraitType.Drought));

        if (IsUnlocked(TraitType.Cold, currentStage, isNight) || IsUnlocked(TraitType.Heat, currentStage, isNight))
            list.Add(Pair(TraitType.Cold, TraitType.Heat));

        return list;
    }

    /// <summary>드롭다운에 표시할 이름 목록.</summary>
    public static string[] GetNames(int currentStage, bool isNight)
    {
        var opts = Build(currentStage, isNight);
        var names = new string[opts.Count];
        for (int i = 0; i < opts.Count; i++) names[i] = opts[i].Name;
        return names;
    }

    /// <summary>선택 인덱스에 해당하는 형질 목록 생성. 범위를 벗어나면 기본(자연사).</summary>
    public static List<GeneticTrait> BuildTraits(int index, ShopContext ctx)
    {
        var opts = Build(ctx.Stage, ctx.World.IsNight);
        var picked = (index >= 0 && index < opts.Count) ? opts[index] : opts[0];

        var result = new List<GeneticTrait>();
        foreach (var t in picked.traits)
            result.Add(new GeneticTrait(t, ctx.World.RollResistance(t, 1), 1, 0.0f));
        return result;
    }

    private static bool IsUnlocked(TraitType trait, int currentStage, bool isNight)
    {
        if (trait == TraitType.NaturalDeath) return true;
        return WaveSchedule.IsShopWaveUnlocked(ToWaveType(trait), currentStage, isNight);
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
