using UnityEngine;

/// <summary>
/// 현재 활성 저주가 각 시스템에 노출하는 런타임 수정자(중앙 접근자).
/// - 각 Curse 인스턴스의 Activate()가 자기 필드를 set, Deactivate()가 기본값으로 clear.
/// - 게임 시스템들은 여기 값을 read해서 효과를 반영 (DawnSystem.Current와 동일한 패턴).
/// - 지속형은 하나, 단발형은 하나만 동시에 활성. 새 게임 시 <see cref="ResetAll"/>로 초기화.
/// </summary>
public static class CurseState
{
    // ───── 지속형(seasonal) ─────
    /// <summary>203 방사능: 매일 모든 저항력 추가 감소 %p.</summary>
    public static float RadiationDecayPercent;
    /// <summary>202 돌연변이: 교배 변종 발생 확률 +%p.</summary>
    public static float MutationAddPercent;
    /// <summary>204 꽃가루 실종: 매 턴 교배 불가로 바뀌는 필드 식물 비율(0~1).</summary>
    public static float PollenLostRatio;
    /// <summary>205 독점시장: 상점 가격 배율 하한/상한(예: 0.8~1.2). 비활성 시 1.</summary>
    public static float ShopPriceMinMul = 1f;
    public static float ShopPriceMaxMul = 1f;
    public static bool ShopMonopoly;
    /// <summary>206 불면증: 자유시간 배율(0~1, 1=영향 없음).</summary>
    public static float InsomniaFreeTimeRatio = 1f;
    /// <summary>207 씨 없는 수박: 교배 실패 확률 %.</summary>
    public static float SeedlessFailPercent;
    /// <summary>201 벌레 대발생: 활성 여부 + 등장 딜레이 감소(초). 벌레 2마리는 스폰 로직에서 처리.</summary>
    public static bool BugFestival;
    public static float BugFestivalDelayReduce;
    /// <summary>208 집중포화: 활성 여부 + 해당 웨이브 저항 추가 감소 %p.</summary>
    public static bool HeavyFire;
    public static float HeavyFireExtraDecayPercent;

    // ───── 단발형(temporal) ─────
    /// <summary>101 반란: 우성 저항 +%p / 열성 저항 -%p.</summary>
    public static float ReversePercent;
    /// <summary>104 기상이변: 웨이브 유형 확인 불가.</summary>
    public static bool WaveBlind;
    /// <summary>108 이중 웨이브: 서로 다른 웨이브 2개 동시.</summary>
    public static bool DoubleWave;
    /// <summary>109 통신장애: 폰 확인 불가(낮 시간의 비율, 0~1).</summary>
    public static float EmpBlockRatio;
    /// <summary>106 광란: 교배 시 랜덤 교배가 될 확률 %.</summary>
    public static float BreedMadnessPercent;

    /// <summary>새 게임 시작 시 모든 저주 상태를 기본값으로 초기화(도메인 리로드 비활성 대비).</summary>
    public static void ResetAll()
    {
        RadiationDecayPercent = 0f;
        MutationAddPercent = 0f;
        PollenLostRatio = 0f;
        ShopPriceMinMul = 1f;
        ShopPriceMaxMul = 1f;
        ShopMonopoly = false;
        InsomniaFreeTimeRatio = 1f;
        SeedlessFailPercent = 0f;
        BugFestival = false;
        BugFestivalDelayReduce = 0f;
        HeavyFire = false;
        HeavyFireExtraDecayPercent = 0f;

        ReversePercent = 0f;
        WaveBlind = false;
        DoubleWave = false;
        EmpBlockRatio = 0f;
        BreedMadnessPercent = 0f;
    }
}
