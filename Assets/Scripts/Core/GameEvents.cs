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
}
