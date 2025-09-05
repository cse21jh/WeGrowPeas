using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameRecordHolder
{
    public static int maxStageReached { get; private set; }
    public static int TotalPeas { get; private set; }
    public static int TotalPeanuts { get; private set; }
    public static int soldPeas { get; private set; }
    public static int soldPeanuts { get; private set; }
    public static int TotalBugsKilled { get; private set; }
    public static int totalGoldEarned { get; private set; }
    public static int totalGoldSpend { get; private set; }
    public static int[] TotalWaveKilled { get; private set; }
    
    //가장 많이 산 아이템

    //최다 판매 식물

    //기타 로그들
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
        if (TotalPeas < 120)
        {
            PlayerRank = "B";
            return;
        }
        else if (TotalPeas < 200)
        {
            PlayerRank = "A";
            return;
        }
        else if (TotalPeas < 280)
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