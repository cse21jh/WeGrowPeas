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
    /// <summary>모종 말고 다른 상점 아이템도 산다.</summary>
    public bool buyItems = true;
    /// <summary>새벽 클리어나 사건으로 해금되는 아이템도 진열한다. 끄면 처음부터 열려 있는 것만.</summary>
    public bool allUnlocked = true;
    public int rotationCount = 6;      // 정원 씬 ShopManager의 로테이션 칸 수
    public bool buySeedlings = true;
    public bool curses = true;         // 새벽 저주 레벨이 있을 때만 걸린다
    public bool requests = true;
    public bool specialItems = true;
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
    /// <summary>산 아이템 이름과 처음 산 날.</summary>
    public readonly Dictionary<string, int> FirstBought = new Dictionary<string, int>();
    public int Purchases;
    public int RequestsCompleted;
    public int CursesApplied;
    public readonly List<string> SpecialItems = new List<string>();
    public readonly int[] WaveFaced = new int[Wave.NumberOfWave];
    /// <summary>판 전체의 요약값. 같은 시드로 두 번 돌려 같은지 비교한다.</summary>
    public ulong Digest = 14695981039346656037UL;

    public void Mix(int value) { unchecked { Digest = (Digest ^ (uint)value) * 1099511628211UL; } }
}

/// <summary>
/// 씬 없이 한 판을 돌린다. 교배, 웨이브, 죽음, 저항력, 판매가는 게임과 같은 코드를 쓴다.
///
/// 아직 없는 것: 특성, 벌레, 교배 시간 제한(시간은 금이다), 리롤.
/// 경제, 세금, 기간제 보정도 게임과 같은 상태 클래스를 쓴다. 강제 매각의 10초 유예만 없다(봇은 기다리지 않는다).
/// </summary>
public sealed partial class RunSimulator : IFieldActions
{
    private readonly SimConfig config;
    private readonly FieldState field;
    private readonly WaveDeck deck = new WaveDeck();
    private readonly SimRunResult result;
    private readonly RngStream botRng; // 봇의 무작위 선택. 게임 난수를 쓰지 않는다.
    private readonly PlantKind kind;

    private int stage;
    private readonly EconomyState economy;
    private readonly TaxState tax = new TaxState();
    private readonly ModState mods = new ModState();
    private WaveType currentWave;
    private WaveType nextWave;
    private WaveType? nextSecondWave;

    private RunSimulator(SimConfig config, int seed)
    {
        this.config = config;
        field = FieldState.CreateEmpty();
        kind = config.species == PlantIds.Peanut ? PlantKind.Peanut : PlantKind.Pea;
        result = new SimRunResult { Seed = seed };
        botRng = new RngStream(unchecked((ulong)(uint)seed) ^ 0xB07B07B07UL);
        shopContext = new ShopContext(this);
        requests = new RequestBoard(requestPool, 5, 3, this);
        curses = new CurseBoard(temporalCurses, seasonalCurses, this);
        economy = new EconomyState(config.startGold);
        field.Board.Changed += KillLonelyMoneyTrees;
        deck.WeightMultiplier = type => mods.GetMul(StatId.WaveWeightMul, (int)type, stage);
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
        LoadItems();
        var results = new List<SimRunResult>(config.runs);
        try
        {
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
            ResetItems();
            CurseState.ResetAll();
            SpecialItemSystem.ResetRun();
            GameRun.StartNew();
        }
        return results;
    }

    private static SimRunResult RunOne(SimConfig config, int seed)
    {
        GameRun.StartNew(seed);
        ResetItems();
        CurseState.ResetAll();
        SpecialItemSystem.ResetRun();
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
        _ = shopContext.Shop.UniqueSeed; // 새 게임에서 정해지는 순서와 맞춘다
        NextStage();

        while (true)
        {
            // GameManager.StartNormalMode → StartStage 순서
            if (CursesActive) curses.Apply();
            if (CursesActive && curses.Temporal != null) result.CursesApplied++;
            if (IsWipedOut) { result.DeathDay = stage; break; }

            CollectOverdueTax();
            if (IsWipedOut) { result.DiedToTax = true; result.DeathDay = stage; break; }

            if (config.requests && stage % 5 == 2)
                requests.StartCycle(stage, unchecked(shopContext.Shop.UniqueSeed + stage * 100));

            economy.Add(shopContext.Shop.ConsumeDailyPurchaseRefund(field.Modifiers.CreditCardRefundPercent));
            foreach (PlantState plant in field.Board.States.Values) plant.DayStartGridIndex = plant.GridIndex;
            BreedPhase();
            MovePlantsForRequest();
            DayRules.EndFreeTime(field, plant => plant.FreeTimePassedCount++);
            UseWaveSkip();

            if (currentWave != WaveType.None) result.WaveFaced[(int)currentWave]++;
            WaveType faced = currentWave;
            DayRules.ResolveWave(field, currentWave, () => nextSecondWave ?? deck.PickSecond(currentWave, stage), this);
            if (IsWipedOut) { result.DeathWave = faced; result.DeathDay = stage; break; }
            AdvanceWave();

            DayRules.AfterWave(field, this);
            curses.OnDayPassed();
            if (CursesActive) curses.Select(stage);
            isNight = true;
            NightPhase();
            isNight = false;
            requests.DayPassed++;
            events.RaiseQuestDayPassed();
            ClaimRequests();

            foreach (PlantState plant in field.Board.States.Values) plant.DaysOnField++;
            Record();

            if (stage == config.days) { result.Cleared = true; break; }
            NextStage();
        }

        result.Gold = economy.Gold;
        result.Mix(result.DeathDay);
        result.Mix(economy.Gold);
    }

    private bool IsWipedOut => field.Board.CountLivingPlants() == 0;

    private void NextStage()
    {
        economy.TakeEarnedToday();
        stage++;
        GameRun.Current.Stage = stage;
        mods.ExpireOn(stage);
        deck.Unlock(stage);
        shopContext.Shop.StartDay();
    }

    private void AdvanceWave()
    {
        currentWave = nextWave;
        nextWave = deck.PickNext(currentWave, stage);
        nextSecondWave = deck.PickNextSecond(nextWave, stage);
    }

    private void Record()
    {
        result.Plants.Add(field.Board.CountLivingPlants());
        result.GoldByDay.Add(economy.Gold);
        result.Mix(stage * 31 + (int)currentWave);
        result.Mix(economy.Gold);
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
            int max = BreedingRules.GetMaxAttempts(config.breedAttempts + bonusBreedAttempts,
                mods.GetMul(StatId.BreedingAttemptsMul, -1, stage), economy.TotalGold);
            if (used >= max) break;
            if (!field.Board.HasEmptyCell() && !SellWorst(6)) break;

            List<PlantState> plants = SortedPlants().FindAll(p => p.Breedable); // 저주(꽃가루 실종)
            if (plants.Count < 2) break;
            (PlantState a, PlantState b) = PickParents(plants);
            if (BreedingRules.Check(field, a, b, used, max) != BreedCheck.Ok) break;

            BreedOutcome outcome = BreedingRules.Breed(field, a, b, config.species, WaveSchedule.GetSeasonByStage(stage),
                (traits, mutation, isTwin) => Plant(traits, -1));
            if (outcome.ConsumesAttempt) used++;
            if (!outcome.Failed) events.RaiseBreeded();
        }

        int limit = BreedingRules.GetMaxAttempts(config.breedAttempts + bonusBreedAttempts,
            mods.GetMul(StatId.BreedingAttemptsMul, -1, stage), economy.TotalGold);
        events.RaiseDayEndedWithRemainingBreeds(Math.Max(0, limit - used));
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
                    score += weights[t] * (Math.Max(ra, rb) + (ra + rb) * 0.25f + GeneticsWeight * (Genetics(plants[i], t) + Genetics(plants[j], t)));
                }
                if (score > best) { best = score; pick = (plants[i], plants[j]); }
            }
        }
        return pick;
    }

    // 교배 짝과 남길 식물을 고를 때 유전자(0~2) 하나를 저항력 얼마로 치는가
    private const float GeneticsWeight = 0.3f;

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
        if (CurseState.WaveBlind) return weights; // 저주(기상이변): 웨이브를 볼 수 없다
        weights[(int)nextWave] += 2f;
        weights[(int)currentWave] += 3f;
        return weights;
    }

    private float Score(PlantState plant, float[] weights)
    {
        float score = 0f;
        for (int t = 0; t < weights.Length; t++)
            score += weights[t] * (plant.GetResistanceValue(t) + GeneticsWeight * Genetics(plant, t));
        return score;
    }

    // ───── 밤: 세금과 모종 ─────

    private void NightPhase()
    {
        if (config.payTax && tax.IsDue(stage))
        {
            while (!tax.CanPay(economy, out _) && economy.Gold < tax.DueAmount && SellWorst(4)) { }
            if (tax.CanPay(economy, out int due))
            {
                economy.Spend(due);
                tax.MarkPaid();
            }
        }
        if (stage == EndlessMode.StartStage) tax.SettleAtEnding();

        TakeSpecialItem();
        ClaimRequests();
        ShopPhase();
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
        // Shovel과 같은 순서: 사망 보너스 → 판매 집계 → 판매가 → 특수(순환)
        economy.Add(outcome.BonusGold);
        economy.CountSale(plant.Kind);
        economy.Add(price);
        field.Soil.HealNeighborsBySell(plant.GridIndex);
        events.RaisePlantSold(plant);
        return true;
    }

    // GameManager.TaxCollectionRoutine: 연체 세금을 강제로 걷고, 모자라면 비싼 식물부터 판다
    private void CollectOverdueTax()
    {
        if (!config.payTax || !tax.IsOverdue(stage)) return;

        int owed = tax.DueAmount;
        tax.MarkPaid();
        economy.Spend(owed);
        if (economy.Gold >= 0) return;

        foreach (PlantState plant in TaxState.PickSeizure(field.Board, -economy.Gold))
        {
            if (economy.Gold >= 0) break;
            economy.Add(plant.GetSellingPrice());
            economy.Add(field.TryKill(plant, DeathCause.Other).BonusGold);
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
        if (!field.AddPlant(plant, cell)) return null;
        economy.Add(FieldState.GetPlantingGold(plant));
        return plant;
    }

    // ───── IFieldActions ─────

    public void Survive(PlantState plant, WaveType wave)
    {
        switch (plant.Kind)
        {
            case PlantKind.Sprinkler:
                plant.SprinklerAfterWave(out _, out _);
                break;
            case PlantKind.MoneyTree:
                MoneyTreeResult outcome = plant.MoneyTreeAfterWave();
                if (outcome == MoneyTreeResult.Growing) break;
                if (outcome == MoneyTreeResult.Matured) economy.Add(PlantState.MoneyTreeRewardGold);
                economy.Add(field.TryKill(plant, DeathCause.Other).BonusGold);
                break;
            default:
                plant.ResistWave(wave);
                break;
        }
    }

    // MoneyTree.OnGridStateChanged: 주변에 완두나 땅콩이 없어진 돈나무는 죽는다
    private void KillLonelyMoneyTrees()
    {
        foreach (PlantState tree in new List<PlantState>(field.Board.StatesInOrder()))
            if (tree.Kind == PlantKind.MoneyTree && !tree.IsDying && !tree.HasAdjacentBreedablePlant())
                economy.Add(field.TryKill(tree, DeathCause.Other).BonusGold);
    }

    public bool Kill(PlantState plant)
    {
        DeathOutcome outcome = field.TryKill(plant, DeathCause.Generic);
        if (outcome.Died) AddGold(outcome.BonusGold);
        return outcome.Died;
    }

    public void OnWaveHit(PlantState plant, WaveType wave, bool isSecondWave)
    {
        result.WaveHits[(int)wave]++;
        events.RaisePlantDied();
    }

    public IReadOnlyList<int> MushroomTiles => curses.MushroomTiles;
    public void Unfreeze(PlantState plant) => plant.SetFrozen(false);
    public void AddGold(int amount) => economy.Add(amount);
    public void Root(PlantState plant) => plant.IsRooted = true;

    public void RefreshTraits(PlantState plant)
    {
        if (!plant.IsMovableKind) return;
        BreedingRules.CompletePairs(plant.Traits, Roll);
        plant.UpdateGoldFromTraits();
    }

    public PlantState SpawnCopy(List<GeneticTrait> traits, int cell, bool benignMutation) => Plant(traits, cell);
    public void AddBonusMultiplier(PlantState plant, int amount) => plant.BonusGoldMultiplierCount += amount;
}
