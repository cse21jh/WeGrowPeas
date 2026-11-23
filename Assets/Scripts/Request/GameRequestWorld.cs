/// <summary>게임의 사건(GameEvents)을 의뢰가 듣는 사건으로 잇는다.</summary>
public sealed class GameRequestWorld : IRequestWorld
{
    public RunEvents Events { get; } = new RunEvents();

    public int PickTraitFromWaves() => GameManager.Instance.enemyController.PickTraitFromUnlockWave();

    public bool IsFreeTime => PhoneManager.Instance != null && PhoneManager.Instance.IsPhonePhase;

    public void Connect()
    {
        GameEvents.OnBugKilled += Events.RaiseBugKilled;
        GameEvents.OnPeaBreeded += Events.RaiseBreeded;
        GameEvents.OnPeaDied += Events.RaisePlantDied;
        GameEvents.OnPeaDiedByBug += Events.RaisePlantDiedByBug;
        GameEvents.OnPlantMoved += Events.RaisePlantMoved;
        GameEvents.OnQuestDayPassed += Events.RaiseQuestDayPassed;
        GameEvents.OnShopBought += Events.RaiseShopBought;
        GameEvents.OnPeaSold += RaiseSold;
        GameEvents.OnDayEndedWithRemainingBreeds += Events.RaiseDayEndedWithRemainingBreeds;
    }

    public void Disconnect()
    {
        GameEvents.OnBugKilled -= Events.RaiseBugKilled;
        GameEvents.OnPeaBreeded -= Events.RaiseBreeded;
        GameEvents.OnPeaDied -= Events.RaisePlantDied;
        GameEvents.OnPeaDiedByBug -= Events.RaisePlantDiedByBug;
        GameEvents.OnPlantMoved -= Events.RaisePlantMoved;
        GameEvents.OnQuestDayPassed -= Events.RaiseQuestDayPassed;
        GameEvents.OnShopBought -= Events.RaiseShopBought;
        GameEvents.OnPeaSold -= RaiseSold;
        GameEvents.OnDayEndedWithRemainingBreeds -= Events.RaiseDayEndedWithRemainingBreeds;
    }

    private void RaiseSold(Plant plant) => Events.RaisePlantSold(plant.State);
}
