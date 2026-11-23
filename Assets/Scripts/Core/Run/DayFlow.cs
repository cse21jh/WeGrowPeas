using System.Collections.Generic;

/// <summary>하루가 멈춰서 기다리는 곳. 게임은 연출과 입력을, 시뮬레이터는 봇의 행동을 한다.</summary>
public enum DayStep
{
    /// <summary>저주가 발동한 직후.</summary>
    DayStarted,
    /// <summary>세금이 밀렸다. 걷어야 한다.</summary>
    TaxOverdue,
    /// <summary>낮의 자유시간(교배, 판매, 이동, 벌레).</summary>
    FreeTime,
    /// <summary>웨이브 판정 직전.</summary>
    BeforeWave,
    /// <summary>웨이브 판정이 끝나고 다음 웨이브가 정해졌다.</summary>
    WaveResolved,
    /// <summary>웨이브 뒤의 일이 끝나고 밤이 되기 전.</summary>
    Evening,
    /// <summary>밤(휴대전화).</summary>
    Night,
    /// <summary>하루가 끝났다. 일차는 아직 오르지 않았다.</summary>
    DayEnded,
}

/// <summary>하루 진행이 쓰는 것들.</summary>
public interface IDayWorld
{
    FieldState Field { get; }
    WaveState Waves { get; }
    ModState Mods { get; }
    CurseBoard Curses { get; }
    RequestBoard Requests { get; }
    TaxState Tax { get; }
    IFieldActions Actions { get; }

    bool CursesActive { get; }
    bool RequestsActive { get; }
    bool SpecialItemsActive { get; }
    /// <summary>방금 끝난 낮에 쓰지 않은 교배 횟수.</summary>
    int RemainingBreeds { get; }

    void StartRequests();
    void AddRefund(int gold);
    void CountFreeTime(PlantState plant);
    void RaiseDayEndedWithRemainingBreeds(int remaining);
    void RaiseQuestDayPassed();
}

/// <summary>하루의 순서. 게임과 시뮬레이터가 함께 쓴다. 순서가 난수에 영향을 주므로 함부로 옮기지 않는다.</summary>
public sealed class DayFlow
{
    private readonly IDayWorld world;

    public DayFlow(IDayWorld world) => this.world = world;

    private static int Stage => GameRun.Current.Stage;

    /// <summary>하루를 진행한다. 값을 돌려줄 때마다 부르는 쪽이 그 단계의 일을 하고 이어서 부른다.</summary>
    public IEnumerable<DayStep> Run()
    {
        if (world.CursesActive) world.Curses.Apply(); // 전날 밤 정해진 저주
        yield return DayStep.DayStarted;

        if (world.Tax.IsOverdue(Stage)) yield return DayStep.TaxOverdue;

        if (world.RequestsActive && world.Requests.IsCycleStart(Stage)) world.StartRequests();
        StartFreeTime();
        yield return DayStep.FreeTime;
        EndFreeTime();

        yield return DayStep.BeforeWave;
        ResolveWave();
        world.Waves.Advance(Stage);
        yield return DayStep.WaveResolved;

        DayRules.AfterWave(world.Field, world.Actions);
        world.Curses.OnDayPassed();
        yield return DayStep.Evening;

        if (world.CursesActive) world.Curses.Select(Stage); // 내일 걸릴 저주
        if (world.SpecialItemsActive && IsGiftNight) SpecialItemSystem.AddGift();
        yield return DayStep.Night;

        PassQuestDay();
        foreach (PlantState plant in world.Field.Board.States.Values)
            if (!plant.IsDying) plant.DaysOnField++;
        yield return DayStep.DayEnded;
    }

    /// <summary>특수 아이템: 엔딩 전 10·20·30일 밤에 선물이 온다.</summary>
    public static bool IsGiftNight => Stage % 10 == 0 && Stage < EndlessMode.StartStage;

    /// <summary>일차를 올린다.</summary>
    /// <returns>기간이 끝난 보정.</returns>
    public List<Mod> NextDay()
    {
        GameRun.Current.Stage++;
        List<Mod> expired = world.Mods.ExpireOn(Stage);
        world.Waves.Deck.Unlock(Stage);
        GameRun.Current.Shop.StartDay();
        return expired;
    }

    // ───── 하루의 조각. 튜토리얼은 정해진 순서가 따로 있어 필요한 것만 부른다. ─────

    public void StartFreeTime()
    {
        // 신용카드: 어제 쓴 골드의 일부를 돌려받는다
        world.AddRefund(GameRun.Current.Shop.ConsumeDailyPurchaseRefund(world.Field.Modifiers.CreditCardRefundPercent));
        // 특수(세계여행): 낮 동안 옮긴 거리를 잴 기준
        foreach (PlantState plant in world.Field.Board.States.Values) plant.DayStartGridIndex = plant.GridIndex;
    }

    public void EndFreeTime()
    {
        world.RaiseDayEndedWithRemainingBreeds(world.RemainingBreeds);
        DayRules.EndFreeTime(world.Field, world.CountFreeTime);
    }

    public void ResolveWave()
        => DayRules.ResolveWave(world.Field, world.Waves.Current, () => world.Waves.GetSecondToday(Stage), world.Actions);

    public void PassQuestDay()
    {
        world.Requests.DayPassed++;
        world.RaiseQuestDayPassed();
    }
}
