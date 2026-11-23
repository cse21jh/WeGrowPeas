using System;
using System.Collections.Generic;

public class ShopInventory
{
    public List<ItemData> Fixed = new();
    public List<ItemData> Rotation = new();
}

/// <summary>한 판의 상점 상태. 구매 이력, 진열 시드, 리롤, 신용카드 집계. 판마다 새로 만들어진다.</summary>
public sealed class ShopState
{
    // 코드가 직접 찾는 아이템의 구매 이력 키. ItemData.PurchaseKey와 같은 값(에셋 이름)이다.
    public const string ShopContactKey = "ShopContactItemData";
    /// <summary>신용카드: 하루에 이만큼 사야 환급된다.</summary>
    public const int CreditCardRequiredPurchaseCount = 3;

    private readonly HashSet<ItemData> boughtThisShop = new HashSet<ItemData>();
    private int uniqueSeed = -1;
    private int rerollCount;

    /// <summary>에셋 이름 → 구매 횟수.</summary>
    public Dictionary<string, int> PurchaseHistory { get; } = new Dictionary<string, int>();
    /// <summary>(일차 * 1000 + 리롤 횟수) → 진열 시드.</summary>
    public Dictionary<int, int> Seeds { get; } = new Dictionary<int, int>();
    public int DailyRerollCount { get; private set; }
    /// <summary>확률 증가 이벤트: 오늘은 모든 아이템의 가중치가 같다.</summary>
    public bool IsProbabilityEqualized { get; set; }
    public int DailyPurchaseCount { get; private set; }
    public int DailySpentGold { get; private set; }

    /// <summary>진열과 의뢰가 쓰는 이번 판의 시드. 처음 부를 때 정해진다.</summary>
    public int UniqueSeed
    {
        get
        {
            if (uniqueSeed == -1) uniqueSeed = GameRun.Rng.Item.Range(0, int.MaxValue);
            return uniqueSeed;
        }
    }

    public int GetItemPurchaseCount(ItemData data)
        => data != null && PurchaseHistory.TryGetValue(data.PurchaseKey, out int count) ? count : 0;

    // ───── 진열 ─────

    private int GetSeed(int day)
    {
        int key = day * 1000 + rerollCount;
        if (!Seeds.TryGetValue(key, out int seed))
            Seeds[key] = seed = unchecked(UniqueSeed + day * 12345 + rerollCount * 67890);
        return seed;
    }

    /// <param name="isUnlocked">계정 진행으로 해금된 아이템인가. 게임은 프로필을, 시뮬레이터는 설정을 본다.</param>
    public ShopInventory Generate(IEnumerable<ItemData> fixedItems, IEnumerable<ItemData> rotationPool, int rotationCount,
        ShopContext ctx, int day, Func<ItemData, bool> isUnlocked)
    {
        var inventory = new ShopInventory();

        foreach (ItemData item in fixedItems)
        {
            if (item == null || !item.CanPurchaseByLimit()) continue;
            inventory.Fixed.Add(item);
            item.InitializePrice(ctx);
        }

        var candidates = new List<ItemData>();
        foreach (ItemData item in rotationPool)
        {
            if (item == null || !isUnlocked(item)) continue;
            if (!item.IsRotationUnlockOk(ctx) || item.GetRotationWeight(ctx) <= 0 || !item.CanPurchaseByLimit()) continue;
            candidates.Add(item);
        }

        // 추첨은 목록 순서를 따르는데 에셋을 읽어 온 순서는 때마다 다를 수 있다
        candidates.Sort((a, b) => string.CompareOrdinal(a.PurchaseKey, b.PurchaseKey));

        inventory.Rotation = Game.Util.WeightedRandom.PickWithoutReplacement(
            candidates,
            // 확률 증가 이벤트 중에는 뜰 수 있는 아이템의 가중치가 모두 8이다
            item => IsProbabilityEqualized ? 8 : item.GetRotationWeight(ctx),
            new Random(GetSeed(day)),
            rotationCount);

        return inventory;
    }

    // ───── 구매 ─────

    /// <summary>이번 상점에서 이미 사서 더 살 수 없는가.</summary>
    public bool WasBoughtThisShop(ItemData data)
        => data != null && data.OnePerShopIfNotStackable && !data.IsStackable && boughtThisShop.Contains(data);

    /// <summary>결제 직후, 효과를 적용하기 전에 부른다.</summary>
    public void RecordPurchase(ItemData data, int price)
    {
        DailyPurchaseCount++;
        DailySpentGold += price;
        PurchaseHistory[data.PurchaseKey] = GetItemPurchaseCount(data) + 1;
    }

    /// <summary>구매가 끝난 뒤. 더 살 수 없게 됐으면 진열에서 뺀다.</summary>
    public void FinishPurchase(ItemData data, ShopInventory inventory)
    {
        if (!data.IsStackable) boughtThisShop.Add(data);
        if (inventory == null || (data.CanPurchaseByLimit() && !WasBoughtThisShop(data))) return;
        inventory.Fixed.Remove(data);
        inventory.Rotation.Remove(data);
    }

    /// <summary>신용카드: 3번 이상 산 날이면 쓴 골드의 일부를 돌려준다. 다음 날 낮이 시작될 때 한 번 부른다.</summary>
    public int ConsumeDailyPurchaseRefund(float refundPercent)
    {
        int refund = 0;
        if (refundPercent > 0f && DailyPurchaseCount >= CreditCardRequiredPurchaseCount)
            refund = (int)Math.Round(DailySpentGold * refundPercent);

        DailyPurchaseCount = 0;
        DailySpentGold = 0;
        return refund;
    }

    // ───── 리롤 ─────

    public void AddDailyRerolls(int count) => DailyRerollCount += count;

    public bool UseFreeReroll()
    {
        if (DailyRerollCount <= 0) return false;
        DailyRerollCount--;
        return true;
    }

    /// <summary>리롤: 다음 진열은 다른 시드로 뽑고, 이번 상점의 1회 제한을 푼다.</summary>
    public void Reroll()
    {
        rerollCount++;
        boughtThisShop.Clear();
    }

    /// <summary>날이 바뀔 때.</summary>
    public void StartDay()
    {
        rerollCount = 0;
        IsProbabilityEqualized = false;
        boughtThisShop.Clear();
        // 상점 연락처: 산 횟수만큼 매일 무료 리롤
        DailyRerollCount = PurchaseHistory.TryGetValue(ShopContactKey, out int contacts) ? contacts : 0;
    }

    // ───── 저장 ─────

    public void Save(ShopSave save)
    {
        save.itemName.Clear();
        save.itemPurchaseCount.Clear();
        foreach (var kv in PurchaseHistory)
        {
            save.itemName.Add(kv.Key);
            save.itemPurchaseCount.Add(kv.Value);
        }

        save.shopSeedDays.Clear();
        save.shopSeeds.Clear();
        foreach (var kv in Seeds)
        {
            save.shopSeedDays.Add(kv.Key);
            save.shopSeeds.Add(kv.Value);
        }
        save.gameUniqueShopSeed = uniqueSeed;
    }

    public void Load(ShopSave save)
    {
        for (int i = 0; i < save.itemName.Count; i++)
            PurchaseHistory[save.itemName[i]] = save.itemPurchaseCount[i];

        uniqueSeed = save.gameUniqueShopSeed; // 예전 세이브에 없으면(-1) 처음 쓸 때 정해진다

        Seeds.Clear();
        if (save.shopSeedDays == null || save.shopSeeds == null) return;
        for (int i = 0; i < save.shopSeedDays.Count && i < save.shopSeeds.Count; i++)
            Seeds[save.shopSeedDays[i]] = save.shopSeeds[i];
    }
}
