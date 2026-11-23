using System;
using System.Collections.Generic;

public enum BreedCheck { Ok, NoAttemptsLeft, SpeciesMismatch, NoSpace }

public enum MutationKind { None, Malignant, Benign }

/// <summary>교배 한 번의 결과.</summary>
public struct BreedOutcome
{
    /// <summary>저주(씨 없는 수박)로 실패했는가. 실패해도 시도 횟수는 쓴다.</summary>
    public bool Failed;
    public bool HasTwin;
    public bool ConsumesAttempt;
}

/// <summary>교배와 저항력 추첨 규칙. 게임과 시뮬레이터가 함께 쓴다. 난수를 뽑는 순서를 바꾸지 않는다.</summary>
public static class BreedingRules
{
    /// <summary>
    /// 교배로 생긴 자식을 실제로 심는 일. 게임은 프리팹을 만들고, 시뮬레이터는 상태만 심는다.
    /// </summary>
    public delegate void SpawnChild(List<GeneticTrait> traits, MutationKind mutation, bool isTwin);

    // ───── 교배 시도 ─────

    /// <summary>하루에 교배할 수 있는 횟수.</summary>
    public static int GetMaxAttempts(int baseCount, float attemptsMul, int totalGold)
        => Math.Max(1, (int)Math.Floor(baseCount * attemptsMul) + GetKingReturnDelta(totalGold));

    /// <summary>특수(왕의 귀환): 기본 교배 횟수 -2, 누적 획득 골드 5000당 +1.</summary>
    private static int GetKingReturnDelta(int totalGold)
        => SpecialItemSystem.Has("king_return") ? -2 + totalGold / 5000 : 0;

    public static BreedCheck Check(FieldState field, PlantState a, PlantState b, int usedAttempts, int maxAttempts)
    {
        bool sameKind = a.Kind == b.Kind;
        if (field.Board.HasEmptyCell() && usedAttempts < maxAttempts && sameKind) return BreedCheck.Ok;
        if (usedAttempts >= maxAttempts) return BreedCheck.NoAttemptsLeft;
        return sameKind ? BreedCheck.NoSpace : BreedCheck.SpeciesMismatch;
    }

    /// <summary><see cref="Check"/>를 통과한 두 식물을 교배한다.</summary>
    public static BreedOutcome Breed(FieldState field, PlantState a, PlantState b, string species, Season season, SpawnChild spawn)
    {
        // 활성형 껍질: 교배를 시도한 식물은 이후 자가번식 확률 보너스를 받지 못한다
        a.HasTriedBreed = true;
        b.HasTriedBreed = true;

        // 저주(씨 없는 수박): 확률적으로 실패
        if (CurseState.SeedlessFailPercent > 0f && GameRun.Rng.Breed.Range(0f, 100f) < CurseState.SeedlessFailPercent)
            return new BreedOutcome { Failed = true, ConsumesAttempt = true };

        // 저주(광란): 확률적으로 랜덤 교배
        bool madness = CurseState.BreedMadnessPercent > 0f && GameRun.Rng.Breed.Range(0f, 100f) < CurseState.BreedMadnessPercent;

        List<GeneticTrait> child = CreateChild(a.Traits, b.Traits, field.Modifiers, species, season, madness, out MutationKind mutation);
        spawn(child, mutation, false);

        // 쌍둥이: 확률적으로 한 번 더 교배해 유전자가 다른 자식을 하나 더 낳는다
        bool twin = field.Modifiers.TwinBreedProbability > 0f
            && GameRun.Rng.Breed.Range(0f, 1f) < field.Modifiers.TwinBreedProbability
            && field.Board.HasEmptyCell();
        if (twin)
        {
            List<GeneticTrait> second = CreateChild(a.Traits, b.Traits, field.Modifiers, species, season, madness, out MutationKind secondMutation);
            spawn(second, secondMutation, true);
        }

        // 특수(밑장빼기): 25% 확률로 교배 횟수를 쓰지 않는다
        bool free = SpecialItemSystem.Has("bottom_deal") && GameRun.Rng.Breed.Range(0f, 100f) < 25f;
        return new BreedOutcome { HasTwin = twin, ConsumesAttempt = !free };
    }

    // ───── 자식 형질 ─────

    /// <summary>
    /// 두 부모의 형질로 자식 형질을 정한다. 가뭄과 더위는 여기서 정하지 않고
    /// <see cref="CompletePairs"/>가 폭우와 추위의 짝으로 채운다.
    /// </summary>
    public static List<GeneticTrait> CreateChild(List<GeneticTrait> parent1, List<GeneticTrait> parent2,
        GridModifiers modifiers, string species, Season season, bool randomGenes, out MutationKind mutation)
    {
        var child = new List<GeneticTrait>();

        // 변종 판정(식물 단위). 악성은 유전자 무작위, 양성은 부모 유전자 + 모든 저항력 90~100%
        mutation = MutationKind.None;
        if (GameRun.Rng.Breed.Range(0f, 100f) < GetMutationChancePercent(modifiers))
        {
            // 악성 비율: 기본 0.8, 슈퍼 변종이면 0.2
            float malignantRatio = modifiers.HasSuperMutation ? 0.2f : 0.8f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MutationDebug.HasRatioOverride) malignantRatio = MutationDebug.MalignantRatioOverride;
#endif
            if (SpecialItemSystem.Has("extreme_mutation")) mutation = MutationKind.Malignant; // 특수(극단적 변이): 교배 시 악성만
            else if (GameRun.Rng.Breed.Value < malignantRatio) mutation = MutationKind.Malignant;
            else mutation = MutationKind.Benign;
        }

        for (TraitType trait = TraitType.NaturalDeath; trait < TraitType.Drought; trait++)
        {
            bool has1 = TryFind(parent1, trait, out GeneticTrait t1);
            bool has2 = TryFind(parent2, trait, out GeneticTrait t2);
            if (!has1 && !has2) continue; // 둘 다 없는 형질은 자식에게도 없다

            int genetics = InheritOne(has1 ? t1.genetics : 0, modifiers) + InheritOne(has2 ? t2.genetics : 0, modifiers);

            if (randomGenes || mutation == MutationKind.Malignant)
                genetics = GameRun.Rng.Breed.Range(0, 3);

            // 특수(적응형 유전자): 양성 변종의 유전자를 현재 계절에 유리한 쪽으로
            if (mutation == MutationKind.Benign && SpecialItemSystem.Has("adaptive_genetics"))
                genetics = GetSeasonFavoredGenetics(trait, season);

            float resistance = RollResistance(species, modifiers, trait, genetics, has1 ? t1.resistance : 0f, has2 ? t2.resistance : 0f);

            if (mutation == MutationKind.Benign)
                resistance = GameRun.Rng.Breed.Range(90, 101) / 100f;

            child.Add(new GeneticTrait(trait, resistance, genetics,
                trait == TraitType.Pest ? modifiers.AdditionalPestResistance : 0f));
        }
        return child;
    }

    // 부모 한쪽이 물려주는 유전자 수(0 또는 1)
    private static int InheritOne(int parentGenetics, GridModifiers modifiers)
    {
        if (parentGenetics == 2) return 1;
        if (parentGenetics != 1) return 0;
        int bonus = (int)(modifiers.GoldenGeneticsProbabilityBonus * 100);
        return GameRun.Rng.Breed.Range(1, 101) <= modifiers.AdditionalInheritance + 50 + bonus ? 1 : 0;
    }

    private static bool TryFind(List<GeneticTrait> traits, TraitType type, out GeneticTrait found)
    {
        foreach (var t in traits)
        {
            if (t.traitType != type) continue;
            found = t;
            return true;
        }
        found = default;
        return false;
    }

    // 앞쪽 4개(자연사, 해충, 바람, 홍수)는 2. 폭우와 추위는 가을·겨울이면 2, 봄·여름이면 0(짝 형질이 강해진다).
    private static int GetSeasonFavoredGenetics(TraitType trait, Season season)
    {
        if (trait != TraitType.HeavyRain && trait != TraitType.Cold) return 2;
        return season == Season.Winter || season == Season.Fall ? 2 : 0;
    }

    /// <summary>폭우와 가뭄, 추위와 더위는 항상 짝으로 갖는다. 한쪽만 있으면 유전자가 반대인 짝을 채운다.</summary>
    public static void CompletePairs(List<GeneticTrait> traits, Func<TraitType, int, float> rollResistance)
    {
        CompletePair(traits, TraitType.HeavyRain, TraitType.Drought, rollResistance);
        CompletePair(traits, TraitType.Cold, TraitType.Heat, rollResistance);
    }

    private static void CompletePair(List<GeneticTrait> traits, TraitType a, TraitType b, Func<TraitType, int, float> rollResistance)
    {
        bool hasA = TryFind(traits, a, out GeneticTrait traitA);
        bool hasB = TryFind(traits, b, out GeneticTrait traitB);
        if (hasA == hasB) return;

        TraitType missing = hasA ? b : a;
        int genetics = 2 - (hasA ? traitA : traitB).genetics;
        traits.Add(new GeneticTrait(missing, rollResistance(missing, genetics), genetics, 0f));
    }

    // ───── 저항력 추첨과 변종 ─────

    /// <summary>유전자에 따른 기본 저항력을 굴린다. 부모 저항력이 높으면 최솟값이 올라간다.</summary>
    public static float RollResistance(string species, GridModifiers modifiers, TraitType trait, int genetics,
        float parent1Resistance = 0f, float parent2Resistance = 0f)
    {
        float min = (parent1Resistance + parent2Resistance) * 0.45f;
        float max = 0.9f;
        bool seasonal = trait >= TraitType.HeavyRain;

        if (species == PlantIds.Pea)
        {
            if (seasonal) ApplyRange(genetics, ref min, ref max, 0.3f, 0.5f, 0.5f, 0.7f, 0.7f, 0.9f);
            else ApplyRange(genetics, ref min, ref max, 0.3f, 0.5f, 0.3f, 0.5f, 0.5f, 0.9f);
        }
        else if (species == PlantIds.Peanut)
        {
            if (seasonal) ApplyRange(genetics, ref min, ref max, 0.2f, 0.4f, 0.4f, 0.6f, 0.6f, 0.8f);
            else ApplyRange(genetics, ref min, ref max, 0.3f, 0.4f, 0.3f, 0.4f, 0.4f, 0.8f);
        }

        if (min > max) min = max;

        float resistance = (float)Math.Round(GameRun.Rng.Breed.Range(min, max) * 100f) / 100f; // 소수점 둘째 자리 반올림
        resistance += modifiers.ResistanceBonus;
        resistance += genetics == 2 ? modifiers.StrongGeneticsResistanceBonus : modifiers.WeakGeneticsResistanceBonus;

        // 특수(이중 시도): 최초 저항력 35% 감소 (대신 생존 시도 2회)
        if (SpecialItemSystem.Has("double_try"))
            resistance *= 0.65f;

        return Math.Min(resistance, 1f);
    }

    private static void ApplyRange(int genetics, ref float min, ref float max,
        float min0, float max0, float min1, float max1, float min2, float max2)
    {
        switch (genetics)
        {
            case 0: min = Math.Max(min0, min); max = max0; break;
            case 1: min = Math.Max(min1, min); max = max1; break;
            case 2: min = Math.Max(min2, min); max = max2; break;
        }
    }

    /// <summary>변종 발생 확률(%) = 기본 1% + 새벽 + 저주(돌연변이) + 슈퍼 변종.</summary>
    public static float GetMutationChancePercent(GridModifiers modifiers)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (MutationDebug.HasChanceOverride) return MutationDebug.ChanceOverride; // 디버그 패널(F11)
#endif
        return 1f + DawnSystem.Current.mutationChanceAddPercent + CurseState.MutationAddPercent
            + (modifiers != null ? modifiers.SuperMutationChanceBonus : 0f);
    }

    /// <summary>양성 변종: 모든 형질의 저항력을 90~100%로 (유전자는 유지).</summary>
    public static void ApplyBenignResistance(List<GeneticTrait> traits)
    {
        for (int i = 0; i < traits.Count; i++)
            traits[i] = new GeneticTrait(traits[i].traitType, GameRun.Rng.Breed.Range(90, 101) / 100f,
                traits[i].genetics, traits[i].additionalResistance);
    }
}
