using System;
using System.Collections.Generic;
using UnityEditor;

public enum SimBotKind
{
    /// <summary>아무나 고른다. 봇 실력의 바닥 기준.</summary>
    Random,
    /// <summary>닥칠 웨이브의 저항력을 보고 고른다.</summary>
    Planner,
}

[Serializable]
public class SimConfig
{
    public int runs = 1000;
    public int firstSeed = 1;
    public int days = 40;
    public string species = PlantIds.Pea;
    public SimBotKind bot = SimBotKind.Planner;
    public int dawnStage = 0;
    public int startGold = 3000;       // 정원 씬 EconomyManager의 시작 골드
    public int breedAttempts = 4;      // BreedingController의 기본 교배 횟수
    public int seedlingPrice = 500;    // 창에서 돌릴 때 PeaItemData 에셋 가격으로 덮어쓴다
    public bool buySeedlings = true;
    public bool payTax = true;
}

public class SimRunResult
{
    public int Seed;
    public bool Cleared;
    /// <summary>전멸한 날. 클리어했으면 0.</summary>
    public int DeathDay;
    public bool DiedToTax;
    public WaveType DeathWave = WaveType.None;
    public int Gold;
    /// <summary>하루가 끝났을 때의 완두·땅콩 수와 골드 (0번이 1일차).</summary>
    public readonly List<int> Plants = new List<int>();
    public readonly List<int> GoldByDay = new List<int>();
    public readonly int[] WaveHits = new int[Wave.NumberOfWave];
    public readonly int[] WaveFaced = new int[Wave.NumberOfWave];
    /// <summary>판 전체의 요약값. 같은 시드로 두 번 돌려 같은지 비교한다.</summary>
    public ulong Digest = 14695981039346656037UL;

    public void Mix(int value) { unchecked { Digest = (Digest ^ (uint)value) * 1099511628211UL; } }
}

/// <summary>
/// 씬 없이 한 판을 돌린다. 교배, 웨이브, 죽음, 저항력, 판매가는 게임과 같은 코드를 쓴다.
///
/// 아직 없는 것: 모종 이외의 상점 아이템, 특수 아이템, 저주, 의뢰, 특성, 벌레.
/// 세금 징수(<see cref="CollectOverdueTax"/>)는 게임의 TaxCollectionRoutine을 따로 옮긴 것이라 한쪽을 고치면 함께 고쳐야 한다.
/// </summary>
public sealed class RunSimulator : IFieldActions
{
    private readonly SimConfig config;
    private readonly FieldState field;
    private readonly WaveDeck deck = new WaveDeck();
    private readonly SimRunResult result;
    private readonly RngStream botRng; // 봇의 무작위 선택. 게임 난수를 쓰지 않는다.
    private readonly PlantKind kind;

    private int stage;
    private int gold;
    private int totalGold;
    private int lastPaidTaxStage;
    private WaveType currentWave;
    private WaveType nextWave;

    private RunSimulator(SimConfig config, int seed)
    {
        this.config = config;
        field = FieldState.CreateEmpty();
        kind = config.species == PlantIds.Peanut ? PlantKind.Peanut : PlantKind.Pea;
        result = new SimRunResult { Seed = seed };
        botRng = new RngStream(unchecked((ulong)(uint)seed) ^ 0xB07B07B07UL);
        gold = config.startGold;
    }

    // ───── 여러 판 ─────

    /// <summary>여러 판을 돌린다. 전역 상태를 건드리므로 Play 중에는 돌리지 않는다. 중간에 취소하면 그때까지의 결과를 준다.</summary>
    public static List<SimRunResult> RunMany(SimConfig config, Func<int, bool> onProgress = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play 중에는 시뮬레이터를 돌릴 수 없습니다.");

        // 에디터에서 바꾼 밸런스 값이 바로 반영되도록 다시 읽는다
        WaveSchedule.Reload();
        TaxSchedule.Reload();
        DawnSystem.Reload();

        int savedDawn = DawnSystem.SelectedDawnStage;
        var results = new List<SimRunResult>(config.runs);
        try
        {
            CurseState.ResetAll();
            SpecialItemSystem.ResetRun();
            MutationDebug.Reset();
            DawnSystem.SetSelectedStage(config.dawnStage);

            for (int i = 0; i < config.runs; i++)
            {
                results.Add(RunOne(config, config.firstSeed + i));
                if (onProgress != null && i % 20 == 0 && !onProgress(i)) break;
            }
        }
        finally
        {
            DawnSystem.SetSelectedStage(savedDawn);
            GameRun.StartNew();
        }
        return results;
    }

    private static SimRunResult RunOne(SimConfig config, int seed)
    {
        GameRun.StartNew(seed);
        var sim = new RunSimulator(config, seed);
        sim.Play();
        return sim.result;
    }

    // ───── 한 판 ─────

    private void Play()
    {
        // GameManager.Start의 새 게임 순서: 웨이브 준비 → 시작 식물 2포기 → 1일차
        deck.Init(1);
        currentWave = WaveType.Aging;
        nextWave = deck.PickNext(currentWave, stage);
        for (int i = 0; i < 2; i++)
            Plant(new List<GeneticTrait> { new GeneticTrait(TraitType.NaturalDeath, Roll(TraitType.NaturalDeath, 1), 1, 0f) }, -1);
        NextStage();

        while (true)
        {
            CollectOverdueTax();
            if (IsWipedOut) { result.DiedToTax = true; result.DeathDay = stage; break; }

            BreedPhase();

            result.WaveFaced[(int)currentWave]++;
            WaveType faced = currentWave;
            DayRules.ResolveWave(field, currentWave, () => deck.PickSecond(currentWave, stage), this);
            if (IsWipedOut) { result.DeathWave = faced; result.DeathDay = stage; break; }
            AdvanceWave();

            DayRules.AfterWave(field, this);
            NightPhase();

            foreach (PlantState plant in field.Board.States.Values) plant.DaysOnField++;
            Record();

            if (stage == config.days) { result.Cleared = true; break; }
            NextStage();
        }

        result.Gold = gold;
        result.Mix(result.DeathDay);
        result.Mix(gold);
    }

    private bool IsWipedOut => field.Board.CountLivingPlants() == 0;

    private void NextStage()
    {
        stage++;
        deck.Unlock(stage);
    }

    private void AdvanceWave()
    {
        currentWave = nextWave;
        nextWave = deck.PickNext(currentWave, stage);
    }

    private void Record()
    {
        result.Plants.Add(field.Board.CountLivingPlants());
        result.GoldByDay.Add(gold);
        result.Mix(stage * 31 + (int)currentWave);
        result.Mix(gold);
        foreach (PlantState plant in SortedPlants())
        {
            result.Mix(plant.GridIndex * 1000 + plant.Taste);
            foreach (GeneticTrait t in plant.Traits)
                result.Mix((int)t.traitType * 100000 + t.genetics * 10000 + (int)Math.Round(t.resistance * 1000));
        }
    }

    // ───── 낮: 교배 ─────

    private void BreedPhase()
    {
        int used = 0;
        for (int guard = 0; guard < 50; guard++)
        {
            int max = BreedingRules.GetMaxAttempts(config.breedAttempts, 1f, totalGold);
            if (used >= max) break;
            if (!field.Board.HasEmptyCell() && !SellWorst(6)) break;

            List<PlantState> plants = SortedPlants();
            if (plants.Count < 2) break;
            (PlantState a, PlantState b) = PickParents(plants);
            if (BreedingRules.Check(field, a, b, used, max) != BreedCheck.Ok) break;

            BreedOutcome outcome = BreedingRules.Breed(field, a, b, config.species, WaveSchedule.GetSeasonByStage(stage),
                (traits, mutation, isTwin) => Plant(traits, -1));
            if (outcome.ConsumesAttempt) used++;
        }
    }

    private (PlantState, PlantState) PickParents(List<PlantState> plants)
    {
        if (config.bot == SimBotKind.Random)
        {
            int i = botRng.Range(0, plants.Count);
            int j = botRng.Range(0, plants.Count - 1);
            return (plants[i], plants[j >= i ? j + 1 : j]);
        }

        // 두 부모 중 나은 쪽의 저항력을 중시하되, 유전자가 좋은 쪽과 형질이 다양한 쪽을 높게 친다
        float[] weights = TraitWeights();
        float best = float.NegativeInfinity;
        (PlantState, PlantState) pick = (plants[0], plants[1]);
        for (int i = 0; i < plants.Count; i++)
        {
            for (int j = i + 1; j < plants.Count; j++)
            {
                float score = 0f;
                for (int t = 0; t < weights.Length; t++)
                {
                    if (weights[t] <= 0f) continue;
                    float ra = plants[i].GetResistanceValue(t), rb = plants[j].GetResistanceValue(t);
                    score += weights[t] * (Math.Max(ra, rb) + (ra + rb) * 0.25f + 0.1f * (Genetics(plants[i], t) + Genetics(plants[j], t)));
                }
                if (score > best) { best = score; pick = (plants[i], plants[j]); }
            }
        }
        return pick;
    }

    private static int Genetics(PlantState plant, int trait)
    {
        foreach (GeneticTrait t in plant.Traits)
            if ((int)t.traitType == trait) return t.genetics;
        return 0;
    }

    // 웨이브별 중요도. 오늘과 내일 웨이브를 크게, 상점에서 예고된 웨이브를 조금 본다.
    private float[] TraitWeights()
    {
        var weights = new float[Wave.NumberOfWave];
        for (int t = 0; t < weights.Length; t++)
            if (t == (int)WaveType.Aging || WaveSchedule.IsShopWaveUnlocked((WaveType)t, stage, true))
                weights[t] = 1f;
        weights[(int)nextWave] += 2f;
        weights[(int)currentWave] += 3f;
        return weights;
    }

    private float Score(PlantState plant, float[] weights)
    {
        float score = 0f;
        for (int t = 0; t < weights.Length; t++)
            score += weights[t] * plant.GetResistanceValue(t);
        return score;
    }

    // ───── 밤: 세금과 모종 ─────

    private int DueTaxStage => lastPaidTaxStage + TaxSchedule.Interval;

    private void NightPhase()
    {
        // 엔딩 날 세금은 걷지 않는다
        if (config.payTax && stage >= DueTaxStage && DueTaxStage != 40)
        {
            int due = TaxSchedule.GetTaxAmount(DueTaxStage);
            while (gold < due && SellWorst(4)) { }
            if (gold >= due)
            {
                gold -= due;
                lastPaidTaxStage = DueTaxStage;
            }
        }

        if (!config.buySeedlings) return;
        int price = SeedlingPrice;
        if (gold < price) return;
        if (!field.Board.HasEmptyCell() && !SellWorst(6)) return;

        List<TraitType[]> sets = TraitOptions.GetTraitSets(stage, isNight: true);
        TraitType[] set = PickSeedling(sets);
        if (set == null) return;

        gold -= price;
        var traits = new List<GeneticTrait>();
        foreach (TraitType t in set) traits.Add(new GeneticTrait(t, Roll(t, 1), 1, 0f));
        Plant(traits, -1);
    }

    private int SeedlingPrice
    {
        get
        {
            float mul = DawnSystem.Current.shopPriceMultiplier;
            return (int)Math.Round(config.seedlingPrice * (mul > 0f ? mul : 1f));
        }
    }

    // 가진 식물이 가장 적은 형질을 산다. 모든 형질을 3포기 이상이 갖고 있으면 사지 않는다.
    private TraitType[] PickSeedling(List<TraitType[]> sets)
    {
        if (config.bot == SimBotKind.Random) return sets[botRng.Range(0, sets.Count)];

        TraitType[] best = null;
        int fewest = 3;
        foreach (TraitType[] set in sets)
        {
            int owners = 0;
            foreach (PlantState plant in field.Board.States.Values)
                if (plant.HasTrait((WaveType)(int)set[0])) owners++;
            if (owners < fewest) { fewest = owners; best = set; }
        }
        return best;
    }

    /// <summary>가장 쓸모없는 식물을 판다. minKeep포기 이하로는 팔지 않는다.</summary>
    private bool SellWorst(int minKeep)
    {
        List<PlantState> plants = SortedPlants();
        if (plants.Count <= minKeep) return false;

        PlantState worst = null;
        if (config.bot == SimBotKind.Random)
            worst = plants[botRng.Range(0, plants.Count)];
        else
        {
            float[] weights = TraitWeights();
            float lowest = float.PositiveInfinity;
            foreach (PlantState plant in plants)
            {
                float score = Score(plant, weights);
                if (score < lowest) { lowest = score; worst = plant; }
            }
        }
        return Sell(worst);
    }

    private bool Sell(PlantState plant)
    {
        int price = plant.GetSellingPrice();
        DeathOutcome outcome = field.TryKill(plant, DeathCause.Shovel);
        if (!outcome.Died) return false;
        AddGold(price + outcome.BonusGold);
        return true;
    }

    // GameManager.TaxCollectionRoutine: 연체 세금을 강제로 걷고, 모자라면 비싼 식물부터 판다
    private void CollectOverdueTax()
    {
        if (!config.payTax || stage <= DueTaxStage) return;

        gold -= TaxSchedule.GetTaxAmount(DueTaxStage);
        lastPaidTaxStage = DueTaxStage;
        if (gold >= 0) return;

        List<PlantState> plants = SortedPlants();
        plants.Sort((a, b) =>
        {
            int c = b.GetSellingPrice().CompareTo(a.GetSellingPrice());
            return c != 0 ? c : a.GridIndex.CompareTo(b.GridIndex);
        });
        foreach (PlantState plant in plants)
        {
            if (gold >= 0) break;
            int price = plant.GetSellingPrice();
            AddGold(price);
            DeathOutcome outcome = field.TryKill(plant, DeathCause.Other);
            if (outcome.Died) AddGold(outcome.BonusGold);
        }
    }

    // ───── 공통 ─────

    // 칸 번호 순서의 완두·땅콩. 사전의 순서에 기대지 않는다.
    private List<PlantState> SortedPlants()
    {
        var list = new List<PlantState>();
        for (int idx = 0; idx < field.Board.CellCount; idx++)
            if (field.Board.States.TryGetValue(idx, out PlantState plant) && plant.IsMovableKind)
                list.Add(plant);
        return list;
    }

    private float Roll(TraitType trait, int genetics)
        => BreedingRules.RollResistance(config.species, field.Modifiers, trait, genetics);

    // Plant.SetTrait → Grid.AddPlantToGrid와 같은 순서
    private PlantState Plant(List<GeneticTrait> traits, int cell)
    {
        var plant = new PlantState(kind) { Traits = traits };
        RefreshTraits(plant);
        return field.AddPlant(plant, cell) ? plant : null;
    }

    // ───── IFieldActions ─────

    public void Survive(PlantState plant, WaveType wave) => plant.ResistWave(wave);

    public bool Kill(PlantState plant)
    {
        DeathOutcome outcome = field.TryKill(plant, DeathCause.Generic);
        if (outcome.Died) AddGold(outcome.BonusGold);
        return outcome.Died;
    }

    public void OnWaveHit(PlantState plant, WaveType wave, bool isSecondWave) => result.WaveHits[(int)wave]++;
    public void ResolveMushrooms() { }
    public void Unfreeze(PlantState plant) => plant.SetFrozen(false);
    public void AddGold(int amount) { gold += amount; totalGold += amount; }
    public void Root(PlantState plant) => plant.IsRooted = true;

    public void RefreshTraits(PlantState plant)
    {
        BreedingRules.CompletePairs(plant.Traits, Roll);
        plant.UpdateGoldFromTraits();
    }

    public PlantState SpawnCopy(List<GeneticTrait> traits, int cell, bool benignMutation) => Plant(traits, cell);
    public void AddBonusMultiplier(PlantState plant, int amount) => plant.BonusGoldMultiplierCount += amount;
}
