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

    public static event Action OnDayPassedForRequest; //NoSellPea Àü¿ë
    public static void RaiseDayPassedForRequest() => OnDayPassedForRequest?.Invoke();

    public static event Action OnPeaSold;
    public static void RaisePeaSold() => OnPeaSold?.Invoke();
}
