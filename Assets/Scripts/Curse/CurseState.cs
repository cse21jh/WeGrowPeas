/// <summary>
/// 지금 걸린 저주가 각 시스템에 노출하는 수정자. <c>GameRun.Curse</c>로 쓴다. 판마다 새로 만들어진다.
/// - 각 Curse 인스턴스의 Activate()가 자기 필드를 set, Deactivate()가 기본값으로 clear.
/// - 지속형은 하나, 단발형은 하나만 동시에 활성.
/// </summary>
public sealed class CurseState
{
    // ───── 지속형(seasonal) ─────
    /// <summary>203 방사능: 매일 모든 저항력 추가 감소 %p.</summary>
    public float RadiationDecayPercent;
    /// <summary>202 돌연변이: 교배 변종 발생 확률 +%p.</summary>
    public float MutationAddPercent;
    /// <summary>204 꽃가루 실종: 매 턴 교배 불가로 바뀌는 필드 식물 비율(0~1).</summary>
    public float PollenLostRatio;
    /// <summary>205 독점시장: 상점 가격 배율 하한/상한(예: 0.8~1.2). 비활성 시 1.</summary>
    public float ShopPriceMinMul = 1f;
    public float ShopPriceMaxMul = 1f;
    public bool ShopMonopoly;
    /// <summary>206 불면증: 밤(폰 페이즈) 자유시간 배율(0~1, 1=영향 없음).</summary>
    public float InsomniaFreeTimeRatio = 1f;
    /// <summary>207 씨 없는 수박: 교배 실패 확률 %.</summary>
    public float SeedlessFailPercent;
    /// <summary>201 벌레 대발생: 활성 여부 + 등장 딜레이 감소(초). 벌레 2마리는 스폰 로직에서 처리.</summary>
    public bool BugFestival;
    public float BugFestivalDelayReduce;
    /// <summary>208 집중포화: 활성 여부 + 해당 웨이브 저항 추가 감소 %p.</summary>
    public bool HeavyFire;
    public float HeavyFireExtraDecayPercent;

    // ───── 단발형(temporal) ─────
    /// <summary>101 반란: 우성 저항 +%p / 열성 저항 -%p.</summary>
    public float ReversePercent;
    /// <summary>104 기상이변: 웨이브 유형 확인 불가.</summary>
    public bool WaveBlind;
    /// <summary>108 이중 웨이브: 서로 다른 웨이브 2개 동시.</summary>
    public bool DoubleWave;
    /// <summary>109 통신장애: 폰 확인 불가(낮 시간의 비율, 0~1).</summary>
    public float EmpBlockRatio;
    /// <summary>106 광란: 교배 시 랜덤 교배가 될 확률 %.</summary>
    public float BreedMadnessPercent;
}
