using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 저주 시각 이펙트 제어. Assets/Resource/Sprites/Curse/CurseEffectManager 프리팹에 붙여 씬에 배치한다.
/// 각 저주 인스턴스(Activate/Deactivate)가 여기 함수를 호출한다.
///
/// 식물 개별 이펙트(돌연변이·꽃가루 실종)는 각 MovablePlant의 <see cref="PlantCurseManager"/>가 담당하며,
/// 여기서는 필드 전체 이펙트와 "모든 식물에 일괄 적용"을 처리한다.
/// </summary>
public class CurseEffectManager : Singleton<CurseEffectManager>
{
    [Header("Field Effects (파티클 Play/Stop)")]
    [Tooltip("벌레 대발생 — 화면 전체 파티클 (여러 개면 모두 등록)")]
    [SerializeField] private ParticleSystem[] bugEffects;

    [Tooltip("방사능")]
    [SerializeField] private ParticleSystem[] radioActiveEffects;

    [Tooltip("꽃가루 실종 — 전용 파티클 (식물 색 변화와 함께 사용)")]
    [SerializeField] private ParticleSystem[] pollenEffects;

    [Tooltip("씨 없는 수박")]
    [SerializeField] private ParticleSystem[] watermelonEffects;

    [Tooltip("반란")]
    [SerializeField] private RebellionEffectController rebellion;

    // [Tooltip("안개")]

    [Tooltip("도둑이야!")]
    [SerializeField] private ThiefEffectController thief;

    [Tooltip("기상 이변 채도 변경")]
    [SerializeField] private Volume[] volumes;
    [SerializeField] private float targetValue = -75f; // 목표 채도 값
    [SerializeField] private float[] originalValues; // 원래 채도 값
    [SerializeField] private float duration = 1f; // 채도 변화 시간
    [SerializeField] private Ease ease = Ease.Linear; // 채도 변화 이징

    [Header("대격변 연출")]
    [Tooltip("대격변 등장 파티클 (CurseEffectManager 프리팹 내부)")]
    [SerializeField] private ParticleSystem appearParticle;

    private Coroutine doubleWaveCoroutine;

    private void Start()
    {
        volumes = FindObjectsByType<Volume>(
            FindObjectsSortMode.None
        );

        originalValues = new float[volumes.Length];

        // 프리팹 파티클들이 Play On Awake로 설정돼 있어 배치만 해도 재생된다.
        // 저주가 걸리기 전까지는 아무 이펙트도 보이지 않아야 하므로 시작 시 전부 정지.
        StopAll();
    }

    // ── 저주별 on/off ─────────────────────────────────────────────────────────

    /// <summary>201 벌레 대발생.</summary>
    public void SetBugFestival(bool on) => SetEffects(bugEffects, on);

    /// <summary>203 방사능.</summary>
    public void SetRadioActive(bool on) => SetEffects(radioActiveEffects, on);

    /// <summary>207 씨 없는 수박.</summary>
    public void SetWatermelon(bool on) => SetEffects(watermelonEffects, on);

    /// <summary>101 반란.</summary>
    public void SetReverseCurse(bool on)
    {
        if (on)
            rebellion.PlayArrowAnimation();
    }

    /// <summary>102 안개.</summary>
    ///

    /// <summary>103 도둑이야!.</summary>
    public void SetThiefCurse(bool on)
    {
        if (on)
            thief.PlayLineAnimation();
    }

    /// <summary>104 기상 이변.</summary>
    public void SetWaveBlind(bool on)
    {
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] == null) continue;

            VolumeProfile profile = volumes[i].profile;
            originalValues[i] = profile.TryGet<ColorAdjustments>(out var colorAdjustments) ? colorAdjustments.saturation.value : 0f;

            if (colorAdjustments == null) continue;

            if (on)
            {
                originalValues[i] = colorAdjustments.saturation.value;
                DOTween.To(
                    () => colorAdjustments.saturation.value,
                    value => colorAdjustments.saturation.value = value,
                    targetValue,
                    duration
                )
                .SetEase(ease);
            }
            else
            {
                DOTween.To(
                    () => colorAdjustments.saturation.value,
                    value => colorAdjustments.saturation.value = value,
                    originalValues[i],
                    duration
                )
                .SetEase(ease);
            }
        }

    }

    /// <summary>105 버섯 발생.</summary>
    ///

    /// <summary>106 광란.</summary>
    ///

    /// <summary>107 대격변.</summary>
    ///

    /// <summary>108 이중 웨이브.</summary>
    ///

    /// <summary>109 통신장애.</summary>

    /// <summary>
    /// 204 꽃가루 실종. 전용 파티클 + 교배 불가 식물 색 변화(각 식물의 PlantCurseManager)를 함께 적용.
    /// </summary>
    public void SetPollenLost(bool on)
    {
        SetEffects(pollenEffects, on);
        RefreshPollenPlants(on);
    }

    /// <summary>
    /// 꽃가루 실종: 현재 교배 불가 상태인 식물만 색을 바꾼다.
    /// 저주가 꺼지면(on=false) 전부 원래 색으로 되돌린다.
    /// 매 턴 대상이 새로 굴려지므로 Grid에서 갱신 후에도 호출한다.
    /// </summary>
    public void RefreshPollenPlants(bool on)
    {
        var grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == null) return;

        foreach (var plant in grid.plantGrid.Values)
        {
            if (plant == null) continue;
            var pcm = plant.GetComponent<PlantCurseManager>();
            if (pcm == null) continue;

            pcm.SetPolenSpritesColor(on && !plant.IsBreedable);
        }
    }

    /// <summary>
    /// 202 돌연변이(변종 발생 시 해당 식물에 1회 재생).
    /// 악성이면 minus, 양성이면 plus 이펙트.
    /// </summary>
    public static void PlayMutation(Plant plant, bool benign)
    {
        if (plant == null) return;
        var pcm = plant.GetComponent<PlantCurseManager>();
        if (pcm == null) return;

        if (benign) pcm.SetMutantPlusEffect(true);
        else pcm.SetMutantMinusEffect(true);
    }

    /// <summary>안개 연출 (102)</summary>
    public void SetFogCurse(bool on)
    {
        if (GameManager.Instance != null && GameManager.Instance.grid != null)
        {
            GameManager.Instance.grid.RefreshCurseEffects();
        }
    }

    /// <summary>버섯 연출</summary>
    public void SetMushroomCurse(bool on)
    {
        if (GameManager.Instance != null && GameManager.Instance.grid != null)
        {
            GameManager.Instance.grid.RefreshCurseEffects();
        }
    }

    /// <summary>광란 연출 (일회성 재생)</summary>
    public void PlayMadnessCurse()
    {
        if (GameManager.Instance != null && GameManager.Instance.grid != null)
        {
            GameManager.Instance.grid.PlayAllPlantsMadness();
        }
    }

    /// <summary>대격변 연출 (일회성 재생)</summary>
    public void PlayAppearParticle()
    {
        if (appearParticle != null)
        {
            appearParticle.gameObject.SetActive(true);
            appearParticle.Play(true);
        }
    }

    /// <summary>통신장애 연출</summary>
    public void SetEMPCurse(bool on)
    {
        if (GameManager.Instance != null && GameManager.Instance.phoneManager != null)
        {
            GameManager.Instance.phoneManager.SetEMPEffect(on);
        }
    }

    /// <summary>이중 웨이브 연출 (타임바 교차)</summary>
    public void SetDoubleWaveCurse(bool on)
    {
        if (on)
        {
            if (doubleWaveCoroutine == null)
            {
                doubleWaveCoroutine = StartCoroutine(DoubleWaveTimerRoutine());
            }
        }
        else
        {
            if (doubleWaveCoroutine != null)
            {
                StopCoroutine(doubleWaveCoroutine);
                doubleWaveCoroutine = null;
            }
            if (GameManager.Instance != null && GameManager.Instance.enemyController != null)
            {
                // Restore to original timer
                GameManager.Instance.enemyController.SetCurrentWaveTimer();
            }
        }
    }

    private System.Collections.IEnumerator DoubleWaveTimerRoutine()
    {
        bool showFirstWave = true;
        while (true)
        {
            var breedTimerManager = FindAnyObjectByType<BreedTimerManager>();
            if (GameManager.Instance != null && GameManager.Instance.enemyController != null && breedTimerManager != null)
            {
                Wave wave1 = GameManager.Instance.enemyController.GetNextWave();
                Wave wave2 = GameManager.Instance.enemyController.GetNextSecondWave();
                
                if (wave1 != null && wave2 != null && wave2.WaveType != WaveType.None)
                {
                    breedTimerManager.SetTimer(showFirstWave ? wave1.WaveType : wave2.WaveType);
                    showFirstWave = !showFirstWave;
                }
            }
            yield return new WaitForSeconds(1.5f);
        }
    }

    /// <summary>모든 필드 이펙트 정지 (새 게임/저주 해제 시).</summary>
    public void StopAll()
    {
        SetEffects(bugEffects, false);
        SetEffects(radioActiveEffects, false);
        SetEffects(pollenEffects, false);
        SetEffects(watermelonEffects, false);
        RefreshPollenPlants(false);
    }

    private static void SetEffects(IReadOnlyList<ParticleSystem> effects, bool on)
    {
        if (effects == null) return;
        for (int i = 0; i < effects.Count; i++)
        {
            var ps = effects[i];
            if (ps == null) continue;

            if (on)
            {
                ps.Play(true); // 하위 파티클까지 함께
            }
            else
            {
                // 이미 화면에 남아있는 파티클까지 제거해야 저주가 꺼진 게 보인다.
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
