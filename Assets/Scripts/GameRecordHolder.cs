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
    public static string PopularItemName {  get; private set; }

    //최다 판매 식물

    //기타 로그들
    public static int PlayerRank { get; private set; }

    

    public static void SaveRecord(int stage, int peas, int peanuts, int speas, int speanuts, int bugs, int egold, int sgold, int[] killed, string iName)
    {
        maxStageReached = stage - 1;
        TotalPeas = peas;
        TotalPeanuts = peanuts;
        soldPeas = speas;
        soldPeanuts = speanuts;
        TotalBugsKilled = bugs;
        totalGoldEarned = egold;
        totalGoldSpend = sgold;
        TotalWaveKilled = (int[])killed.Clone();
        PopularItemName = iName;

        CalculateRank();
    }

    private static void CalculateRank()
    {
        if (TotalPeas < 120)
        {
            PlayerRank = 0;
            return;
        }
        else if (TotalPeas < 200)
        {
            PlayerRank = 1;
            return;
        }
        else if (TotalPeas < 280)
        {
            PlayerRank = 2;
            return;
        }
        else
        {
            PlayerRank = 3;
            return;
        }
    }
}