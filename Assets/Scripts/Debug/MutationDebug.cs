using UnityEngine;

/// <summary>
/// 변종(악성/양성) 테스트용 디버그 오버라이드.
/// 평상시엔 전부 비활성(<see cref="ChanceOverride"/> &lt; 0)이라 게임 로직에 영향이 없다.
/// F11 디버그 패널에서 조절한다.
/// </summary>
public static class MutationDebug
{
    /// <summary>변종 발생 확률(%) 강제. 음수면 미적용(원래 확률 사용).</summary>
    public static float ChanceOverride = -1f;

    /// <summary>악성 비율(0~1) 강제. 음수면 미적용(기본 0.8 / 슈퍼 변종 시 0.2).</summary>
    public static float MalignantRatioOverride = -1f;

    public static bool HasChanceOverride => ChanceOverride >= 0f;
    public static bool HasRatioOverride => MalignantRatioOverride >= 0f;

    public static void Reset()
    {
        ChanceOverride = -1f;
        MalignantRatioOverride = -1f;
    }
}
