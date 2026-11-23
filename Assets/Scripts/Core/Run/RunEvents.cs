using System;

/// <summary>한 판 안에서 일어난 일. 의뢰가 진행도를 세는 데 쓴다. 게임은 GameEvents를 여기로 잇고, 시뮬레이터는 직접 부른다.</summary>
public sealed class RunEvents
{
    public event Action BugKilled;
    public event Action Breeded;
    public event Action PlantDied;
    public event Action PlantDiedByBug;
    public event Action PlantMoved;
    public event Action QuestDayPassed;
    public event Action<ItemData> ShopBought;
    public event Action<PlantState> PlantSold;
    public event Action<int> DayEndedWithRemainingBreeds;

    public void RaiseBugKilled() => BugKilled?.Invoke();
    public void RaiseBreeded() => Breeded?.Invoke();
    public void RaisePlantDied() => PlantDied?.Invoke();
    public void RaisePlantDiedByBug() => PlantDiedByBug?.Invoke();
    public void RaisePlantMoved() => PlantMoved?.Invoke();
    public void RaiseQuestDayPassed() => QuestDayPassed?.Invoke();
    public void RaiseShopBought(ItemData item) => ShopBought?.Invoke(item);
    public void RaisePlantSold(PlantState plant) => PlantSold?.Invoke(plant);
    public void RaiseDayEndedWithRemainingBreeds(int remaining) => DayEndedWithRemainingBreeds?.Invoke(remaining);
}
