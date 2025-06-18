using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameRecordHolder
{
    public static int maxStageReached { get; private set; }
    public static int TotalPeas { get; private set; }
    public static int TotalBugsKilled { get; private set; }

    public static void SaveData(int stage, int peas, int bugs)
    {
        maxStageReached = stage;
        TotalPeas = peas;
        TotalBugsKilled = bugs;
    }
}