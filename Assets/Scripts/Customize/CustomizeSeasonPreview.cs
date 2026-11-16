using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 치장 화면에서 계절별 농장 모습을 미리 보여 준다(게임의 <see cref="SeasonManager"/>와 같은 머티리얼·눈 효과를 같은 값으로 바꾼다).
/// 치장 씬에는 게임 로직이 없으므로 계절 진행과 상관없이 버튼으로만 바꾼다.
///
/// 계절별 최종 값 (SeasonManager와 같음)
///   여름: 풀 마름 0 / 꽃 숨김 / 단풍 숨김 / 눈 녹음 / 눈 내림 끔   ← 게임 시작 계절, 머티리얼 기본값
///   가을: 풀 마름 1 / 단풍 보임
///   겨울: 풀 마름 1 / 눈 쌓임 / 눈 내림 켬
///   봄  : 풀 마름 0 / 꽃 보임
///
/// 머티리얼은 에셋을 직접 바꾸므로, 이 오브젝트가 사라질 때(치장 씬을 나갈 때) 처음 값으로 되돌린다.
/// </summary>
public class CustomizeSeasonPreview : MonoBehaviour
{
    [Header("SeasonManager와 같은 대상")]
    [SerializeField] private Material grassMat;
    [SerializeField] private Material flowerMat;
    [SerializeField] private Material mapleMat;
    [SerializeField] private Material[] snowMats;
    [Tooltip("겨울에 켜는 눈 내림 효과.")]
    [SerializeField] private GameObject snowEffect;

    [Header("표시")]
    [Tooltip("지금 계절 이름을 보여 줄 글자(계절 버튼의 글자).")]
    [SerializeField] private TMP_Text seasonLabel;

    [Header("연출")]
    [SerializeField] private float changeDuration = 1f;
    [SerializeField] private Ease changeEase = Ease.InOutSine;

    private const float SnowMelted = 1.2f;
    private const float SnowPiled = 0.6f;

    // 게임과 같은 순서: 여름 → 가을 → 겨울 → 봄
    private static readonly Season[] Order = { Season.Summer, Season.Fall, Season.Winter, Season.Spring };

    public Season Current { get; private set; } = Season.Summer;

    // 씬을 나갈 때 되돌릴 원래 값
    private float _grassDryness, _flowerDissolve, _mapleDissolve;
    private float[] _snowMelt;

    private void Awake()
    {
        if (grassMat) _grassDryness = grassMat.GetFloat(ShaderProps.Dryness);
        if (flowerMat) _flowerDissolve = flowerMat.GetFloat(ShaderProps.DissolveAmount);
        if (mapleMat) _mapleDissolve = mapleMat.GetFloat(ShaderProps.DissolveAmount);
        _snowMelt = new float[snowMats?.Length ?? 0];
        for (int i = 0; i < _snowMelt.Length; i++)
            if (snowMats[i]) _snowMelt[i] = snowMats[i].GetFloat(ShaderProps.MeltStrength);
    }

    private void Start() => SetSeason(Season.Summer, instant: true);

    private void OnDestroy()
    {
        KillTweens();
        if (grassMat) grassMat.SetFloat(ShaderProps.Dryness, _grassDryness);
        if (flowerMat) flowerMat.SetFloat(ShaderProps.DissolveAmount, _flowerDissolve);
        if (mapleMat) mapleMat.SetFloat(ShaderProps.DissolveAmount, _mapleDissolve);
        for (int i = 0; i < _snowMelt.Length; i++)
            if (snowMats[i]) snowMats[i].SetFloat(ShaderProps.MeltStrength, _snowMelt[i]);
    }

    /// <summary>다음 계절로 넘긴다(여름 → 가을 → 겨울 → 봄 → 여름).</summary>
    public void NextSeason()
    {
        int i = System.Array.IndexOf(Order, Current);
        SetSeason(Order[(i + 1) % Order.Length], instant: false);
    }

    public void SetSeason(Season season, bool instant)
    {
        Current = season;
        KillTweens();

        float dryness = season == Season.Fall || season == Season.Winter ? 1f : 0f;
        float flower = season == Season.Spring ? 0f : 1f; // 0 = 보임, 1 = 숨김
        float maple = season == Season.Fall ? 0f : 1f;
        float melt = season == Season.Winter ? SnowPiled : SnowMelted;

        To(grassMat, ShaderProps.Dryness, dryness, instant);
        To(flowerMat, ShaderProps.DissolveAmount, flower, instant);
        To(mapleMat, ShaderProps.DissolveAmount, maple, instant);
        if (snowMats != null)
            foreach (var m in snowMats) To(m, ShaderProps.MeltStrength, melt, instant);

        if (snowEffect) snowEffect.SetActive(season == Season.Winter);
        if (seasonLabel) seasonLabel.text = NameOf(season);
    }

    public static string NameOf(Season season) => season switch
    {
        Season.Spring => "봄",
        Season.Summer => "여름",
        Season.Fall => "가을",
        Season.Winter => "겨울",
        _ => season.ToString(),
    };

    private void To(Material mat, int property, float value, bool instant)
    {
        if (mat == null) return;
        if (instant) { mat.SetFloat(property, value); return; }
        float from = mat.GetFloat(property);
        DOTween.To(() => from, x => mat.SetFloat(property, x), value, changeDuration)
            .SetEase(changeEase)
            .SetTarget(this);
    }

    private void KillTweens() => DOTween.Kill(this);
}
