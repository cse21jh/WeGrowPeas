using System;
using System.Collections.Generic;

/// <summary>
/// 하루 규칙이 밭에 일으키는 변화. 게임은 화면 처리까지(<see cref="GridFieldActions"/>),
/// 시뮬레이터는 상태만 바꾼다. 무엇을 언제 일으킬지는 <see cref="DayRules"/>가 정한다.
/// </summary>
public interface IFieldActions
{
    /// <summary>웨이브를 버틴 식물의 뒤처리.</summary>
    void Survive(PlantState plant, WaveType wave);
    /// <summary>웨이브에 죽는다. 무언가가 막았으면 false.</summary>
    bool Kill(PlantState plant);
    /// <summary>버티지 못한 식물이 나왔다(죽음이 막히기 전). 집계용.</summary>
    void OnWaveHit(PlantState plant, WaveType wave, bool isSecondWave);
    /// <summary>저주(버섯)가 난 칸.</summary>
    IReadOnlyList<int> MushroomTiles { get; }
    void Unfreeze(PlantState plant);
    void AddGold(int amount);
    void Root(PlantState plant);
    /// <summary>형질 값을 직접 고친 뒤 황금 여부와 표시를 다시 맞춘다.</summary>
    void RefreshTraits(PlantState plant);
    PlantState SpawnCopy(List<GeneticTrait> traits, int cell, bool benignMutation);
    void AddBonusMultiplier(PlantState plant, int amount);
}

/// <summary>하루의 판정 순서. 게임의 코루틴은 사이사이에 연출과 입력을 기다릴 뿐 같은 함수를 부른다.</summary>
public static class DayRules
{
    private const float PeanutCopyProbability = 0.25f;

    /// <summary>웨이브 판정 전체. 흡수 비료 → 웨이브 → (저주) 두 번째 웨이브 → 버섯 → 해동.</summary>
    /// <param name="pickSecondWave">저주(이중 웨이브)일 때 두 번째 웨이브를 정한다. 첫 판정이 끝난 뒤 불린다.</param>
    public static void ResolveWave(FieldState field, WaveType wave, Func<WaveType?> pickSecondWave, IFieldActions actions)
    {
        field.Soil.ProcessResistanceAbsorption();

        if (wave != WaveType.None)
        {
            for (int idx = 0; idx < field.Board.CellCount; idx++)
            {
                if (!field.Board.States.TryGetValue(idx, out PlantState plant)) continue;

                if (plant.CanResist(wave))
                {
                    // 특수(도박꾼): 저항력 40% 이하인 웨이브를 스스로 버티면 가격의 20%
                    if (SpecialItemSystem.Has("gambler") && !plant.IsFrozen && plant.GetResistanceValue((int)wave) <= 0.4f)
                        actions.AddGold((int)Math.Round(plant.GetSellingPrice() * 0.2f));

                    actions.Survive(plant, wave);
                    continue;
                }

                if (field.IsTutorial && field.Board.CountLivingPlants() <= 1) continue;

                actions.OnWaveHit(plant, wave, false);
                if (!actions.Kill(plant))
                    actions.Survive(plant, wave);
            }

            // 저주(이중 웨이브): 저항력 감소가 두 번 일어나지 않게 죽이는 판정만 한다
            if (CurseState.DoubleWave)
            {
                WaveType? second = pickSecondWave();
                if (second.HasValue && second.Value != WaveType.None)
                {
                    for (int idx = 0; idx < field.Board.CellCount; idx++)
                    {
                        if (!field.Board.States.TryGetValue(idx, out PlantState plant)) continue;
                        if (plant.CanResist(second.Value)) continue;
                        actions.OnWaveHit(plant, second.Value, true);
                        actions.Kill(plant);
                    }
                }
            }
        }

        foreach (int cell in actions.MushroomTiles)
            if (field.Board.States.TryGetValue(cell, out PlantState plant)) actions.Kill(plant); // 페트병이 막을 수 있다

        foreach (PlantState plant in field.Board.States.Values)
            if (plant.IsFrozen) actions.Unfreeze(plant);
    }

    /// <summary>낮(교배 시간)이 끝날 때. 완두커피를 갖고 있으면 자유시간 횟수가 쌓인다.</summary>
    public static void EndFreeTime(FieldState field, Action<PlantState> countFreeTime)
    {
        if (field.Modifiers.PeaCoffeeMultiplier <= 0f) return;
        foreach (PlantState plant in field.Board.StatesInOrder()) countFreeTime(plant);
    }

    /// <summary>웨이브가 지나간 뒤 밤이 되기 전. 땅콩 자가번식 → 뿌리내림 → 무당벌레 골드 → 고추 치료.</summary>
    public static void AfterWave(FieldState field, IFieldActions actions)
    {
        GridModifiers modifiers = field.Modifiers;

        // 이번에 새로 생긴 땅콩이 다시 번식하지 않게 먼저 모은다
        var peanuts = new List<PlantState>();
        for (int idx = 0; idx < field.Board.CellCount; idx++)
            if (field.Board.States.TryGetValue(idx, out PlantState plant) && plant.Kind == PlantKind.Peanut)
                peanuts.Add(plant);
        foreach (PlantState peanut in peanuts)
            TryCopyPeanut(field, peanut, actions);

        // 땅과 콩: 아직 뿌리내리지 않은 식물이 확률적으로 뿌리를 내린다
        float rootChance = modifiers.GetLandAndBeanRootChance();
        if (rootChance > 0f)
        {
            foreach (PlantState plant in field.Board.StatesInOrder())
                if (plant.IsMovable && !plant.IsDying && GameRun.Rng.Field.Value < rootChance)
                    actions.Root(plant);
        }

        if (modifiers.AdditionalLadybugGoldPerUnit > 0)
        {
            int gold = field.Board.Ladybugs.Count * modifiers.AdditionalLadybugGoldPerUnit;
            if (gold > 0) actions.AddGold(gold);
        }

        // 치료형 캡사이신: 고추 주변 식물의 모든 저항력 회복
        if (modifiers.ChiliPepperHealPercent > 0f)
        {
            foreach (PlantState chili in field.Board.StatesInOrder())
            {
                if (chili.Kind != PlantKind.ChiliPepper) continue;
                foreach (PlantState plant in field.Soil.GetPlantsNearChiliPepper(chili.GridIndex))
                {
                    List<GeneticTrait> traits = plant.Traits;
                    for (int i = 0; i < traits.Count; i++)
                    {
                        float healed = Math.Min(1f, Math.Max(0.1f, traits[i].resistance + modifiers.ChiliPepperHealPercent));
                        traits[i] = new GeneticTrait(traits[i].traitType, healed, traits[i].genetics, traits[i].additionalResistance);
                    }
                    actions.RefreshTraits(plant);
                }
            }
        }
    }

    private static void TryCopyPeanut(FieldState field, PlantState peanut, IFieldActions actions)
    {
        float probability = PeanutCopyProbability + field.Modifiers.AdditionalPeanutCopyProbability;
        // 활성형 껍질: 한 번도 교배를 시도하지 않은 식물은 확률 증가
        if (!peanut.HasTriedBreed) probability += field.Modifiers.ActiveShellProbability;

        if (GameRun.Rng.Field.Range(0f, 100f) >= 100f * probability) return;

        int cell = PickCopyCell(field, peanut.GridIndex);
        if (cell == -1) return;

        var traits = new List<GeneticTrait>(peanut.Traits);

        // 특수(임시땅콩B): 자가번식 때 양성 변종만 나온다
        bool benign = SpecialItemSystem.Has("peanut_special_8")
            && GameRun.Rng.Field.Range(0f, 100f) < BreedingRules.GetMutationChancePercent(field.Modifiers);
        if (benign) BreedingRules.ApplyBenignResistance(traits);

        PlantState child = actions.SpawnCopy(traits, cell, benign);

        // 왕위 계승: 부모 가격 배율의 일부를 물려준다
        float inheritRatio = field.Modifiers.SuccessionInheritRatio;
        if (child != null && inheritRatio > 0f)
        {
            int inherited = (int)Math.Floor((peanut.ResistWaveCount + peanut.BonusGoldMultiplierCount) * inheritRatio);
            if (inherited > 0) actions.AddBonusMultiplier(child, inherited);
        }
    }

    // 위, 아래, 왼쪽, 오른쪽의 빈 칸 중 하나. 조건식과 순서는 기존 동작을 그대로 옮긴 것이다.
    // (0번 칸은 -1번 칸도 후보에 들어가고, 그것이 뽑히면 번식하지 않는다)
    private static int PickCopyCell(FieldState field, int idx)
    {
        var empty = new List<int>();
        if ((idx - 1) / 4 == idx / 4 && !field.Board.States.ContainsKey(idx - 1)) empty.Add(idx - 1);
        if ((idx + 1) / 4 == idx / 4 && !field.Board.States.ContainsKey(idx + 1)) empty.Add(idx + 1);
        if (idx - 4 >= 0 && !field.Board.States.ContainsKey(idx - 4)) empty.Add(idx - 4);
        if (idx + 4 < field.Board.CellCount && !field.Board.States.ContainsKey(idx + 4)) empty.Add(idx + 4);
        return empty.Count == 0 ? -1 : empty[GameRun.Rng.Field.Range(0, empty.Count)];
    }
}
