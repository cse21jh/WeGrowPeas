using UnityEngine;

/// <summary>
/// 런 요약(<see cref="RunSummary"/>)을 결과 화면 문구로 바꾼다.
///
/// 결과 화면과 회상이 같은 문구를 쓰도록 표시 로직만 여기 모았다.
/// 이 클래스는 값을 읽기만 하며 게임 상태를 바꾸지 않는다 —
/// 유일한 예외인 <see cref="AwardGenetics"/>는 런이 끝나는 지점에서만 불린다.
/// </summary>
public static class RunRecordFormatter
{
    private static readonly string[] EndingTextByRank =
    {
        "살아남는 데는 성공했지만, 식량 확보가 보장되지 않는다고 판단되어 다른 대안을 탐색하러 떠났다….",
        "안정적인 환경에서 생산이 빠른 좋은 식량으로 평가받아 환경적인 변화가 크게 없는 일부 지역에서 쓰이게 되었다.",
        "다양한 환경에서 괜찮은 생산량을 보여 주었기에 비상시에 사용될 대체식품으로 각광받으며 좋은 먹거리가 되었다.",
        "뛰어난 적응성과 번식 속도를 입증해 전세계에 확산되었고, 이후 인류의 핵심적인 식량이 되었다!"
    };

    /// <summary>등급을 표에 넣을 수 있는 범위로 자른다.</summary>
    private static int ClampRank(int rank) => Mathf.Clamp(rank, 0, EndingTextByRank.Length - 1);

    /// <summary>등급별 완두콩 표정 스프라이트 인덱스. 등급이 높을수록 앞쪽 그림.</summary>
    public static int GetPeaEmotionSpriteIndex(int rank) => (EndingTextByRank.Length - 1) - ClampRank(rank);

    /// <summary>등급별 유전자 토큰 배율.</summary>
    public static float GetGeneticsMultiplier(int rank)
    {
        switch (ClampRank(rank))
        {
            case 0: return 0.75f;
            case 3: return 1.25f;
            default: return 1.0f;
        }
    }

    // ── 문구 ──────────────────────────────────────────────────────────────────

    public static string BuildEndingText(RunSummary s)
        => $"우리는 {s.maxStageReached}일간 {EndingTextByRank[ClampRank(s.playerRank)]}";

    public static string BuildDaysLine(RunSummary s)
        => $"총 \"{s.maxStageReached}\"일을 버텼다!";

    public static string BuildStatsLine(RunSummary s)
    {
        return $"\"{s.totalPeas}\"개의 식물 중 \"{s.soldPeas}\"개를 판매했다.\n" +
               $"벌레는 \"{s.totalBugsKilled}\"마리 잡았다.\n" +
               $"총 \"{s.totalGoldEarned}\"골드를 벌었다!\n" +
               $"상점에서 \"{s.totalGoldSpend}\"골드를 소모했다.\n" +
               $"가장 비싸게 판 식물은 \"{s.mostExpensivePlant}\"골드였다.\n" +
               $"사람들을 \"{s.completeRequestCount}\"번 도와줬다!.\n" +
               $"총 \"{s.totalGenetics}\"개의 유전자 토큰을 획득했다.";
    }

    public static string BuildFarmNoteLine(RunSummary s)
    {
        if (string.IsNullOrEmpty(s.popularItemName))
        {
            return $"우리 농장은 {s.mostKilledWave}에 취약했다….\n" +
                   $"사람들은 {s.mostSellPlantName}을 가장 좋아하는 듯하다.";
        }

        return $"우리 농장은 {s.mostKilledWave}에 취약했다…\n" +
               $"상점에서 {s.popularItemName}을/를 애용했다.\n" +
               $"사람들은 {s.mostSellPlantName}을 가장 좋아하는 듯하다.";
    }

    // ── 지급 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 이번 런의 유전자 토큰을 지급한다. 런이 끝나는 지점에서 딱 한 번만 부른다.
    ///
    /// 결과 화면이 아니라 <see cref="GameManager"/>에서 부르는 이유:
    /// 결과 화면 UI가 켜질 때마다 지급하면 회상으로 같은 화면을 다시 볼 때 또 지급된다.
    /// 지급 결과(<see cref="RunSummary.totalGenetics"/>)는 회상 기록에도 그대로 남는다.
    /// </summary>
    public static void AwardGenetics(RunSummary s)
    {
        if (AbilityManager.Instance == null)
        {
            Debug.Log("Ability Manager가 없습니다");
            return;
        }

        int g = s.totalGoldEarned / 500;
        AbilityManager.Instance.AddGeneStorage((int)(g * GetGeneticsMultiplier(s.playerRank)));
        AbilityManager.Instance.AddGenetics(); // 내부에서 GameRecordHolder.SaveGenetics 호출
    }
}
