/// <summary>골드와 판매 집계. 화면 표시와 연출은 <see cref="EconomyManager"/>가 맡는다.</summary>
public sealed class EconomyState
{
    public EconomyState(int startGold)
    {
        Gold = startGold;
    }

    /// <summary>빚이 있으면 음수다.</summary>
    public int Gold { get; private set; }
    public int TotalGold { get; private set; }
    public int ConsumeGold { get; private set; }
    /// <summary>오늘 번 골드.</summary>
    public int EarnedToday { get; private set; }
    public int PeaSellCount { get; private set; }
    public int PeanutSellCount { get; private set; }

    public bool HasGold(int amount) => Gold >= amount;

    public void Add(int amount)
    {
        Gold += amount;
        TotalGold += amount;
        EarnedToday += amount;
    }

    public void Spend(int amount)
    {
        Gold -= amount;
        ConsumeGold += amount;
    }

    public void CountSale(PlantKind kind)
    {
        if (kind == PlantKind.Pea) PeaSellCount++;
        else if (kind == PlantKind.Peanut) PeanutSellCount++;
    }

    /// <summary>오늘 번 골드를 돌려주고 0으로 되돌린다.</summary>
    public int TakeEarnedToday()
    {
        int earned = EarnedToday;
        EarnedToday = 0;
        return earned;
    }

    /// <summary>새 게임: 집계만 비운다. 보유 골드는 특성이 먼저 더해 놓았을 수 있어 그대로 둔다.</summary>
    public void ResetCounters()
    {
        PeaSellCount = 0;
        PeanutSellCount = 0;
        TotalGold = 0;
        ConsumeGold = 0;
        EarnedToday = 0;
    }

    public void Save(EconomySave save)
    {
        save.gold = Gold;
        save.sellCount[0] = PeaSellCount;
        save.sellCount[1] = PeanutSellCount;
        save.totalGold = TotalGold;
        save.consumeGold = ConsumeGold;
    }

    public void Load(EconomySave save)
    {
        Gold = save.gold;
        PeaSellCount = save.sellCount[0];
        PeanutSellCount = save.sellCount[1];
        TotalGold = save.totalGold;
        ConsumeGold = save.consumeGold;
    }
}
