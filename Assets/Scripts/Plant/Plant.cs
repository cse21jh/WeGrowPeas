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
    public string speciesname;

    /// <summary>이 식물의 종류. 하위 클래스가 정한다.</summary>
    public abstract PlantKind Kind { get; }

    /// <summary>
    /// 판정에 쓰는 값과 공식. 저항력, 판매가, 웨이브 통과 처리는 모두 여기 있고,
    /// 이 컴포넌트는 그 결과를 화면에 보여 주는 일을 맡는다.
    /// </summary>
    public PlantState State => state ??= new PlantState(Kind);
    private PlantState state;

    // 아래는 State의 값을 예전 이름으로 가리킨다. 새 코드는 State를 직접 쓴다.
    protected List<GeneticTrait> traits { get => State.Traits; set => State.Traits = value; }
    public int gridIndex { get => State.GridIndex; private set => State.GridIndex = value; }
    protected int taste { get => State.Taste; set => State.Taste = value; }
    protected int resistWaveCount { get => State.ResistWaveCount; set => State.ResistWaveCount = value; }
    protected int bonusGoldMultiplierCount { get => State.BonusGoldMultiplierCount; set => State.BonusGoldMultiplierCount = value; }
    protected float travelSellBonus { get => State.TravelSellBonus; set => State.TravelSellBonus = value; }
    private int dayStartGridIndex { get => State.DayStartGridIndex; set => State.DayStartGridIndex = value; }
    protected int freeTimePassedCount { get => State.FreeTimePassedCount; set => State.FreeTimePassedCount = value; }
    protected int daysOnField { get => State.DaysOnField; set => State.DaysOnField = value; }
    protected bool hasTriedBreed { get => State.HasTriedBreed; set => State.HasTriedBreed = value; }
    public bool isDying { get => State.IsDying; set => State.IsDying = value; }

    protected Grid grid;

    // 세금 압류
    [Header("시각 효과")]
    [SerializeField] private GameObject seizeSticker; // 세금 압류 스티커
    [SerializeField] private ParticleSystem fogEffect; // 안개 연출 (102)
    [SerializeField] private GameObject mushroomEffect; // 버섯 연출
    [SerializeField] private ParticleSystem madnessEffect; // 광란 연출
    public bool IsSeized { get => State.IsSeized; private set => State.IsSeized = value; }
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
                mushroomEffect.transform.DOScale(Vector3.one, 0.5f).From(Vector3.zero).SetLink(mushroomEffect);
            }
            else
            {
                mushroomEffect.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() => mushroomEffect.SetActive(false)).SetLink(mushroomEffect);
            }
        }
    }

    public void PlayMadnessEffect()
    {
        madnessEffect?.Play(true);
    }

    // 급속 냉각기 관련
    protected bool isFrozen => State.IsFrozen;

    [SerializeField] private float dissolveDuration = 1.0f; // 분해 애니메이션 지속 시간
    private SpriteRenderer[] childSpriteRenderers;
    private Material[] childMaterials;
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
    private bool breedable { get => State.Breedable; set => State.Breedable = value; }
    public bool IsBreedable => breedable;

    protected int plantID = -1;

    protected float bonusGoldRatio = 0.2f; // 웨이브를 버틸 수록 추가되는 골드 비율

    public virtual void Init(int gridIndex, Grid grid)
    {
        this.grid = grid;
        State.Attach(grid.Field, gridIndex);
        State.ResistanceChanged -= OnResistanceChanged;
        State.ResistanceChanged += OnResistanceChanged;
        taste = GameRun.Rng.Field.Range(0, 7);

        // 밭에 등장했으면 도감에 발견 처리. 하위 클래스가 speciesname을 정한 뒤 base.Init을 부르므로
        // 여기가 모든 식물이 거치는 유일한 지점이다. (이미 발견된 종이면 아무 일도 하지 않는다)
        if (GameManager.Instance?.Tutorial == null)
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

    public void SetTaste(int val) => State.SetTaste(val);

    public void SetFrozen(bool freeze)
    {
        if (this is MovablePlant p)
        {
            State.SetFrozen(freeze);
            p.SetIceEffect(freeze);
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

    /// <summary>웨이브를 버티는지 굴린다. 못 버티면 호출한 쪽이 Die()를 부른다.</summary>
    public bool CanResist(WaveType wave) => State.CanResist(wave);

    /// <summary>(int)waveType 또는 (int)traitType에 대한 최종 저항력.</summary>
    public float GetResistanceValue(int traitNum) => State.GetResistanceValue(traitNum);

    public virtual bool Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        if (GameManager.Instance?.Tutorial != null && grid != null && grid.Board.CountLivingPlants() <= 1)
            return false;

        // 페트병이 막으면 true 리턴 → 사망 취소
        if (grid != null && grid.Soil.TryInterceptDeath(gridIndex, cause, killer))
            return false;

        if (isFrozen && cause == DeathCause.Generic) return false; // 얼어있으면 웨이브(Generic)로 인한 죽음 면역

        int alive = grid.Board.CountLivingPlants();

        // 냉각 방패: 식물이 1개 이하가 될 경우 1회에 한해 보호
        if (alive <= 1 && grid != null && grid.Soil.HasCoolingShield && !grid.Soil.IsCoolingShieldActivated())
        {
            grid.Soil.SetCoolingShieldActivated();
            return false; // 살아있는 식물이 1개 이하이고 냉각 방패가 있으면 죽지 않음
        }
        isDying = true;
        StartCoroutine(Vanish());
        //UIPlantStat.Instance.HideInfo();
        grid.ClearGridIndex(gridIndex);
        grid.Breeder.CheckBreedButtonBeforeDie(this.gameObject);
        if (cause == DeathCause.Shovel || cause == DeathCause.Bug) // 웨이브로 인해 단체로 죽을 때는 최종 한 번만 갱신하도록
            grid.Scouter.UpdateGoldScouterImageInGrid();
        if (this is ChiliPepper)
            grid.Scouter.UpdateResistanceScouterImageInGrid(GameManager.Instance.enemyController.CurrentWave.WaveType);
        if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
            FenceUIManager.Instance.HideFenceElements();
        if (GameManager.Instance != null)
            GameManager.Instance.economyManager.AddGold((int)(GetSellingPrice() * grid.Modifiers.BonusRatioWhenDie));
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
                childMaterials[i].SetFloat(ShaderProps.DissolveAmount, lerpedDissolve);
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
                childMaterials[i].SetFloat(ShaderProps.DissolveAmount, lerpedDissolve);
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
            case PlantIds.Pea:
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
            case PlantIds.Peanut:
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
        resistance += Mathf.Round(GameRun.Rng.Breed.Range(minResistance, maxResistance) * 100f) / 100f; // 소수점 둘째 자리 반올림



        resistance += GameManager.Instance.grid.Modifiers.ResistanceBonus;

        // 강한 유전자 저항력 보너스 적용
        if (genetics == 2)
        {
            resistance += GameManager.Instance.grid.Modifiers.StrongGeneticsResistanceBonus;
        }
        // 약한 유전자 저항력 보너스 적용
        else
        {
            resistance += GameManager.Instance.grid.Modifiers.WeakGeneticsResistanceBonus;
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
              ? GameManager.Instance.grid.Modifiers.SuperMutationChanceBonus : 0f);
    }

    /// <summary>양성 변종: 모든 형질의 저항력을 90~100%로 설정 (유전자는 유지).</summary>
    public static void ApplyBenignResistance(List<GeneticTrait> traitList)
    {
        for (int i = 0; i < traitList.Count; i++)
            traitList[i] = new GeneticTrait(traitList[i].traitType,
                GameRun.Rng.Breed.Range(90, 101) / 100f,
                traitList[i].genetics, traitList[i].additionalResistance);
    }

    public int GetSellingPrice() => State.GetSellingPrice();

    public bool CheckChiliPepper()
    {
        if (grid == null) return false;
        return grid.Soil.IsAffectedByChiliPepper(gridIndex);
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
        if (state != null) state.ResistanceChanged -= OnResistanceChanged;
        DOTween.Kill(this);
    }

    public void ShowSnow(float duration, Ease ease)
    {
        float meltAmount = 1.2f;

        foreach (var sr in snowRenderers)
        {
            DOTween.To(() => meltAmount,
               x => { meltAmount = x; sr.material.SetFloat(ShaderProps.MeltStrength, x); },
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
               x => { meltAmount = x; sr.material.SetFloat(ShaderProps.MeltStrength, x); },
               1.2f,
               duration)
           .SetEase(ease).SetLink(sr.gameObject);
        }
    }

    public bool CanMove() => State.CanMove();

    public virtual void ResistWave(WaveType waveType)
    {
        if (isFrozen) return;

        int previousSellingPrice = GetSellingPrice();
        State.ResistWave(waveType);

        if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
        {
            FenceUIManager.Instance.SetFenceElements(plantID, this);
        }
        RefreshSellingPriceAndNotify(previousSellingPrice, PlantValueChangeReason.WaveSurvived);
    }

    /// <summary>기본 저항력을 바꾼다. 기본 저항력은 반드시 이 함수(또는 State의 같은 함수)로만 바꾼다.</summary>
    public bool ChangeResistance(int traitNum, float amount) => State.ChangeResistance(traitNum, amount);

    // 기본 저항력이 바뀔 때마다 State가 알려 준다. 황금 식물 여부와 스카우터 표시를 맞춘다.
    private void OnResistanceChanged(int traitNum)
    {
        if (stemController != null)
        {
            if (stemController.CheckGold(traits))
                stemController.SetGold(true);
        }
        if (this is MovablePlant p && traitNum == (int)GameManager.Instance.enemyController.CurrentWave.WaveType) // 바뀐 저항력이 현 웨이브랑 동일한 저항력이면 스카우터 체크 
        {
            p.CheckResistanceScouterImage(GameManager.Instance.enemyController.CurrentWave.WaveType);
        }
    }

    public int GetPlantID()
    {
        return plantID;
    }

    public int GetResistWaveCount()
    {
        return resistWaveCount;
    }



    public int GetBadGenesCount() => State.GetBadGenesCount();

    protected void RefreshSellingPriceAndNotify(int previousPrice, PlantValueChangeReason reason)
    {
        int currentSellingPrice = GetSellingPrice();
        if (priceSign != null)
            priceSign.SetPrice(currentSellingPrice);

        if (reason == PlantValueChangeReason.None || currentSellingPrice <= previousPrice)
            return;

        GameEvents.RaisePlantValueFeedback(new PlantValueFeedbackData(
            GetInstanceID(),
            previousPrice,
            currentSellingPrice,
            reason,
            transform.position));
    }

    /// <summary>외부 가격 배율 변경 뒤 가격표를 갱신하고, 실제 상승분이 있으면 연출을 요청한다.</summary>
    public void RefreshSellingPriceAndNotifyExternal(int previousPrice, PlantValueChangeReason reason)
    {
        RefreshSellingPriceAndNotify(previousPrice, reason);
    }

    /// <summary>저장 데이터 복원처럼 연출이 필요 없는 경우 가격표만 현재 값으로 맞춘다.</summary>
    public void RefreshSellingPriceDisplay()
    {
        if (priceSign != null)
            priceSign.SetPrice(GetSellingPrice());
    }

    /// <summary>완두커피: 자유시간이 지날 때마다 호출되어 판매 골드 배수를 누적한다. (Grid.Breeder.Breeding 종료 시)</summary>
    public void OnFreeTimePassed()
    {
        int previousSellingPrice = GetSellingPrice();
        freeTimePassedCount++;
        RefreshSellingPriceAndNotify(previousSellingPrice, PlantValueChangeReason.FreeTimePassed);
    }

    public int GetFreeTimePassedCount() => freeTimePassedCount;

    /// <summary>밭에 심긴 뒤 지난 날 수. 옮겨 심어도 유지된다.</summary>
    public int DaysOnField => daysOnField;
    public void AddDayOnField() => daysOnField++;
    public void SetDaysOnField(int value) => daysOnField = Mathf.Max(0, value);
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

    public void AddBonusGoldMultiplier(int amount, PlantValueChangeReason reason = PlantValueChangeReason.None)
    {
        int previousSellingPrice = GetSellingPrice();
        bonusGoldMultiplierCount += amount;
        RefreshSellingPriceAndNotify(previousSellingPrice, reason);
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

    public bool HasTrait(WaveType wave) => State.HasTrait(wave);

    public bool isOnGoldenSoil()
    {
        return grid.Soil.HasGoldSoil(gridIndex);
    }
}
