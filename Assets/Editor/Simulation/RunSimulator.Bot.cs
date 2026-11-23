using System;
using System.Collections.Generic;

/// <summary>계획 봇의 조절값. <see cref="SimBotTuner"/>가 찾아 준다.</summary>
[Serializable]
public class SimBotParams
{
    public float weightToday = 4.59f;      // 교배와 보존에서 오늘 웨이브를 보는 비중
    public float weightNext = 0.91f;
    public float weightFuture = 0.53f;     // 그 뒤에 올 수 있는 웨이브 하나하나
    public float genetics = 0.3f;          // 유전자(0~2) 하나를 저항력 얼마로 치는가
    public float keepParents = 13f;        // 좋은 순서로 이만큼은 부모로 아껴 둔다
    public float parentValue = 93f;        // 가장 좋은 부모 하나를 팔 때 잃는 것으로 치는 골드
    public float minKeep = 7f;             // 이 수까지만 판다
    public float breedValue = 96f;         // 자리가 없을 때, 이만큼까지 손해 보고 팔아 새 식물을 낳는다
    public float saveBreedPlants = 12f;    // 식물이 이만큼 있으면 교배를 아껴 의뢰를 채운다
    public float requestSale = 1.69f;      // 의뢰 보상을 얼마나 확실하다고 보고 파는가
    public float taxDays = 1f;             // 마감 며칠 전부터 세금 낼 돈을 남기는가
    public float liquidation = 0.25f;      // 식물을 팔아 세금에 보탤 수 있다고 보는 비율
    public float buyRequest = 0.87f;       // 구매 의뢰를 채우려고 보상의 얼마까지 쓰는가
    public float priceExponent = 0.3f;     // 아이템 점수를 가격으로 나누는 정도
    public float buyThreshold = 35f;
    public float rerollGold = 2704f;       // 쓸 돈이 이보다 많으면 리롤한다
    public float seedlingReserve = 1.75f;  // 모자란 형질이 있거나 곧 풀리면 모종 몇 개 값을 남기는가
    public float lateDays = 9.31f;         // 끝나기 며칠 전부터 투자성 아이템을 사지 않는가

    // 아이템 점수
    public float seedlingFirst = 54f, seedlingMore = 60f, highQuality = 59f, healing = 80f, fertilizer = 46f, signPost = 10f;
    public float breedCount = 74f, addSoil = 60f, coolingShield = 43f, timeMachine = 50f, adrenaline = 13f, dna = 55f;
    public float chili = 23f, chiliMax = 2f, sprinkler = 40f, sprinklerMax = 1f, moneyTree = 40f, moneyTreeMax = 5f;
    public float freezer = 49f, petBottle = 77f, petBottleMax = 3f, goldSoil = 20f;
    public float strong = 27f, weak = 55f, goldenGenetics = 48f, maturation = 31f, taste = 161f, badGuy = 40f, twins = 45f, coffee = 54f;
    public float landAndBean = 30f, royal = 41f, activeShell = 40f, capsaicin = 25f, chiliRange = 40f, sprinklerRange = 34f, synergy = 26f;
}

// 계획 봇: 웨이브 전에 죽을 식물을 팔고, 의뢰를 골라 채우고, 세금 낼 돈을 남기고 투자한다.
public sealed partial class RunSimulator
{
    private const int HoldDays = 8; // 식물을 갖고 있을 때의 가치를 며칠 앞까지 보는가

    private SimBotParams Bot => config.botParams;
    private bool IsPlanner => config.bot == SimBotKind.Planner;
    private bool noSellToday;

    // ───── 웨이브 예측 ─────

    private float[] futureWeights = new float[Wave.NumberOfWave];

    // 모레 이후의 웨이브 확률. 사람도 제철과 해금 일정을 알고 있다.
    private void RefreshForecast()
    {
        futureWeights = new float[Wave.NumberOfWave];
        float sum = 0f;
        foreach (var kv in deck.GetEffectiveWeights(stage))
        {
            if (kv.Value <= 0f || kv.Key == WaveType.None) continue;
            futureWeights[(int)kv.Key] = kv.Value;
            sum += kv.Value;
        }
        if (sum <= 0f) return;
        for (int i = 0; i < futureWeights.Length; i++) futureWeights[i] /= sum;
    }

    private float Survival(PlantState plant, WaveType wave)
    {
        if (wave == WaveType.None || plant.IsFrozen || field.Soil.HasPetBottle(plant.GridIndex)) return 1f;
        float r = plant.GetResistanceValue((int)wave);
        return SpecialItemSystem.Has("double_try") ? 1f - (1f - r) * (1f - r) : r;
    }

    private float FutureSurvival(PlantState plant)
    {
        float sum = 0f;
        for (int i = 0; i < futureWeights.Length; i++)
            if (futureWeights[i] > 0f) sum += futureWeights[i] * Survival(plant, (WaveType)i);
        return sum;
    }

    /// <summary>지금 팔지 않고 가장 좋은 때에 팔 때의 기대 판매가.</summary>
    private float HoldValue(PlantState plant, int price)
    {
        float multiplier = field.Modifiers.GetAdditionalPlantGoldMultiplier();
        int count = plant.ResistWaveCount + plant.BonusGoldMultiplierCount;
        float growth = (multiplier + field.Modifiers.PeaCoffeeMultiplier) / (1f + multiplier * count
            + field.Modifiers.PeaCoffeeMultiplier * plant.FreeTimePassedCount);

        bool blind = CurseState.WaveBlind;
        float future = FutureSurvival(plant), alive = 1f, best = 0f;
        int days = Math.Min(HoldDays, config.days - stage + 1);
        for (int day = 1; day <= days; day++)
        {
            alive *= blind || day > 2 ? future : Survival(plant, day == 1 ? currentWave : nextWave);
            best = Math.Max(best, alive * price * (1f + growth * day));
        }
        return best;
    }

    // ───── 판매 ─────

    /// <summary>지금 팔면 잃는 값이 작은 순서.</summary>
    private List<(PlantState plant, float loss)> RankSales()
    {
        List<PlantState> plants = SortedPlants();
        float[] weights = TraitWeights();
        var scored = plants.ConvertAll(p => (plant: p, score: Score(p, weights)));
        scored.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.plant.GridIndex.CompareTo(b.plant.GridIndex));

        var list = new List<(PlantState plant, float loss)>();
        for (int i = 0; i < scored.Count; i++)
        {
            PlantState plant = scored[i].plant;
            int price = plant.GetSellingPrice();
            float parent = Bot.parentValue * Math.Max(0f, 1f - i / Bot.keepParents);
            list.Add((plant, HoldValue(plant, price) + parent - price));
        }
        list.Sort((a, b) => a.loss != b.loss ? a.loss.CompareTo(b.loss) : a.plant.GridIndex.CompareTo(b.plant.GridIndex));
        return list;
    }

    private bool SellCheapest(int minKeep, float maxLoss)
    {
        if (noSellToday || field.Board.CountLivingPlants() <= minKeep) return false;
        foreach ((PlantState plant, float loss) in RankSales())
        {
            if (loss > maxLoss) return false;
            if (Sell(plant)) return true;
        }
        return false;
    }

    // 웨이브 전: 갖고 있는 것보다 지금 파는 편이 나은 식물을 판다
    private void SellDoomed()
    {
        if (noSellToday) return;
        foreach ((PlantState plant, float loss) in RankSales())
        {
            if (loss > 0f || field.Board.CountLivingPlants() <= (int)Bot.minKeep) return;
            Sell(plant);
        }
    }

    // 판매 의뢰를 채운다. 보상을 남은 수로 나눈 값이 파는 손해보다 크면 판다.
    private void SellForRequests()
    {
        if (noSellToday) return;
        for (int guard = 0; guard < 30 && field.Board.CountLivingPlants() > (int)Bot.minKeep; guard++)
        {
            PlantState pick = null;
            float best = 0f;
            foreach ((PlantState plant, float loss) in RankSales())
            {
                float gain = -loss;
                bool useful = false;
                foreach (RequestInstance request in requests.Active)
                {
                    if (request.State != RequestState.InProgress || !request.AcceptsSale(plant)) continue;
                    if (request is SellPlantFreeTimeRequest && !isNight) continue;
                    gain += Bot.requestSale * request.GoldReward / Math.Max(1, request.Remaining);
                    useful = true;
                }
                if (useful && gain > best) { best = gain; pick = plant; }
            }
            if (pick == null || !Sell(pick)) return;
            ClaimRequests();
        }
    }

    // ───── 의뢰 ─────

    private int RequestDaysLeft => requests.CycleEndRound - stage + 1;

    private int MaxRemaining<T>() where T : RequestInstance
    {
        int most = 0;
        foreach (RequestInstance request in requests.Active)
            if (request is T && request.State == RequestState.InProgress) most = Math.Max(most, request.Remaining);
        return most;
    }

    // 팔지 않기 의뢰는 세금일 전에 몰아서 채운다
    private void DecideNoSell()
    {
        noSellToday = false;
        if (!IsPlanner) return;
        int need = MaxRemaining<NoSellDayRequest>();
        if (need == 0) return;
        if (need > RequestDaysLeft) { need = MinRemaining<NoSellDayRequest>(); if (need > RequestDaysLeft) return; }
        if (config.payTax && tax.IsDue(stage) && economy.Gold < tax.DueAmount) return;
        noSellToday = true;
    }

    private int MinRemaining<T>() where T : RequestInstance
    {
        int least = int.MaxValue;
        foreach (RequestInstance request in requests.Active)
            if (request is T && request.State == RequestState.InProgress) least = Math.Min(least, request.Remaining);
        return least;
    }

    // 교배 아끼기 의뢰를 위해 오늘 남길 횟수
    private int BreedsToSave(int max)
    {
        if (!IsPlanner) return 0;
        int need = MaxRemaining<SaveBreedingCountRequest>();
        if (need == 0 || need > max * RequestDaysLeft) return 0;
        bool last = need > max * (RequestDaysLeft - 1);
        if (!last && field.Board.CountLivingPlants() < Bot.saveBreedPlants) return 0;
        return Math.Min(need, max);
    }

    // 하루에 여러 개 사는 의뢰: 싼 것부터 채운다
    private void BuyForRequests(ShopInventory inventory)
    {
        int need = MaxRemaining<BuyItemInDayRequest>();
        if (need == 0) return;

        int reward = 0;
        foreach (RequestInstance request in requests.Active)
            if (request is BuyItemInDayRequest && request.State == RequestState.InProgress) reward += request.GoldReward;

        List<Choice> choices = FillerChoices(inventory);
        if (choices.Count < need)
        {
            need = MinRemaining<BuyItemInDayRequest>();
            if (choices.Count < need) return;
        }
        int cost = 0;
        for (int i = 0; i < need; i++) cost += choices[i].item.GetDisplayPrice();
        if (cost > economy.Gold || cost > reward * Bot.buyRequest) return;

        for (int i = 0; i < 12 && MinRemaining<BuyItemInDayRequest>() != int.MaxValue && need > 0; i++, need--)
        {
            choices = FillerChoices(inventory);
            if (choices.Count == 0 || !Buy(choices[0], inventory)) return;
        }
        ClaimRequests();
    }

    private List<Choice> FillerChoices(ShopInventory inventory)
    {
        var list = new List<Choice>();
        foreach (List<ItemData> items in new[] { inventory.Fixed, inventory.Rotation })
            foreach (ItemData item in items)
            {
                if (shopContext.Shop.WasBoughtThisShop(item) || item.GetDisplayPrice() > economy.Gold) continue;
                if (!item.CanPurchase(shopContext, out _)) continue;
                Choice choice = Evaluate(item);
                if (choice.item == null)
                {
                    choice = new Choice { item = item, option = (item.GetSelectableOptions(shopContext)?.Length ?? 0) > 0 ? 0 : -1, cell = -1 };
                    if (item.FlowType != ShopFlowType.Instant)
                    {
                        List<int> cells = ValidCells(item);
                        if (cells.Count == 0) continue;
                        choice.cell = cells[0];
                    }
                }
                list.Add(choice);
            }
        list.Sort((a, b) =>
        {
            int byPrice = a.item.GetDisplayPrice().CompareTo(b.item.GetDisplayPrice());
            return byPrice != 0 ? byPrice : string.CompareOrdinal(a.item.PurchaseKey, b.item.PurchaseKey);
        });
        return list;
    }

    // ───── 교배 ─────

    // 형질별 (가졌는가, 우성을 물려줄 확률, 저항력). 짝을 모두 견주기 전에 한 번만 모은다.
    private struct Gene { public bool has; public float p, r; }

    private (PlantState, PlantState) PickBestParents(List<PlantState> plants)
    {
        float[] weights = TraitWeights();
        int traits = weights.Length;
        var genes = new Gene[plants.Count * traits];
        for (int i = 0; i < plants.Count; i++)
            foreach (GeneticTrait t in plants[i].Traits)
                genes[i * traits + (int)t.traitType] = new Gene { has = true, p = t.genetics * 0.5f, r = t.resistance };

        float best = float.NegativeInfinity;
        (PlantState, PlantState) pick = (plants[0], plants[1]);
        for (int i = 0; i < plants.Count; i++)
            for (int j = i + 1; j < plants.Count; j++)
            {
                float score = 0f;
                for (int t = 0; t < traits; t++)
                    if (weights[t] > 0f) score += weights[t] * ChildValue(genes, i * traits, j * traits, t);
                if (score > best) { best = score; pick = (plants[i], plants[j]); }
            }
        return pick;
    }

    // 자식의 저항력 기댓값 + 유전자 값. 가뭄과 더위는 폭우와 추위의 반대 유전자로 정해진다.
    private float ChildValue(Gene[] genes, int a, int b, int trait)
    {
        bool paired = trait == (int)TraitType.Drought || trait == (int)TraitType.Heat;
        int source = trait == (int)TraitType.Drought ? (int)TraitType.HeavyRain : trait == (int)TraitType.Heat ? (int)TraitType.Cold : trait;

        Gene ga = genes[a + source], gb = genes[b + source];
        if (!ga.has && !gb.has) return 0.1f;

        float two = ga.p * gb.p, zero = (1f - ga.p) * (1f - gb.p), one = 1f - two - zero;
        float min = (ga.r + gb.r) * 0.45f;
        if (paired) { (two, zero) = (zero, two); min = 0f; }

        // BreedingRules.RollResistance의 구간
        bool seasonal = source >= (int)TraitType.HeavyRain;
        float value = kind == PlantKind.Peanut
            ? seasonal
                ? zero * Mid(0.2f, 0.4f, min) + one * Mid(0.4f, 0.6f, min) + two * Mid(0.6f, 0.8f, min)
                : (zero + one) * Mid(0.3f, 0.4f, min) + two * Mid(0.4f, 0.8f, min)
            : seasonal
                ? zero * Mid(0.3f, 0.5f, min) + one * Mid(0.5f, 0.7f, min) + two * Mid(0.7f, 0.9f, min)
                : (zero + one) * Mid(0.3f, 0.5f, min) + two * Mid(0.5f, 0.9f, min);
        return value + Bot.genetics * (one + 2f * two);
    }

    private static float Mid(float low, float high, float min) => (Math.Min(high, Math.Max(low, min)) + high) * 0.5f;

    // ───── 밤 ─────

    private void PlannerNight()
    {
        SellForRequests();
        PayTax();
        TakeSpecialItem();
        ClaimRequests();

        ShopInventory inventory = GenerateInventory();
        BuyAll(inventory);
        BuyForRequests(inventory);

        for (int i = 0; i < 3 && (shopContext.Shop.DailyRerollCount > 0 || Budget > Bot.rerollGold); i++)
        {
            if (!shopContext.Shop.UseFreeReroll()) economy.Spend(ShopController.RerollPrice);
            shopContext.Shop.Reroll();
            inventory = GenerateInventory();
            BuyAll(inventory);
            BuyForRequests(inventory);
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

    private int SeedlingReserve
    {
        get
        {
            if (!config.buySeedlings) return 0;
            bool soon = TraitOptions.GetTraitSets(stage + 3, true).Count > TraitOptions.GetTraitSets(stage, true).Count;
            bool missing = FindMissingTraitSet(out int owners) >= 0 && owners < 2;
            return soon || missing ? (int)(SeedlingPrice * Bot.seedlingReserve) : 0;
        }
    }

    private void PayTax()
    {
        if (config.payTax && tax.IsDue(stage))
        {
            while (economy.Gold < tax.DueAmount && field.Board.CountLivingPlants() > 3)
            {
                noSellToday = false; // 못 내면 비싼 식물부터 압류당한다
                if (!SellCheapest(3, float.PositiveInfinity)) break;
            }
            if (tax.CanPay(economy, out int due))
            {
                economy.Spend(due);
                tax.MarkPaid();
            }
        }
        if (stage == EndlessMode.StartStage) tax.SettleAtEnding();
    }

    private int TaxReserve
    {
        get
        {
            if (!config.payTax || tax.DueStage >= EndlessMode.StartStage) return 0;
            int days = tax.DueStage - stage;
            if (days > Bot.taxDays) return 0;
            float plants = 0f;
            foreach (PlantState plant in SortedPlants()) plants += plant.GetSellingPrice();
            return (int)Math.Max(0f, tax.DueAmount - Bot.liquidation * plants);
        }
    }
}
