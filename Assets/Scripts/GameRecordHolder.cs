using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 한 판(런)의 결과 요약. 결과 화면과 회상 기록이 같은 값을 보도록 직렬화 가능한 형태로 둔다.
/// 회상 파일(<see cref="RecallRunFile"/>)에 그대로 실린다.
/// </summary>
[Serializable]
public class RunSummary
{
    /// <summary>버틴 일수. 게임오버는 죽은 날의 전날(stage - 1)까지 친다.</summary>
    public int maxStageReached;

    public int totalPeas;
    public int totalPeanuts;
    public int soldPeas;
    public int soldPeanuts;
    public int totalBugsKilled;
    public int totalGoldEarned;
    public int totalGoldSpend;
    public string mostKilledWave;
    public string popularItemName;
    public string mostSellPlantName;
    public int totalGenetics;
    public int mostExpensivePlant;
    public int completeRequestCount;
    public int playerRank;
}

/// <summary>
/// 이번 런의 결과 요약을 씬 전환(농장 → GameOverScene) 너머로 들고 있는다.
/// 값은 <see cref="Current"/> 하나에 모여 있고, 아래 프로퍼티는 기존 호출부를 위한 창구다.
/// </summary>
public static class GameRecordHolder
{
    /// <summary>이번 런의 요약. 회상 기록으로 확정될 때 이 객체가 그대로 파일에 실린다.</summary>
    public static RunSummary Current { get; private set; } = new RunSummary();

    public static int maxStageReached => Current.maxStageReached;
    public static int TotalPeas => Current.totalPeas;
    public static int TotalPeanuts => Current.totalPeanuts;
    public static int soldPeas => Current.soldPeas;
    public static int soldPeanuts => Current.soldPeanuts;
    public static int TotalBugsKilled => Current.totalBugsKilled;
    public static int totalGoldEarned => Current.totalGoldEarned;
    public static int totalGoldSpend => Current.totalGoldSpend;
    public static string MostKilledWave => Current.mostKilledWave;
    public static string PopularItemName => Current.popularItemName;
    public static string MostSellPlantName => Current.mostSellPlantName;
    public static int totalGenetics => Current.totalGenetics;
    public static int MostExpensivePlant => Current.mostExpensivePlant;
    public static int CompleteRequestCount => Current.completeRequestCount;

    //기타 로그들
    public static int PlayerRank => Current.playerRank;

    public static void SaveRecord(int stage, int peas, int peanuts, int speas, int speanuts, int bugs, int egold, int sgold, string wName, string iName, int mPlant, int rCount)
    {
        Current = new RunSummary();

        Current.maxStageReached = GameStartContext.StartType == GameStartType.GameOver ? stage - 1 : stage;
        Current.totalPeas = peas + 2;
        Current.totalPeanuts = peanuts;
        Current.soldPeas = speas;
        Current.soldPeanuts = speanuts;
        Current.totalBugsKilled = bugs;
        Current.totalGoldEarned = egold;
        Current.totalGoldSpend = sgold;
        Current.mostKilledWave = wName;
        Current.popularItemName = iName;
        Current.mostExpensivePlant = mPlant;
        Current.completeRequestCount = rCount;

        CalculateRank();
        CalculateMSP();
    }

    public static void SaveGenetics(int g)
    {
        Current.totalGenetics = g;
    }

    private static void CalculateRank()
    {
        if (TotalPeas < 80)
        {
            Current.playerRank = 0;
            return;
        }
        else if (TotalPeas < 120)
        {
            Current.playerRank = 1;
            return;
        }
        else if (TotalPeas < 160)
        {
            Current.playerRank = 2;
            return;
        }
        else
        {
            Current.playerRank = 3;
            return;
        }
    }

    private static void CalculateMSP()
    {
        Current.mostSellPlantName = (soldPeas >= soldPeanuts) ? "완두콩" : "땅콩";
    }
}
