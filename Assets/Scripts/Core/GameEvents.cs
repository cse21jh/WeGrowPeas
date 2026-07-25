using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnSaveGameRequested;

    public static void RequestSaveGame()
    {
        OnSaveGameRequested?.Invoke();
    }

    public static event Action OnBugKilled;
    public static void RaiseBugKilled () => OnBugKilled?.Invoke();

    public static event Action OnPeaBreeded;
    public static void RaisePeaBreeded() => OnPeaBreeded?.Invoke();

    public static event Action OnPeaDied;
    public static void RaisePeaDied() => OnPeaDied?.Invoke();

    public static event Action<ItemData> OnShopBought;
    public static void RaiseShopBought(ItemData item) => OnShopBought?.Invoke(item);

    public static event Action OnDayPassedForRequest; //NoSellPea & remaining day check
    public static void RaiseDayPassedForRequest() => OnDayPassedForRequest?.Invoke();

    public static event Action<Plant> OnPeaSold;
    public static void RaisePeaSold(Plant p) => OnPeaSold?.Invoke(p);

    public static event Action OnPlantMoved;
    public static void RaisePlantMoved() => OnPlantMoved?.Invoke();

    public static event Action<int> OnDayEndedWithRemainingBreeds;
    public static void RaiseDayEndedWithRemainingBreeds(int remainingBreeds) => OnDayEndedWithRemainingBreeds?.Invoke(remainingBreeds);

    public static event Action OnPeaDiedByBug;
    public static void RaisePeaDiedByBug() => OnPeaDiedByBug?.Invoke();

    /// <summary>
    /// 모든 구독을 강제로 끊는다. ⚠ 평상시 호출 금지.
    ///
    /// 구독자들은 OnEnable에서 구독하고 OnDisable에서 해제하므로(현재 모든 구독/해제가 1:1 대응)
    /// 별도 청소가 필요 없다. Unity는 (모든 Awake) → (모든 OnEnable) → (모든 Start) 순으로 돌기 때문에,
    /// Start나 그 이후에 이걸 호출하면 이미 걸린 정상 구독까지 전부 지워진다.
    /// (실제로 GameManager.Start에서 호출해 CurseManager의 저주 해제 구독이 날아가,
    ///  단발형 저주가 하루가 지나도 해제되지 않는 버그가 있었다.)
    /// </summary>
    public static void Reset()
    {
        OnSaveGameRequested = null;
        OnBugKilled = null;
        OnPeaBreeded = null;
        OnPeaDied = null;
        OnShopBought = null;
        OnDayPassedForRequest = null;
        OnPeaSold = null;
        OnPlantMoved = null;
        OnDayEndedWithRemainingBreeds = null;
        OnPeaDiedByBug = null;
    }
}
