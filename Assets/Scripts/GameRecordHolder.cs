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
    public static string MostKilledWave { get; private set; }
    public static string PopularItemName { get; private set; }
    public static string MostSellPlantName { get; private set; }

    //±âÅ¸ ·Î±×µé
    public static int PlayerRank { get; private set; }

    

    public static void SaveRecord(int stage, int peas, int peanuts, int speas, int speanuts, int bugs, int egold, int sgold, string wName, string iName)
    {
        maxStageReached = GameStartContext.StartType == GameStartType.GameOver ? stage - 1 : stage;
        TotalPeas = peas + 2;
        TotalPeanuts = peanuts;
        soldPeas = speas;
        soldPeanuts = speanuts;
        TotalBugsKilled = bugs;
        totalGoldEarned = egold;
        totalGoldSpend = sgold;
        MostKilledWave = wName;
        PopularItemName = iName;

        CalculateRank();
        CalculateMSP();
    }

    private static void CalculateRank()
    {
        if (TotalPeas < 80)
        {
            PlayerRank = 0;
            return;
        }
        else if (TotalPeas < 120)
        {
            PlayerRank = 1;
            return;
        }
        else if (TotalPeas < 160)
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

    private static void CalculateMSP()
    {
        MostSellPlantName = (soldPeas >= soldPeanuts) ? "¿ÏµÎÄá" : "¶¥Äá";
    }
}