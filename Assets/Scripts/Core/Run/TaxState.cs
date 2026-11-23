using System.Collections.Generic;

/// <summary>세금 납부 상태와 규칙. 금액과 일정은 <see cref="TaxSchedule"/>이 정한다.</summary>
public sealed class TaxState
{
    /// <summary>이 날의 세금은 낼 수 없다. 엔딩 때 낸 것으로 처리한다.</summary>
    private const int EndingTaxStage = 40;

    /// <summary>이미 낸 가장 높은 세금일. (저장 대상)</summary>
    public int LastPaidStage { get; set; }

    /// <summary>다음에 낼 세금일.</summary>
    public int DueStage => LastPaidStage + TaxSchedule.Interval;
    public int DueAmount => TaxSchedule.GetTaxAmount(DueStage);
    public int NextAmount => TaxSchedule.GetTaxAmount(DueStage + TaxSchedule.Interval);

    public bool IsDue(int stage) => stage >= DueStage;
    /// <summary>마감 밤이 지났는데 안 냈는가.</summary>
    public bool IsOverdue(int stage) => stage > DueStage;

    public void MarkPaid() => LastPaidStage = DueStage;

    /// <summary>지금 낼 수 있으면 낼 금액을 준다. 골드 차감과 <see cref="MarkPaid"/>는 호출한 쪽이 한다.</summary>
    public bool CanPay(EconomyState economy, out int amount)
    {
        amount = DueAmount;
        return DueStage != EndingTaxStage && economy.HasGold(amount);
    }

    /// <summary>엔딩: 마지막 세금을 걷지 않고 낸 것으로 친다.</summary>
    public void SettleAtEnding()
    {
        if (DueStage == EndingTaxStage) MarkPaid();
    }

    /// <summary>
    /// 빚을 갚으려면 압류할 완두·땅콩. 비싼 것부터, 값이 같으면 앞 칸부터 빚을 채울 때까지 고른다.
    /// 강제 매각도 이 순서로 하고, 골드가 0 이상이 되면 멈춘다.
    /// </summary>
    public static List<PlantState> PickSeizure(PlantBoard board, int debt)
    {
        var priced = new List<(PlantState plant, int price)>();
        foreach (PlantState plant in board.StatesInOrder())
            if (plant.IsMovableKind) priced.Add((plant, plant.GetSellingPrice()));

        priced.Sort((a, b) =>
        {
            int byPrice = b.price.CompareTo(a.price);
            return byPrice != 0 ? byPrice : a.plant.GridIndex.CompareTo(b.plant.GridIndex);
        });

        var seized = new List<PlantState>();
        int sum = 0;
        foreach ((PlantState plant, int price) in priced)
        {
            if (sum >= debt) break;
            seized.Add(plant);
            sum += price;
        }
        return seized;
    }
}
