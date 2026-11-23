using System.Collections.Generic;
using UnityEngine;

// 시뮬레이터의 상점. 진열, 가격, 구매 조건, 효과는 게임의 아이템 코드를 그대로 쓰고, 여기서는 무엇을 살지만 정한다.
public sealed partial class RunSimulator : IShopWorld
{
    private const int MaxPurchasesPerNight = 20;

    private static ItemData[] fixedItems;
    private static ItemData[] rotationItems;
    private static readonly Dictionary<ItemData, (int price, int stock)> savedItems = new Dictionary<ItemData, (int, int)>();

    private readonly ShopContext shopContext;
    private int bonusBreedAttempts;
    private int waveSkips;

    // ───── 에셋 ─────

    private static void LoadItems()
    {
        fixedItems = Resources.LoadAll<ItemData>("Data/Item Data/Fixed");
        rotationItems = Resources.LoadAll<ItemData>(ResourcePaths.RotationItems);

        temporalCurses = Resources.LoadAll<CurseScriptable>("Data/Codex/Curse/temporal");
        seasonalCurses = Resources.LoadAll<CurseScriptable>("Data/Codex/Curse/seasonal");
        // 추첨은 목록 순서를 따른다. 정원 씬의 순서: 단발형은 이름순, 지속형과 의뢰는 번호순
        System.Array.Sort(temporalCurses, (a, b) => string.CompareOrdinal(a.name, b.name));
        System.Array.Sort(seasonalCurses, (a, b) => string.CompareOrdinal(a.curseId, b.curseId));

        var found = new List<RequestScriptable>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:RequestScriptable", new[] { "Assets/Scripts/Request/ReqItems" }))
        {
            var request = UnityEditor.AssetDatabase.LoadAssetAtPath<RequestScriptable>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (request != null && !request.requestId.StartsWith("99")) found.Add(request); // 99로 시작하면 옛 의뢰
        }
        found.Sort((a, b) => string.CompareOrdinal(a.requestId, b.requestId));
        requestPool = found;

        // 아이템이 자기 가격과 재고를 고치므로(가격 상승 등) 판마다, 그리고 끝나고 되돌린다
        savedItems.Clear();
        foreach (ItemData item in fixedItems) savedItems[item] = (item.Price, item.InitialStock);
        foreach (ItemData item in rotationItems) savedItems[item] = (item.Price, item.InitialStock);
    }

    private static void RestoreItems()
    {
        foreach (var kv in savedItems)
        {
            kv.Key.Price = kv.Value.price;
            kv.Key.InitialStock = kv.Value.stock;
            kv.Key.Cancel(null);
        }
    }

    private bool IsUnlocked(ItemData item)
        => config.allUnlocked || (!item.requiresUnlock && item.metaRequiredDawnStage <= 0 && string.IsNullOrEmpty(item.metaRequiredEventId));

    // ───── 밤의 쇼핑 ─────

    private void ShopPhase()
    {
        if (!config.buySeedlings && !config.buyItems) return;

        // 새 웨이브에 쓸 모종 값이 모자라면 남는 식물을 판다
        if (config.buySeedlings && config.bot == SimBotKind.Planner && FindMissingTraitSet(out int owned) >= 0 && owned < 2)
            while (economy.Gold < SeedlingPrice && SellWorst(5)) { }

        // 자리가 없으면 한 포기를 팔아 새 식물이 들어올 칸을 만든다
        if (!field.Board.HasEmptyCell()) SellWorst(6);

        ShopInventory inventory = shopContext.Shop.Generate(fixedItems, rotationItems, config.rotationCount, shopContext, stage, IsUnlocked);

        for (int i = 0; i < MaxPurchasesPerNight; i++)
        {
            Choice choice = config.bot == SimBotKind.Random ? PickRandom(inventory) : PickBest(inventory);
            if (choice.item == null || !Buy(choice, inventory)) break;
        }
    }

    private struct Choice
    {
        public ItemData item;
        public int option;
        public int cell;
        public float score;
    }

    private IEnumerable<ItemData> Buyable(ShopInventory inventory)
    {
        int budget = Budget;
        int reserve = config.bot == SimBotKind.Planner ? SeedlingReserve : 0;
        foreach (List<ItemData> list in new[] { inventory.Fixed, inventory.Rotation })
            foreach (ItemData item in list)
            {
                bool seedling = item is PeaItemData;
                if (seedling ? !config.buySeedlings : !config.buyItems) continue;
                if (shopContext.Shop.WasBoughtThisShop(item) || item.GetDisplayPrice() > (seedling ? budget : budget - reserve)) continue;
                if (!item.CanPurchase(shopContext, out _)) continue;
                yield return item;
            }
    }

    private int SeedlingPrice
    {
        get
        {
            foreach (ItemData item in fixedItems)
                if (item is PeaItemData) return item.GetDisplayPrice();
            return 0;
        }
    }

    // 모자란 형질이 있거나 사흘 안에 새 형질이 풀리면 모종 두 번 살 돈은 남겨 둔다
    private int SeedlingReserve
    {
        get
        {
            if (!config.buySeedlings) return 0;
            bool soon = TraitOptions.GetTraitSets(stage + 3, true).Count > TraitOptions.GetTraitSets(stage, true).Count;
            bool missing = FindMissingTraitSet(out int owners) >= 0 && owners < 2;
            return soon || missing ? SeedlingPrice * 2 : 0;
        }
    }

    // 세금 마감이 이틀 안이면 낼 돈은 남겨 둔다
    private int Budget
    {
        get
        {
            bool dueSoon = config.payTax && config.bot == SimBotKind.Planner && tax.DueStage - stage <= 2;
            return dueSoon ? economy.Gold - tax.DueAmount : economy.Gold;
        }
    }

    private bool Buy(Choice choice, ShopInventory inventory)
    {
        ItemData item = choice.item;
        int price = item.GetDisplayPrice();

        if (choice.option >= 0) item.SetSelectedOption(shopContext, choice.option);
        if (choice.cell >= 0) item.SelectCell(choice.cell);

        bool ready = false;
        item.StartEffect(shopContext, () => ready = true, _ => { });
        if (!ready) { item.Cancel(shopContext); return false; }

        // ShopManager.TryPurchase와 같은 순서: 결제 → 기록 → 효과
        economy.Spend(price);
        events.RaiseShopBought(item);
        shopContext.Shop.RecordPurchase(item, price);
        if (!result.FirstBought.ContainsKey(item.PurchaseKey)) result.FirstBought[item.PurchaseKey] = stage;
        result.Purchases++;
        result.Mix(StableHash(item.PurchaseKey));
        item.Commit(shopContext);
        shopContext.Shop.FinishPurchase(item, inventory);
        return true;
    }

    private static int StableHash(string s)
    {
        int h = 17;
        foreach (char c in s) h = unchecked(h * 31 + c);
        return h;
    }

    // ───── 무작위 봇 ─────

    private Choice PickRandom(ShopInventory inventory)
    {
        var items = new List<ItemData>(Buyable(inventory));
        if (items.Count == 0) return default;

        ItemData item = items[botRng.Range(0, items.Count)];
        int options = item.GetSelectableOptions(shopContext)?.Length ?? 0;
        var choice = new Choice { item = item, option = options > 0 ? botRng.Range(0, options) : -1, cell = -1 };

        if (item.FlowType != ShopFlowType.Instant)
        {
            List<int> cells = ValidCells(item);
            if (cells.Count == 0) return default;
            choice.cell = cells[botRng.Range(0, cells.Count)];
        }
        return choice;
    }

    private List<int> ValidCells(ItemData item)
    {
        var cells = new List<int>();
        for (int cell = 0; cell < field.Board.CellCount; cell++)
        {
            bool valid;
            if (item.FlowType == ShopFlowType.SelectExistingPlant)
                valid = field.Board.States.TryGetValue(cell, out PlantState plant) && item.ValidateTarget(shopContext, plant, out _);
            else
                valid = item.ValidateCell(shopContext, cell, out _);
            if (valid) cells.Add(cell);
        }
        return cells;
    }

    // ───── 계획 봇 ─────

    // 아이템별 선호는 사람이 정한 어림값이다. 봇의 성적은 밸런스 수치가 아니라 변경 전후를 견주는 잣대로 쓴다.
    private Choice PickBest(ShopInventory inventory)
    {
        Choice best = default;
        foreach (ItemData item in Buyable(inventory))
        {
            Choice choice = Evaluate(item);
            if (choice.item != null && choice.score > best.score) best = choice;
        }
        return best;
    }

    private Choice Evaluate(ItemData item)
    {
        var choice = new Choice { item = item, option = -1, cell = -1 };
        int plants = field.Board.CountLivingPlants();
        int empty = field.Board.CellCount - field.Board.States.Count;
        bool late = stage > config.days - 4; // 막판에는 투자성 아이템을 사지 않는다

        switch (item)
        {
            case PeaItemData _:
                int set = FindMissingTraitSet(out int owners);
                if (set < 0) return default;
                choice.option = set;
                choice.score = owners == 0 ? 100f : 70f - owners * 5f;
                break;
            case HighQualityPlantItemData _:
                choice.option = 0;
                choice.score = CountOwners(currentWave) < 4 ? 90f : 35f;
                break;
            case PlantHealingItemData _:
                return WithWave(choice, currentWave, CountOwners(currentWave) >= 3 ? 80f : 0f);
            case FertilizerItemData _:
                choice.cell = BestColumnCell(item);
                return choice.cell < 0 ? default : WithWave(choice, nextWave == WaveType.Aging ? currentWave : nextWave, 45f);
            case SignPostItemData _:
                WaveType weakest = WeakestWave();
                return WithWave(choice, weakest, 40f);

            case BreedCountItemData _: choice.score = late ? 0f : 60f; break;
            case AddSoilItemData _: choice.score = late || empty > 3 ? 0f : 65f; break;
            case CoolingShieldItemData _: choice.score = 30f; break;
            case TimeMachineItemData _: choice.score = waveSkips < 2 ? 50f : 10f; break;
            case AdrenalineItemData _: choice.score = late ? 0f : 35f; break;
            case DNAExtractionItemData _:
                if (empty < 3) return default;
                choice.cell = BestPlantCell(item);
                choice.score = plants < 8 ? 55f : 15f;
                break;
            case ChiliPepperItemData _:
                if (empty < 2) return default;
                choice.cell = CellWithMostNeighbours(item, vertical: field.Modifiers.ChiliPepperRangeLevel >= 1);
                choice.score = CountKind(PlantKind.ChiliPepper) < 2 ? 50f : 10f;
                break;
            case SprinklerItemData _:
                if (empty < 2 || late) return default;
                choice.cell = CellWithMostNeighbours(item, vertical: true);
                choice.score = CountKind(PlantKind.Sprinkler) < 2 ? 40f : 5f;
                break;
            case MoneyTreeItemData _:
                if (empty < 2 || stage > config.days - 6) return default;
                choice.cell = CellWithMostNeighbours(item, vertical: true);
                choice.score = CountKind(PlantKind.MoneyTree) < 2 ? 55f : 5f;
                break;
            case RapidFreezerItemData _:
                choice.cell = CellWithMostPlantsAround();
                choice.score = 45f;
                break;
            case PetBottleItemData _:
                choice.cell = BestPlantCell(item);
                choice.score = field.Soil.PetBottleTiles.Count < 3 ? 35f : 0f;
                break;
            case GoldSoilItemData _:
                List<int> free = ValidCells(item);
                choice.cell = free.Count > 0 ? free[0] : -1;
                choice.score = late ? 0f : 40f;
                break;

            case StrongGeneticsResistanceUpgradeItemData _: choice.score = 42f; break;
            case WeakGeneticsResistanceUpgradeItemData _: choice.score = 44f; break;
            case GoldenGeneticsItemData _: choice.score = late ? 0f : 48f; break;
            case PeaMaturationUpgradeItemData _: choice.score = late ? 0f : 55f; break;
            case PeaTasteUpgradeItemData _: choice.score = late ? 0f : 52f; break;
            case BadGuyMoreRiceItemData _: choice.score = late ? 0f : 40f; break;
            case TwinsItemData _: choice.score = late ? 0f : 45f; break;
            case PeaCoffeeItemData _: choice.score = late ? 0f : 42f; break;
            case LandAndBeanItemData _: choice.score = late ? 0f : 30f; break;
            case RoyalSuccessionItemData _: choice.score = late ? 0f : 25f; break;
            case ActiveShellItemData _: choice.score = late ? 0f : 40f; break;
            case HealingCapsaicinItemData _: choice.score = CountKind(PlantKind.ChiliPepper) > 0 ? 25f : 0f; break;
            case ChiliPepperRangeUpgradeItemData _: choice.score = CountKind(PlantKind.ChiliPepper) > 0 ? 40f : 0f; break;
            case SprinklerRangeUpgradeItemData _: choice.score = CountKind(PlantKind.Sprinkler) > 0 && !late ? 25f : 0f; break;
            case SprinklerFertilizerSynergyItemData _: choice.score = CountKind(PlantKind.Sprinkler) > 0 && field.Soil.FertilizerColumns.Count > 0 ? 15f : 0f; break;

            // 벌레, 리롤, 교배 시간처럼 시뮬레이터에 없는 것을 돕는 아이템은 사지 않는다
            default: return default;
        }

        bool needsCell = item.FlowType != ShopFlowType.Instant;
        return choice.score <= 0f || (needsCell && choice.cell < 0) ? default : choice;
    }

    private Choice WithWave(Choice choice, WaveType wave, float score)
    {
        choice.option = WaveSchedule.GetSelectableWaves(stage, true).IndexOf(wave);
        choice.score = score;
        return choice.option < 0 || score <= 0f ? default : choice;
    }

    // 가진 식물이 가장 적은 형질 묶음. 모두 3포기 이상이면 -1.
    private int FindMissingTraitSet(out int owners)
    {
        List<TraitType[]> sets = TraitOptions.GetTraitSets(stage, isNight: true);
        int best = -1;
        owners = 3;
        for (int i = 0; i < sets.Count; i++)
        {
            int count = CountOwners((WaveType)(int)sets[i][0]);
            if (count < owners) { owners = count; best = i; }
        }
        return best;
    }

    private int CountOwners(WaveType wave)
    {
        int count = 0;
        foreach (PlantState plant in SortedPlants())
            if (plant.HasTrait(wave)) count++;
        return count;
    }

    private int CountKind(PlantKind target)
    {
        int count = 0;
        foreach (PlantState plant in field.Board.StatesInOrder())
            if (plant.Kind == target) count++;
        return count;
    }

    private WaveType WeakestWave()
    {
        WaveType weakest = WaveType.Aging;
        float lowest = float.PositiveInfinity;
        foreach (WaveType wave in WaveSchedule.GetSelectableWaves(stage, true))
        {
            float sum = 0f;
            foreach (PlantState plant in SortedPlants()) sum += plant.GetResistanceValue((int)wave);
            if (sum < lowest) { lowest = sum; weakest = wave; }
        }
        return weakest;
    }

    private int BestPlantCell(ItemData item)
    {
        float[] weights = TraitWeights();
        int best = -1;
        float highest = float.NegativeInfinity;
        foreach (int cell in ValidCells(item))
        {
            if (!field.Board.States.TryGetValue(cell, out PlantState plant)) continue;
            float score = Score(plant, weights);
            if (score > highest) { highest = score; best = cell; }
        }
        return best;
    }

    private int BestColumnCell(ItemData item)
    {
        int best = -1, most = -1;
        foreach (int cell in ValidCells(item))
        {
            int count = 0;
            for (int row = 0; row < PlantBoard.RowCount; row++)
                if (field.Board.HasBreedablePlantAt(cell / 4 * 4 + row)) count++;
            if (count > most) { most = count; best = cell; }
        }
        return best;
    }

    private int CellWithMostNeighbours(ItemData item, bool vertical)
    {
        int best = -1, most = 0;
        foreach (int cell in ValidCells(item))
        {
            int count = 0;
            if (field.Board.HasBreedablePlantAt(cell - 4)) count++;
            if (field.Board.HasBreedablePlantAt(cell + 4)) count++;
            if (vertical && cell % 4 > 0 && field.Board.HasBreedablePlantAt(cell - 1)) count++;
            if (vertical && cell % 4 < 3 && field.Board.HasBreedablePlantAt(cell + 1)) count++;
            if (count > most) { most = count; best = cell; }
        }
        return best;
    }

    private int CellWithMostPlantsAround()
    {
        int best = 0, most = -1;
        for (int cell = 0; cell < field.Board.CellCount; cell++)
        {
            int count = field.Soil.GetFreezeTargets(cell).Count;
            if (count > most) { most = count; best = cell; }
        }
        return best;
    }

    // 타임 머신: 절반 넘게 죽을 것 같으면 웨이브를 건너뛴다
    private void UseWaveSkip()
    {
        if (waveSkips <= 0 || currentWave == WaveType.None) return;

        if (config.bot == SimBotKind.Planner)
        {
            float sum = 0f;
            List<PlantState> plants = SortedPlants();
            foreach (PlantState plant in plants) sum += plant.GetResistanceValue((int)currentWave);
            if (plants.Count > 0 && sum / plants.Count >= 0.5f) return;
        }
        waveSkips--;
        currentWave = WaveType.None;
    }

    /// <summary>빈 밭의 시뮬레이터 세계 하나. 아이템 효과를 게임과 대조할 때 쓴다.</summary>
    public static IShopWorld CreateWorld(SimConfig config, int stage, WaveType currentWave)
        => new RunSimulator(config, 0) { stage = stage, currentWave = currentWave };

    // ───── IShopWorld ─────

    FieldState IShopWorld.Field => field;
    ModState IShopWorld.Mods => mods;
    int IShopWorld.Stage => stage;
    WaveType IShopWorld.CurrentWave => currentWave;
    string IShopWorld.Species => config.species;
    bool IShopWorld.IsNight => true; // 봇은 밤에만 산다

    float IShopWorld.RollResistance(TraitType trait, int genetics) => Roll(trait, genetics);
    void IShopWorld.PlantSeedling(List<GeneticTrait> traits) => Plant(traits, -1);
    void IShopWorld.PlantFixed(PlantKind kind, int cell) => field.AddPlant(new PlantState(kind), cell);

    void IShopWorld.ExpandField() => field.Board.MaxCol++;
    void IShopWorld.AddLandRichColumn() => field.Soil.AddLandRichColumn();
    bool IShopWorld.PlaceFertilizer(int cell, WaveType wave) => field.Soil.PlaceFertilizer(cell, wave);
    bool IShopWorld.PlaceGoldSoil(int cell) => field.Soil.PlaceGoldSoil(cell);
    void IShopWorld.PlacePetBottle(int cell) => field.Soil.PlacePetBottle(cell);
    void IShopWorld.AddAbsorbFertilizer(int cell) => field.Soil.AddAbsorbFertilizer(cell);

    void IShopWorld.FreezeArea(int cell)
    {
        foreach (PlantState plant in field.Soil.GetFreezeTargets(cell)) plant.SetFrozen(true);
    }

    void IShopWorld.AddBreedAttempts(int count) => bonusBreedAttempts += count;
    void IShopWorld.AddBreedSeconds(int seconds) { } // 교배 시간 제한이 없다
    void IShopWorld.AddWaveSkips(int count) => waveSkips += count;

    void IShopWorld.AddTimedMultiplier(StatId stat, int param, float multiplier, int days, string sourceTag)
        => mods.AddTimed(stat, param, ModOp.Multiply, multiplier, stage, days, sourceTag);

    void IShopWorld.AddWeakGeneticsResistanceBonus(float value)
    {
        field.Modifiers.AddWeakGeneticsResistanceBonus(value);
        foreach (PlantState plant in SortedPlants()) plant.IncreaseResistance(false, value);
    }

    void IShopWorld.AddStrongGeneticsResistanceBonus(float value)
    {
        field.Modifiers.AddStrongGeneticsResistanceBonus(value);
        foreach (PlantState plant in SortedPlants()) plant.IncreaseResistance(true, value);
    }

    void IShopWorld.AddSprinklerFertilizerSynergyBonus(float value) => field.Modifiers.AddSprinklerFertilizerSynergyBonus(value);
    void IShopWorld.AddChiliPepperRangeLevel(int value) => field.Modifiers.AddChiliPepperRangeLevel(value);
    void IShopWorld.EnableNepenthesPheromone() => field.Modifiers.SetNepenthesPheromone(true);
    void IShopWorld.AddNepenthesPheromoneSize(float value) => field.Modifiers.AddAdditionalNepenthesPheromoneSizeMultiplier(value);
    void IShopWorld.AddAdditionalPlantGold(int value) => field.Modifiers.AddAdditionalPlantGold(value);
    void IShopWorld.AddAdditionalPlantGoldMultiplier(float value) => field.Modifiers.AddAdditionalPlantGoldMultiplier(value);
    void IShopWorld.AddPeaCoffeeMultiplier(float value) => field.Modifiers.AddPeaCoffeeMultiplier(value);
    void IShopWorld.AddLandAndBeanLevel(int value) => field.Modifiers.AddLandAndBeanLevel(value);
}
