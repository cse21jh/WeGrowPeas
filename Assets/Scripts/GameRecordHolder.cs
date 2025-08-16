using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameRecordHolder
{
    public static int maxStageReached { get; private set; }
    public static int TotalPeas { get; private set; }
    public static int TotalBugsKilled { get; private set; }

    public static int[] TotalWaveKilled { get; private set; }

    public static string PlayerRank { get; private set; }

    public static void SaveRecord(int stage, int peas, int bugs, int[] killed)
    {
        maxStageReached = stage - 1;
        TotalPeas = peas;
        TotalBugsKilled = bugs;
        TotalWaveKilled = (int[])killed.Clone();

        CalculateRank();
    }

    private static void CalculateRank()
    {
        if (TotalPeas < 144)
        {
            PlayerRank = "B";
            return;
        }
        else if (TotalPeas < 240)
        {
            PlayerRank = "A";
            return;
        }
        else if (TotalPeas < 336)
        {
            PlayerRank = "S";
            return;
        }
        else
        {
            PlayerRank = "SS";
            return;
        }
    }
}