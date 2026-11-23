using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// 대조 테스트에서 한 판을 조작하는 손. 게임(<see cref="GameParityActor"/>)과 시뮬레이터가 각각 구현한다.
/// 게임 쪽은 플레이어가 쓰는 것과 같은 함수를 부른다.
/// </summary>
public interface IParityActor
{
    FieldState Field { get; }
    ShopContext Shop { get; }
    string Species { get; }
    int Stage { get; }
    int Gold { get; }
    int WaveSkips { get; }
    int UsedBreeds { get; }
    int MaxBreeds { get; }
    TaxState Tax { get; }
    RequestBoard Requests { get; }
    ShopInventory Inventory { get; }

    void AddGold(int amount);
    void Spawn(List<GeneticTrait> traits);
    void Breed(PlantState a, PlantState b);
    void Move(PlantState plant, int cell);
    void Sell(PlantState plant);
    void SkipWave();
    void PayTax();
    void Claim(RequestInstance request);
    void Buy(ItemData item, int option, int cell);
    void Reroll();

    void AppendExtra(StringBuilder sb);
}

/// <summary>게임과 시뮬레이터에 똑같은 조작을 시키고 상태를 글자로 남긴다.</summary>
public sealed class ParityScript
{
    // 남은 낮 시간(초)으로 골드를 주는 아이템은 실제 시간에 따라 달라진다
    private const string TimeBasedItem = "TimeIsGoldItemData";

    private readonly int seed;
    private RngStream rng; // 조작을 고르는 난수. 게임 난수를 쓰지 않는다.

    public ParityScript(int seed) => this.seed = seed;

    public StringBuilder Log { get; } = new StringBuilder();

    private bool Chance(int percent) => rng.Range(0, 100) < percent;

    // ───── 조작 ─────

    public void Day(IParityActor a)
    {
        rng = new RngStream(unchecked((ulong)(uint)seed * 1000UL + (ulong)a.Stage));
        Record("낮 시작", a);
        if (a.Stage == 1)
        {
            // 상점, 세금, 특수 아이템을 고루 거치게
            a.AddGold(15000);
            for (int i = 0; i < 4; i++) AcquireSpecial(a);
        }
        if (a.Stage % 5 == 0) a.AddGold(6000);

        // 전멸하지 않게 튼튼한 식물을 채운다
        while (Living(a).Count < 8 && a.Field.Board.HasEmptyCell()) a.Spawn(SupplyTraits());

        for (int i = rng.Range(0, 4); i > 0; i--) MoveRandom(a);
        while (Chance(90) && BreedRandom(a)) { }
        if (Chance(40)) SellRandom(a);
        if (a.WaveSkips > 0 && Chance(30)) a.SkipWave();
        Record("낮 끝", a);
    }

    public void Night(IParityActor a)
    {
        Record("밤 시작", a);
        for (int i = rng.Range(0, 4); i > 0; i--) SellRandom(a);
        Claim(a);
        if (a.Tax.IsDue(a.Stage) && a.Gold >= a.Tax.DueAmount && Chance(70)) a.PayTax();

        if (SpecialItemSystem.PendingGifts > 0) AcquireSpecial(a);

        for (int i = rng.Range(1, 6); i > 0; i--) BuyRandom(a);
        if (Chance(20) && a.Gold >= ShopController.RerollPrice)
        {
            a.Reroll();
            BuyRandom(a);
        }
        Claim(a);
        Record("밤 끝", a);
    }

    private void AcquireSpecial(IParityActor a)
    {
        List<SpecialItemData> items = SpecialItemSystem.RollCandidates(a.Species, 3, _ => true);
        if (items.Count == 0) return;
        SpecialItemData pick = items[rng.Range(0, items.Count)];
        Note("특수 " + pick.id);
        SpecialItemSystem.Acquire(pick);
    }

    private List<GeneticTrait> SupplyTraits()
    {
        var traits = new List<GeneticTrait>();
        for (TraitType t = TraitType.NaturalDeath; t < TraitType.None; t++)
            traits.Add(new GeneticTrait(t, 0.7f + rng.Range(0, 26) * 0.01f, rng.Range(0, 3), 0f));
        return traits;
    }

    private static List<PlantState> Living(IParityActor a)
    {
        var list = new List<PlantState>();
        foreach (PlantState plant in a.Field.Board.StatesInOrder())
            if (plant.IsMovableKind && !plant.IsDying) list.Add(plant);
        return list;
    }

    private PlantState Pick(List<PlantState> list) => list.Count == 0 ? null : list[rng.Range(0, list.Count)];

    private void MoveRandom(IParityActor a)
    {
        PlantState plant = Pick(Living(a).FindAll(p => p.CanMove() && !p.IsFrozen));
        var empty = new List<int>();
        for (int cell = 0; cell < a.Field.Board.CellCount; cell++)
            if (!a.Field.Board.States.ContainsKey(cell)) empty.Add(cell);
        if (plant == null || empty.Count == 0) return;

        int to = empty[rng.Range(0, empty.Count)];
        Note($"이동 {plant.GridIndex}→{to}");
        a.Move(plant, to);
    }

    private bool BreedRandom(IParityActor a)
    {
        List<PlantState> parents = Living(a).FindAll(p => p.Breedable && !p.IsFrozen);
        if (parents.Count < 2) return false;
        PlantState first = Pick(parents);
        parents.Remove(first);
        PlantState second = Pick(parents);
        if (BreedingRules.Check(a.Field, first, second, a.UsedBreeds, a.MaxBreeds) != BreedCheck.Ok) return false;

        Note($"교배 {first.GridIndex}+{second.GridIndex}");
        a.Breed(first, second);
        return true;
    }

    private void SellRandom(IParityActor a)
    {
        List<PlantState> plants = Living(a);
        if (plants.Count <= 6) return;
        PlantState plant = Pick(plants);
        Note($"판매 {plant.GridIndex} {plant.GetSellingPrice()}");
        a.Sell(plant);
    }

    private void Claim(IParityActor a)
    {
        foreach (RequestInstance request in new List<RequestInstance>(a.Requests.Active))
            if (request.CanAcceptReward) a.Claim(request);
    }

    private void BuyRandom(IParityActor a)
    {
        var items = new List<ItemData>();
        foreach (List<ItemData> list in new[] { FixedInOrder(a.Inventory), a.Inventory.Rotation })
            foreach (ItemData item in list)
                if (item.name != TimeBasedItem && !a.Shop.Shop.WasBoughtThisShop(item) && item.GetDisplayPrice() <= a.Gold
                    && item.CanPurchase(a.Shop, out _)) items.Add(item);
        if (items.Count == 0) return;

        ItemData pick = items[rng.Range(0, items.Count)];
        int options = pick.GetSelectableOptions(a.Shop)?.Length ?? 0;
        int option = options > 0 ? rng.Range(0, options) : -1;
        int cell = -1;
        if (pick.FlowType != ShopFlowType.Instant)
        {
            var cells = new List<int>();
            for (int c = 0; c < a.Field.Board.CellCount; c++)
                if (pick.FlowType == ShopFlowType.SelectExistingPlant
                    ? a.Field.Board.States.TryGetValue(c, out PlantState plant) && pick.ValidateTarget(a.Shop, plant, out _)
                    : pick.ValidateCell(a.Shop, c, out _)) cells.Add(c);
            if (cells.Count == 0) return;
            cell = cells[rng.Range(0, cells.Count)];
        }
        Note($"구매 {pick.name} {pick.GetDisplayPrice()} 선택 {option} 칸 {cell}");
        a.Buy(pick, option, cell);
    }

    // 고정 상품이 놓인 차례는 씬에서 정하고 규칙과 무관하다
    private static List<ItemData> FixedInOrder(ShopInventory inventory)
    {
        var list = new List<ItemData>(inventory.Fixed);
        list.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
        return list;
    }

    // ───── 기록 ─────

    private void Note(string text) => Log.Append("  ").AppendLine(text);

    private static string F(float value) => value.ToString("F3", CultureInfo.InvariantCulture);

    private void Record(string when, IParityActor a)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{a.Stage}일차 {when}] 골드 {a.Gold} 세금 {a.Tax.LastPaidStage} 스킵 {a.WaveSkips} 교배 {a.UsedBreeds}/{a.MaxBreeds} 칸 {a.Field.Board.CellCount}");
        a.AppendExtra(sb);

        foreach (PlantState p in a.Field.Board.StatesInOrder())
        {
            sb.Append($"  {p.GridIndex} {p.Kind} 맛 {p.Taste} 값 {p.GetSellingPrice()} 버팀 {p.ResistWaveCount}+{p.BonusGoldMultiplierCount} 커피 {p.FreeTimePassedCount} 이동 {F(p.TravelSellBonus)} 턴 {p.SurvivedTurns}"
                + $" {(p.IsRooted ? "뿌리 " : "")}{(p.IsFrozen ? "얼음 " : "")}{(p.IsGold ? "황금 " : "")}{(p.Breedable ? "" : "교배불가 ")}{(p.HasTriedBreed ? "교배함" : "")} 형질");
            foreach (GeneticTrait t in p.Traits) sb.Append($" {t.traitType}:{t.genetics}:{F(t.resistance)}+{F(t.additionalResistance)}={F(p.GetResistanceValue((int)t.traitType))}");
            sb.AppendLine();
        }

        SoilState soil = a.Field.Soil;
        sb.AppendLine($"  땅 황금 {string.Join(",", soil.GoldSoilTiles)} 페트 {string.Join(",", soil.PetBottleTiles)} 흡수 {Sorted(soil.AbsorbFertilizerTiles)} 비료 {Sorted(soil.FertilizerColumns)} 배수 {Sorted(soil.ColumnGoldMulBonus)} 방패 {soil.HasCoolingShield}");
        var save = new GridSave();
        a.Field.Modifiers.Save(save);
        sb.AppendLine("  보정 " + JsonUtility.ToJson(save));
        sb.AppendLine($"  구매 {Sorted(a.Shop.Shop.PurchaseHistory)} 무료리롤 {a.Shop.Shop.DailyRerollCount}");
        sb.Append($"  의뢰 완료 {a.Requests.CompletedCount} 진행");
        foreach (RequestInstance r in a.Requests.Active) sb.Append($" {r.Data.requestId}:{r.State}:{r.GetProgressText()}");
        sb.AppendLine();
        var special = new List<string>(SpecialItemSystem.OwnedIds);
        special.Sort(StringComparer.Ordinal);
        sb.AppendLine($"  특수 {string.Join(",", special)} 선물 {SpecialItemSystem.PendingGifts}");

        var rngSave = new RngSave();
        GameRun.Rng.Save(rngSave);
        rngSave.states.RemoveAt(rngSave.states.Count - 1); // 벌레는 대조하지 않는다
        sb.AppendLine("  난수 " + string.Join(",", rngSave.states));
        Log.Append(sb);
    }

    private static string Sorted<T>(IEnumerable<T> values)
    {
        var list = new List<string>();
        foreach (T v in values) list.Add(v.ToString());
        list.Sort(StringComparer.Ordinal);
        return string.Join(",", list);
    }

    public static void AppendShared(StringBuilder sb, WaveType current, WaveType next, WaveType? second, CurseBoard curses, List<Mod> mods, ShopInventory inventory)
    {
        sb.AppendLine($"  웨이브 {current}→{next}/{second}");
        sb.AppendLine($"  저주 {curses.Temporal?.Data.curseId}:{curses.RemainingTemporalDays} {curses.Seasonal?.Data.curseId}:{curses.RemainingSeasonalDays} 안개 {string.Join(",", curses.FogTiles)} 버섯 {string.Join(",", curses.MushroomTiles)}");
        sb.Append("  기간제");
        foreach (Mod mod in mods) sb.Append(' ').Append(JsonUtility.ToJson(mod));
        sb.AppendLine();
        if (inventory == null) return;
        sb.Append("  진열");
        foreach (ItemData item in FixedInOrder(inventory)) sb.Append($" {item.name}:{item.GetDisplayPrice()}");
        sb.Append(" |");
        foreach (ItemData item in inventory.Rotation) sb.Append($" {item.name}:{item.GetDisplayPrice()}");
        sb.AppendLine();
    }
}

public sealed partial class RunSimulator : IParityActor
{
    private ParityScript script;
    private int usedBreeds;
    private ShopInventory scriptInventory;

    /// <summary>대본대로 한 판을 돌리고 기록을 돌려준다.</summary>
    public static string RunScript(SimConfig config, int seed)
    {
        var parity = new ParityScript(seed);
        RunMany(config, seed, parity);
        return parity.Log.ToString();
    }

    private void ScriptNight()
    {
        scriptInventory = GenerateInventory();
        script.Night(this);
        scriptInventory = null;
    }

    FieldState IParityActor.Field => field;
    ShopContext IParityActor.Shop => shopContext;
    string IParityActor.Species => config.species;
    int IParityActor.Stage => stage;
    int IParityActor.Gold => economy.Gold;
    int IParityActor.WaveSkips => waveSkips;
    int IParityActor.UsedBreeds => usedBreeds;
    int IParityActor.MaxBreeds => MaxBreedAttempts;
    TaxState IParityActor.Tax => tax;
    RequestBoard IParityActor.Requests => requests;
    ShopInventory IParityActor.Inventory => scriptInventory;

    void IParityActor.AddGold(int amount) => economy.Add(amount);
    void IParityActor.Spawn(List<GeneticTrait> traits) => Plant(traits, -1);
    void IParityActor.Breed(PlantState a, PlantState b) => BreedOnce(a, b);
    void IParityActor.Move(PlantState plant, int cell) => MovePlant(plant, cell);
    void IParityActor.Sell(PlantState plant) => Sell(plant);

    void IParityActor.SkipWave() => waves.SkipToday();

    void IParityActor.PayTax()
    {
        if (!tax.CanPay(economy, out int due)) return;
        economy.Spend(due);
        tax.MarkPaid();
    }

    void IParityActor.Claim(RequestInstance request)
    {
        if (request.TryGrant(out int gold)) economy.Add(gold);
    }

    void IParityActor.Buy(ItemData item, int option, int cell)
        => Buy(new Choice { item = item, option = option, cell = cell }, scriptInventory);

    void IParityActor.Reroll()
    {
        if (!shopContext.Shop.UseFreeReroll()) economy.Spend(ShopController.RerollPrice);
        shopContext.Shop.Reroll();
        scriptInventory = GenerateInventory();
    }

    void IParityActor.AppendExtra(StringBuilder sb)
        => ParityScript.AppendShared(sb, currentWave, nextWave, waves.Second, curses, mods.Mods, scriptInventory);
}
