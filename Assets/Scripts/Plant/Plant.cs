using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevelPhysics;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public enum DeathCause { Generic, Bug, Shovel, Other }
// Generic은 보통 웨이브에 의해 죽는 경우
// 형질이나 웨이브 추가 시 GetResistantValue 및 번식 시 Initialize Trait 에서 저항력 계산 추가 필요.

public enum PlayablePlantType
{
    Pea,
    Peanut
}

public abstract class Plant : MonoBehaviour
{
    //저장이 필요한 값들
    public string speciesname;
    protected List<GeneticTrait> traits = new List<GeneticTrait>();
    public int gridIndex { get; private set; }
    protected int taste;
    protected int resistWaveCount = 0;
    protected int bonusGoldMultiplierCount = 0; // 스프링클러 등으로 인한 추가 배수 카운트

    // 특수(세계여행): 낮 이동 거리 누적 배수(웨이브 저항 횟수당 골드 배수에 가산). 낮 시작 위치는 Grid.Breeding에서 마킹.
    protected float travelSellBonus = 0f;
    private int dayStartGridIndex = -1;

    // 완두커피: 완두커피 보유 중에 지나간 자유시간 횟수(판매 골드 배수에 가산)
    protected int freeTimePassedCount = 0;
    // 활성형 껍질: 한 번이라도 교배를 시도했는가(시도한 식물은 자가번식 확률 보너스 제외)
    protected bool hasTriedBreed = false;


    protected Grid grid;

    public bool isDying = false;

    // 세금 압류
    [Header("시각 효과")]
    [SerializeField] private GameObject seizeSticker; // 세금 압류 스티커
    [SerializeField] private ParticleSystem fogEffect; // 안개 연출 (102)
    [SerializeField] private GameObject mushroomEffect; // 버섯 연출
    [SerializeField] private ParticleSystem madnessEffect; // 광란 연출
    public bool IsSeized { get; private set; }
    public void SetSeized(bool on)
    {
        IsSeized = on;
        if (seizeSticker != null) seizeSticker.SetActive(on);
    }

    public void SetFogEffect(bool on)
    {
        if (on) fogEffect?.Play(true);
        else fogEffect?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetMushroomEffect(bool on)
    {
        if (mushroomEffect != null)
        {
            DOTween.Kill(mushroomEffect.transform);
            if (on)
            {
                mushroomEffect.SetActive(true);
                mushroomEffect.transform.DOScale(Vector3.one, 0.5f).From(Vector3.zero);
            }
            else
            {
                mushroomEffect.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() => mushroomEffect.SetActive(false));
            }
        }
    }

    public void PlayMadnessEffect()
    {
        madnessEffect?.Play(true);
    }

    // 급속 냉각기 관련
    protected bool isFrozen = false;
    protected int frozenPrice = 0;

    [SerializeField] private float dissolveDuration = 1.0f; // 분해 애니메이션 지속 시간
    private SpriteRenderer[] childSpriteRenderers;
    private Material[] childMaterials;
    private int dissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    [SerializeField] private GameObject vanishEffect;

    [SerializeField] private GameObject appearEffect;

    [SerializeField] private string[] defaultLayerObj;
    [SerializeField] private string[] uiobjLayerObj;


    [SerializeField] protected PriceTagController priceSign;
    [SerializeField] protected StemController stemController;

    [SerializeField] protected Canvas holdCanvas;

    [SerializeField] protected GameObject foamEffect;
    [SerializeField] protected SpriteRenderer[] snowRenderers;

    public virtual bool IsSelected { get; set; } = false;

    public virtual bool IsMovable => false;

    // 저주(꽃가루 실종): 교배 불가 상태
    private bool breedable = true;
    public bool IsBreedable => breedable;

    protected int plantID = -1;

    protected float bonusGoldRatio = 0.2f; // 웨이브를 버틸 수록 추가되는 골드 비율

    public virtual void Init(int gridIndex, Grid grid)
    {
        this.gridIndex = gridIndex;
        this.grid = grid;
        taste = UnityEngine.Random.Range(0, 7);

        // 밭에 등장했으면 도감에 발견 처리. 하위 클래스가 speciesname을 정한 뒤 base.Init을 부르므로
        // 여기가 모든 식물이 거치는 유일한 지점이다. (이미 발견된 종이면 아무 일도 하지 않는다)
        CodexProgress.Discover(CodexProgress.Category.Plant, speciesname);

        //holdCanvas.worldCamera = FindAnyObjectByType<UIAnimationManager>().camManagers[3].GetComponent<Camera>();
        holdCanvas.worldCamera = FindAnyObjectByType<VcamManager>().holdCanvasCamera;

        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        stemController = GetComponentInChildren<StemController>();
        childMaterials = new Material[childSpriteRenderers.Length];
        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            childMaterials[i] = childSpriteRenderers[i].material;
        }
        priceSign.SetPrice(GetSellingPrice());
        HideSnow(0f, Ease.Linear);

        StartCoroutine(Appear());
    }

    public virtual void SetTrait(List<GeneticTrait> newTraits)
    {
        traits = newTraits;

        EnsurePairedTraitExists(TraitType.HeavyRain, TraitType.Drought);
        EnsurePairedTraitExists(TraitType.Cold, TraitType.Heat);
    }

    public virtual List<Vector2> GetPairData_TraitFace()
    {
        var peas = GetComponentsInChildren<PeaSpriteController>()
           .Select(pea => pea.pairData_TraitFace);

        var peanuts = GetComponentsInChildren<PeanutSpriteController>()
                      .Select(pnut => pnut.pairData_TraitFace);

        return peas.Concat(peanuts).ToList();
    }

    private void EnsurePairedTraitExists(TraitType traitA, TraitType traitB) // 대응 형질 넣어주는 
    {
        bool hasTraitA = traits.Any(t => t.traitType == traitA);
        bool hasTraitB = traits.Any(t => t.traitType == traitB);


        // 둘 다 있거나 둘 다 없으면 스탑
        if (hasTraitA && hasTraitB || !hasTraitA && !hasTraitB)
        {
            return;
        }

        // 하나만 존재하는 경우 반대 형질 넣어줘야 함
        if (hasTraitA) // A만 존재
        {
            GeneticTrait existingTrait = traits.First(t => t.traitType == traitA);
            int genetics = existingTrait.genetics;

            GeneticTrait newTraitB = new GeneticTrait(traitB, GetResistanceBasedOnGenetics(traitB, 2 - genetics), 2 - genetics, 0f);

            traits.Add(newTraitB);
        }
        else // B만 존재
        {
            GeneticTrait existingTrait = traits.First(t => t.traitType == traitB);
            int genetics = existingTrait.genetics;

            GeneticTrait newTraitA = new GeneticTrait(traitA, GetResistanceBasedOnGenetics(traitA, 2 - genetics), 2 - genetics, 0f);

            traits.Add(newTraitA);
        }
    }

    public void SetTaste(int val)
    {
        if (isFrozen) return; // 얼어있으면 맛(가격) 변동 없음
        taste = val;
    }

    public void SetFrozen(bool freeze)
    {
        if (this is MovablePlant p)
        {
            isFrozen = freeze;
            if (isFrozen)
            {
                frozenPrice = GetSellingPrice(); // 현재 가격 저장
                p.SetIceEffect(isFrozen);
            }
            else
            {
                p.SetIceEffect(isFrozen);
            }
        }
    }

    public bool IsFrozen() => isFrozen;

    public virtual List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }


    public void SetGridIndex(int idx)
    {
        gridIndex = idx;
        if (isOnGoldenSoil())
        {
            stemController.SetGold(true);
        }

        if (CurseManager.Instance != null)
        {
            SetFogEffect(CurseManager.Instance.IsFogged(idx));
            SetMushroomEffect(CurseManager.Instance.IsMushroom(idx));
        }
    }

    public bool CanResist(WaveType wave) // if can't resist, Call Die()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100);
        if (isFrozen) return true; // 얼어있으면 무조건 저항 성공

        if (randomNumber < (int)(GetResistanceValue((int)wave) * 100))
        {
            return true;
        }

        // 특수(이중 시도): 생존 시도를 2회 시행 (1회 실패 시 재시도)
        if (SpecialItemSystem.Has("double_try"))
        {
            // TODO: 파란색 배리어 이펙트
            randomNumber = UnityEngine.Random.Range(0, 100);
            if (randomNumber < (int)(GetResistanceValue((int)wave) * 100))
                return true;
        }

        return false;
    }

    public virtual float GetResistanceValue(int traitNum) // (int)waveType 혹은 (int)traitType으로 가능
    {
        // 황금 비료에 심어진 식물은 모든 저항력 90%
        if (grid != null && isOnGoldenSoil())
        {
            return 0.9f;
        }

        float resistance = 0f;
        foreach (var g in traits)
        {
            if (traitNum == (int)g.traitType)
            {
                if (grid.HasFertilizerAt(gridIndex)) // 해당 타입에 해당하는 비료가 있다면 0.05 더해줌
                {
                    if ((int)grid.GetFertilizerColumns()[gridIndex / 4] == traitNum)
                    {
                        resistance += 0.05f;
                        // 스프링클러 범위 내에 있다면 시너지 보너스 추가
                        if (grid.IsAffectedBySprinkler(gridIndex))
                        {
                            resistance += grid.GetSprinklerFertilizerSynergyBonus();
                        }
                    }
                }
                if (CheckChiliPepper() && g.genetics <= 1) // 고추가 주변에 있고, 우성인 경우 추가 저항력 20 제공
                    resistance += 0.2f;
                // 무당벌레당 저항력 증가 (모든 형질에 적용)
                if (grid != null && grid.ladybugs != null)
                {
                    resistance += grid.ladybugs.Count * grid.AdditionalLadybugResistancePerUnit;
                }
                // 새벽: 유전자 기반 최대 저항력(우성/열성 base)이 상한선만큼 감소
                float dawnBase = Mathf.Max(0f, g.resistance);
                resistance += dawnBase + g.additionalResistance; // 기본 저항력과 추가 저항력(업그레이드 및 벌레잡기) 더해줌
                // 저주: 반란 — 우성(genetics 0·1) 저항 +%p / 열성(genetics 2) -%p
                if (CurseState.ReversePercent > 0f)
                    resistance += (g.genetics <= 1 ? 1f : -1f) * (CurseState.ReversePercent / 100f);
                if (resistance < 0f) resistance = 0f;
                return resistance = resistance >= 1f ? 1f : resistance; // 총합 상한은 1.0 유지
            }
        }

        return 0.1f; // 저항력 없는 경우 0.1 return
    }

    public virtual bool Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        // 페트병이 막으면 true 리턴 → 사망 취소
        if (grid != null && grid.TryInterceptDeath(gridIndex, cause, killer))
            return false;

        if (isFrozen && cause == DeathCause.Generic) return false; // 얼어있으면 웨이브(Generic)로 인한 죽음 면역

        int alive = grid.GetLivingPlantCount();

        // 냉각 방패: 식물이 1개 이하가 될 경우 1회에 한해 보호
        if (alive <= 1 && grid != null && grid.HasCoolingShield && !grid.IsCoolingShieldActivated())
        {
            grid.SetCoolingShieldActivated();
            return false; // 살아있는 식물이 1개 이하이고 냉각 방패가 있으면 죽지 않음
        }
        isDying = true;
        StartCoroutine(Vanish());
        //UIPlantStat.Instance.HideInfo();
        grid.ClearGridIndex(gridIndex);
        grid.CheckBreedButtonBeforeDie(this.gameObject);
        if (cause == DeathCause.Shovel || cause == DeathCause.Bug) // 웨이브로 인해 단체로 죽을 때는 최종 한 번만 갱신하도록
            grid.UpdateGoldScouterImageInGrid();
        if (this is ChiliPepper)
            grid.UpdateResistanceScouterImageInGrid(GameManager.Instance.enemyController.CurrentWave.WaveType);
        if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
            FenceUIManager.Instance.HideFenceElements();
        if (GameManager.Instance != null)
            GameManager.Instance.economyManager.AddGold((int)(GetSellingPrice() * grid.GetBonusRatioWhenDie()));
        Destroy(this.gameObject, dissolveDuration);
        return true;
    }

    private IEnumerator Vanish()
    {
        ParticlePrefab effect = Instantiate(vanishEffect, transform.position, Quaternion.identity).GetComponent<ParticlePrefab>();
        effect.PlayEffect();

        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            float lerpedDissolve = Mathf.Lerp(0f, 1.1f, elapsedTime / dissolveDuration);

            for (int i = 0; i < childMaterials.Length; i++)
            {
                childMaterials[i].SetFloat(dissolveAmountID, lerpedDissolve);
            }


            yield return null;
        }
    }

    private IEnumerator Appear()
    {
        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            float lerpedDissolve = Mathf.Lerp(1.1f, 0f, elapsedTime / dissolveDuration);

            for (int i = 0; i < childMaterials.Length; i++)
            {
                childMaterials[i].SetFloat(dissolveAmountID, lerpedDissolve);
            }


            yield return null;
        }
    }

    public virtual void MakeSelectedSprite()
    {
        IsSelected = true;
        ChangeLayerOfAllChild(gameObject, "Outline"); // "Outline" 레이어로 변경
    }

    public virtual void MakeDefaultSprite()
    {
        IsSelected = false;
        ChangeLayerOfAllChild(gameObject, "Default"); // "Default" 레이어로 변경
    }

    // 저주(꽃가루 실종): 교배 가능 여부 설정. 불가 시 어둡게 표시.
    public void SetBreedable(bool value)
    {
        breedable = value;
        if (childSpriteRenderers == null) return;
        Color tint = value ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
        foreach (var sr in childSpriteRenderers)
            if (sr != null) sr.color = tint;
    }

    private void ChangeLayerOfAllChild(GameObject obj, string layerName)
    {
        obj.layer = LayerMask.NameToLayer(layerName);

        foreach (string str in defaultLayerObj)
        {
            if (obj.name == str)
            {
                obj.layer = LayerMask.NameToLayer("Default"); // 레이어 변경하지 않음 - 즉, "Default" 레이어로 변경
            }
        }
        foreach (string str in uiobjLayerObj)
        {
            if (obj.name == str)
            {
                obj.layer = LayerMask.NameToLayer("UIObjects"); // 레이어 변경하지 않음 - 즉, "uiObjects" 레이어로 변경
            }
        }

        foreach (Transform child in obj.transform)
        {
            ChangeLayerOfAllChild(child.gameObject, layerName);
        }
    }


    public void AddAdditionalResistance(TraitType traitType, float value, bool byUpgrade = false)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].traitType == traitType)
            {
                float resistance = traits[i].resistance;

                if (byUpgrade)
                    resistance = GetResistanceBasedOnGenetics(traitType, traits[i].genetics);

                traits[i] = new GeneticTrait(traitType, resistance, traits[i].genetics, traits[i].additionalResistance + value);

                if (stemController != null)
                {
                    if (stemController.CheckGold(traits))
                        stemController.SetGold(true);
                }

                if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
                {
                    FenceUIManager.Instance.SetFenceElements(plantID, this);
                }

                return;
            }
        }
    }

    public void IncreaseResistance(float bonus)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            ChangeResistance((int)traits[i].traitType, bonus);
        }
    }

    /// <summary>
    /// 약한 유전자(열성이 아닌 형질, 최초 저항력 80%가 아닌 형질)의 최초 저항력을 증가시킵니다.
    /// </summary>
    public void IncreaseWeakGeneticsResistance(float bonus)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            // 열성이 아니고(genetics != 0) 최초 저항력이 80%가 아닌(resistance != 0.8f) 형질
            if (traits[i].genetics != 2)
            {
                ChangeResistance((int)traits[i].traitType, bonus);
            }
        }
    }

    /// <summary>
    /// 강한 유전자(우성, 최초 저항력 80%인 형질)의 최초 저항력을 증가시킵니다.
    /// </summary>
    public void IncreaseStrongGeneticsResistance(float bonus)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            // 우성(genetics == 2)이고 최초 저항력이 80%인(resistance == 0.8f) 형질
            if (traits[i].genetics == 2)
            {
                ChangeResistance((int)traits[i].traitType, bonus);
            }
        }
    }

    public static float GetResistanceBasedOnGenetics(TraitType traitType, int genetics, float p1Resistance = 0f, float p2Resistance = 0f)
    {
        float resistance = 0.0f;

        float minResistance = (p1Resistance + p2Resistance) * 0.45f;
        float maxResistance = 0.9f;

        switch (GameManager.Instance.currentPlant)
        {
            case "완두콩":
                if ((int)traitType >= (int)TraitType.HeavyRain)
                {
                    switch (genetics)
                    {
                        case 0: minResistance = Mathf.Max(0.3f, minResistance); maxResistance = 0.5f; break;
                        case 1: minResistance = Mathf.Max(0.5f, minResistance); maxResistance = 0.7f; break;
                        case 2: minResistance = Mathf.Max(0.7f, minResistance); maxResistance = 0.9f; break;
                    }
                }
                else
                {
                    switch (genetics)
                    {
                        case 0: minResistance = Mathf.Max(0.3f, minResistance); maxResistance = 0.5f; break;
                        case 1: minResistance = Mathf.Max(0.3f, minResistance); maxResistance = 0.5f; break;
                        case 2: minResistance = Mathf.Max(0.5f, minResistance); maxResistance = 0.9f; break;
                    }
                }
                break;
            case "땅콩":
                if ((int)traitType >= (int)TraitType.HeavyRain)
                {
                    switch (genetics)
                    {
                        case 0: minResistance = Mathf.Max(0.2f, minResistance); maxResistance = 0.4f; break;
                        case 1: minResistance = Mathf.Max(0.4f, minResistance); maxResistance = 0.6f; break;
                        case 2: minResistance = Mathf.Max(0.6f, minResistance); maxResistance = 0.8f; break;
                    }
                }
                else
                {
                    switch (genetics)
                    {
                        case 0: minResistance = Mathf.Max(0.3f, minResistance); maxResistance = 0.4f; break;
                        case 1: minResistance = Mathf.Max(0.3f, minResistance); maxResistance = 0.4f; break;
                        case 2: minResistance = Mathf.Max(0.4f, minResistance); maxResistance = 0.8f; break;
                    }
                }
                break;
        }

        if (minResistance > maxResistance)
            minResistance = maxResistance;

        // 변종 판정은 식물 단위로 이동(Grid.Breed / Peanut 자가번식) — 여기서는 일반 롤만
        resistance += Mathf.Round(UnityEngine.Random.Range(minResistance, maxResistance) * 100f) / 100f; // 소수점 둘째 자리 반올림



        resistance += GameManager.Instance.grid.GetResistanceBonus();

        // 강한 유전자 저항력 보너스 적용
        if (genetics == 2)
        {
            resistance += GameManager.Instance.grid.GetStrongGenericsResistanceBonus();
        }
        // 약한 유전자 저항력 보너스 적용
        else
        {
            resistance += GameManager.Instance.grid.GetWeakGenericsResistanceBonus();
        }




        // 특수(이중 시도): 최초 저항력이 결정된 직후 35% 감소 (대신 생존 시도 2회)
        if (SpecialItemSystem.Has("double_try"))
            resistance *= 0.65f;

        return Mathf.Min(resistance, 1.0f);
    }

    // ───── 변종 (식물 단위) ─────

    /// <summary>변종 발생 확률(%) = 기본 1% + 새벽(2·10단계) + 저주(돌연변이) + 슈퍼 변종.</summary>
    public static float GetMutationChancePercent()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (MutationDebug.HasChanceOverride) return MutationDebug.ChanceOverride; // 디버그 패널(F11)
#endif
        return 1f + DawnSystem.Current.mutationChanceAddPercent + CurseState.MutationAddPercent
           + (GameManager.Instance != null && GameManager.Instance.grid != null
              ? GameManager.Instance.grid.SuperMutationChanceBonus : 0f);
    }

    /// <summary>양성 변종: 모든 형질의 저항력을 90~100%로 설정 (유전자는 유지).</summary>
    public static void ApplyBenignResistance(List<GeneticTrait> traitList)
    {
        for (int i = 0; i < traitList.Count; i++)
            traitList[i] = new GeneticTrait(traitList[i].traitType,
                UnityEngine.Random.Range(90, 101) / 100f,
                traitList[i].genetics, traitList[i].additionalResistance);
    }

    public abstract int GetSellingPrice();

    public bool CheckChiliPepper()
    {
        if (grid == null) return false;
        return grid.IsAffectedByChiliPepper(gridIndex);
    }

    public int GetTaste()
    {
        return taste;
    }


    public void PlayFoamEffect()
    {
        if (foamEffect != null && !foamEffect.activeSelf)
        {
            foamEffect.SetActive(true);
            DeactivateFoamEffectAfterDelay(6f);
        }
    }

    private IEnumerator DeactivateFoamEffectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (foamEffect != null && foamEffect.activeSelf)
        {
            foamEffect.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public void ShowSnow(float duration, Ease ease)
    {
        float meltAmount = 1.2f;

        foreach (var sr in snowRenderers)
        {
            DOTween.To(() => meltAmount,
               x => { meltAmount = x; sr.material.SetFloat("_MeltStrength", x); },
               -0.2f,
               duration)
           .SetEase(ease).SetLink(sr.gameObject);
        }
    }

    public void HideSnow(float duration, Ease ease)
    {
        float meltAmount = -0.2f;

        foreach (var sr in snowRenderers)
        {
            DOTween.To(() => meltAmount,
               x => { meltAmount = x; sr.material.SetFloat("_MeltStrength", x); },
               1.2f,
               duration)
           .SetEase(ease).SetLink(sr.gameObject);
        }
    }

    public bool CanMove()
    {
        if (!IsMovable) return false;
        // 칸에 페트병이 놓여있으면 이동 금지
        if (grid != null && grid.HasPetBottle(gridIndex)) return false;
        // 황금 비료에 심어진 식물은 이동 불가
        if (grid != null && grid.HasGoldSoil(gridIndex)) return false;
        return true;
    }

    public virtual void ResistWave(WaveType waveType)
    {
        if (isFrozen) return;

        resistWaveCount++;

        // 특수(세계여행): 낮 동안 이동한 맨해튼 거리(최초↔최종 위치) 한 칸마다 판매 골드 배수 +0.1
        if (SpecialItemSystem.Has("world_travel") && dayStartGridIndex >= 0)
        {
            int dist = Mathf.Abs(gridIndex / 4 - dayStartGridIndex / 4) + Mathf.Abs(gridIndex % 4 - dayStartGridIndex % 4);
            if (dist > 0) travelSellBonus += 0.1f * dist;
        }

        /*
        bool isGold = false;
        if (stemController != null) //황금 완두콩이면 저항력 감소 아예 X
        {
            isGold = stemController.IsGold();
        }

        int fertilizer = -1;
        if (grid.HasFertilizerAt(gridIndex)) // 해당 타입에 해당하는 비료가 있다면 저항력 감소가 되지 않습니다
            fertilizer = (int)grid.GetFertilizerColumns()[gridIndex / 4];

        if (!isGold)
        {
            for (int i = 0; i < Wave.NumberOfWave; i++)
            {
                if (fertilizer != i)
                {
                    if ((int)waveType != i)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.stage > 25)
                            ChangeResistance(i, -0.1f + grid.GetResistanceDecayReduction());
                        else
                            ChangeResistance(i, -0.05f + grid.GetResistanceDecayReduction());
                    }
                    else
                    {
                        ChangeResistance(i, grid.GetResistanceAdaptation());
                    }
                }
            }
        }
        */

        // 새벽: 매일(웨이브 통과 시) 모든 저항력 감소. (일반 모드는 위 블록이 폐기되어 감소 없음)
        // 황금 완두콩은 기존 규칙대로 저항력 감소 제외.
        float dawnDailyDecay = DawnSystem.Current.dailyResistanceDecayAddPercent / 100f;
        float curseDailyDecay = CurseState.RadiationDecayPercent / 100f; // 저주: 방사능
        float totalDailyDecay = dawnDailyDecay + curseDailyDecay;
        bool isGoldPlant = stemController != null && stemController.IsGold();

        bool hasMatchingFertilizer = grid != null && grid.HasFertilizerAt(gridIndex) && (int)grid.GetFertilizerColumns()[gridIndex / 4] == (int)waveType;

        if (totalDailyDecay > 0f && !isGoldPlant)
        {
            for (int i = 0; i < Wave.NumberOfWave; i++)
            {
                if (hasMatchingFertilizer && i == (int)waveType)
                    continue; // 해당 웨이브와 동일한 비료가 깔려 있다면, 해당 웨이브 저항력은 감소하지 않음
                ChangeResistance(i, -totalDailyDecay);
            }
        }

        // 저주(집중포화): 이번(현재) 웨이브에 대한 저항력 추가 감소
        if (CurseState.HeavyFire && CurseState.HeavyFireExtraDecayPercent > 0f && !isGoldPlant && !hasMatchingFertilizer)
            ChangeResistance((int)waveType, -CurseState.HeavyFireExtraDecayPercent / 100f);

        if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
        {
            FenceUIManager.Instance.SetFenceElements(plantID, this);
        }
        priceSign.SetPrice(GetSellingPrice());
    }

    public bool ChangeResistance(int traitNum, float amount) // 기본 저항력이 바뀔 때는 무조건 해당 함수를 거치도록 (타입을 넣어서 작동)
    {
        if (amount == 0)
            return false;

        for (int i = 0; i < traits.Count; i++)
        {
            if ((int)traits[i].traitType == traitNum)
            {
                float var = traits[i].resistance;
                var += amount;

                // 특수(프로모션): 저항력이 10% 이하가 되면 해당 저항력을 90%로 변경
                if (var <= 0.1f && SpecialItemSystem.Has("promotion"))
                    var = 0.9f;

                if (var < 0.1f)
                    var = 0.1f;

                traits[i] = new GeneticTrait((TraitType)(int)traitNum, var, traits[i].genetics, traits[i].additionalResistance);
                if (stemController != null)
                {
                    if (stemController.CheckGold(traits))
                        stemController.SetGold(true);
                }
                if (this is MovablePlant p && traitNum == (int)GameManager.Instance.enemyController.CurrentWave.WaveType) // 바뀐 저항력이 현 웨이브랑 동일한 저항력이면 스카우터 체크 
                {
                    p.CheckResistanceScouterImage(GameManager.Instance.enemyController.CurrentWave.WaveType);
                }
                return true;
            }
        }
        return false;
    }

    public int GetPlantID()
    {
        return plantID;
    }

    public int GetResistWaveCount()
    {
        return resistWaveCount;
    }



    public int GetBadGenesCount()
    {
        int count = 0;
        foreach (var trait in traits)
        {
            // 자연사, 해충, 바람, 홍수 중 우성(genetics <= 1)인 경우
            if (trait.genetics <= 1 &&
               (trait.traitType == TraitType.NaturalDeath ||
                trait.traitType == TraitType.Pest ||
                trait.traitType == TraitType.Wind ||
                trait.traitType == TraitType.Flood))
            {
                count++;
            }
        }
        return count;
    }

    protected int CalculateSellingPrice(int basePrice, float multiplier)
    {
        if (grid == null) return 0;

        int totalMultiplierCount = GetResistWaveCount() + GetBonusGoldMultiplierCount();
        int badGeneBonus = grid.GetBadGuyMoreRiceLevel() * 5 * GetBadGenesCount();

        multiplier += travelSellBonus; // 특수(세계여행): 이동 거리 누적 배수
        multiplier += grid.GetColumnGoldMulBonus(gridIndex); // 특수(땅부자): 세로줄 고속 숙성 효과

        // 특수(알록달록): 땅에 적용 중인 효과 1개당 판매 배수 +0.1
        if (SpecialItemSystem.Has("colorful"))
            multiplier += 0.1f * grid.CountTileEffects(gridIndex);

        // 완두커피: 자유시간이 지난 횟수만큼 판매 골드 배수 추가
        float freeTimeBonus = grid.GetPeaCoffeeMultiplier() * freeTimePassedCount;

        int price = (int)((basePrice + grid.GetAdditionalPlantGold() + badGeneBonus)
                          * (1f + (multiplier * totalMultiplierCount) + freeTimeBonus));

        // 땅과 콩: 뿌리를 내린 식물의 가격 증가 (다른 효과와 곱적용)
        if (this is MovablePlant && !IsMovable)
            price = (int)(price * grid.GetRootedPriceMultiplier());

        return price;
    }

    /// <summary>완두커피: 자유시간이 지날 때마다 호출되어 판매 골드 배수를 누적한다. (Grid.Breeding 종료 시)</summary>
    public void OnFreeTimePassed()
    {
        freeTimePassedCount++;
        priceSign.SetPrice(GetSellingPrice());
    }

    public int GetFreeTimePassedCount() => freeTimePassedCount;
    public void SetFreeTimePassedCount(int value) => freeTimePassedCount = Mathf.Max(0, value);

    /// <summary>활성형 껍질: 교배를 시도한 식물로 표시. (Grid.ExecuteBreeding에서 호출)</summary>
    public void MarkBreedAttempted() => hasTriedBreed = true;
    public bool HasTriedBreed => hasTriedBreed;
    public void SetHasTriedBreed(bool value) => hasTriedBreed = value;

    /// <summary>특수(세계여행): 낮(교배 페이즈) 시작 시 위치 기록. Grid.Breeding에서 호출.</summary>
    public void MarkDayStartPosition()
    {
        dayStartGridIndex = gridIndex;
    }

    // 특수(세계여행) 누적 배수 저장/복원용
    public float GetTravelSellBonus() => travelSellBonus;
    public void SetTravelSellBonus(float value) => travelSellBonus = Mathf.Max(0f, value);

    public void SetResistWaveCount(int val)
    {
        resistWaveCount = val;
        priceSign.SetPrice(GetSellingPrice());
    }

    public void AddBonusGoldMultiplier(int amount)
    {
        bonusGoldMultiplierCount += amount;
        priceSign.SetPrice(GetSellingPrice());
    }

    public int GetBonusGoldMultiplierCount()
    {
        return bonusGoldMultiplierCount;
    }

    public void ShowPriceSign()
    {
        priceSign.gameObject.SetActive(true);
    }

    public void HidePriceSign()
    {
        priceSign.gameObject.SetActive(false);
    }

    public bool HasTrait(WaveType wave)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            if ((int)traits[i].traitType == (int)wave)
                return true;
        }
        return false;
    }

    public bool isOnGoldenSoil()
    {
        return grid.HasGoldSoil(gridIndex);
    }
}
