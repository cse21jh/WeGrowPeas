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
        State.OnPlanted(grid.Field, gridIndex); // 칸을 정하고 맛을 굴린다
        State.ResistanceChanged -= OnResistanceChanged;
        State.ResistanceChanged += OnResistanceChanged;

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

        BreedingRules.CompletePairs(traits, (trait, genetics) => GetResistanceBasedOnGenetics(trait, genetics));

        if (State.IsMovableKind) State.UpdateGoldFromTraits();
    }

    public virtual List<Vector2> GetPairData_TraitFace()
    {
        var peas = GetComponentsInChildren<PeaSpriteController>()
           .Select(pea => pea.pairData_TraitFace);

        var peanuts = GetComponentsInChildren<PeanutSpriteController>()
                      .Select(pnut => pnut.pairData_TraitFace);

        return peas.Concat(peanuts).ToList();
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
        State.MoveTo(idx);
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

    /// <summary>
    /// 식물을 죽인다. 죽을지 말지는 <see cref="FieldState.TryKill"/>이 정하고, 여기서는 그 결과를 화면에 반영한다.
    /// 막혔으면 false.
    /// </summary>
    public virtual bool Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        // 벌레가 죽이려는데 밭에 무당벌레가 있으면 무당벌레가 대신 벌레를 잡는다
        bool ladybugCanIntercept = cause == DeathCause.Bug && killer != null && grid.Board.LadybugCount != 0;

        DeathOutcome outcome = grid.Field.TryKill(State, cause, ladybugCanIntercept);
        if (!outcome.Died)
        {
            if (outcome.PetBottleRemoved)
                grid.Soil.RemovePetBottleMarker(gridIndex);
            if (outcome.Result == DeathResult.BlockedByLadybug)
                grid.StartCoroutine(grid.FirstLadybug.KillFuckingBug(killer));
            return false;
        }

        StartCoroutine(Vanish());
        grid.ClearGridIndex(gridIndex); // 밭에서는 이미 빠졌다. 여기서는 게임 오버 판정만 일어난다.
        grid.Breeder.CheckBreedButtonBeforeDie(this.gameObject);
        if (cause == DeathCause.Shovel || cause == DeathCause.Bug) // 웨이브로 인해 단체로 죽을 때는 최종 한 번만 갱신하도록
            grid.Scouter.UpdateGoldScouterImageInGrid();
        if (this is ChiliPepper)
            grid.Scouter.UpdateResistanceScouterImageInGrid(GameManager.Instance.enemyController.CurrentWave.WaveType);
        if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
            FenceUIManager.Instance.HideFenceElements();
        if (GameManager.Instance != null)
            GameManager.Instance.economyManager.AddGold(outcome.BonusGold);
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
                State.AddAdditionalResistance(traitType, value,
                    byUpgrade ? GetResistanceBasedOnGenetics(traitType, traits[i].genetics) : (float?)null);

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
    public void IncreaseWeakGeneticsResistance(float bonus) => State.IncreaseResistance(false, bonus);

    /// <summary>
    /// 강한 유전자(우성, 최초 저항력 80%인 형질)의 최초 저항력을 증가시킵니다.
    /// </summary>
    public void IncreaseStrongGeneticsResistance(float bonus) => State.IncreaseResistance(true, bonus);

    /// <summary>지금 판의 식물과 밭 보정으로 저항력을 굴린다. 공식은 <see cref="BreedingRules.RollResistance"/>에 있다.</summary>
    public static float GetResistanceBasedOnGenetics(TraitType traitType, int genetics, float p1Resistance = 0f, float p2Resistance = 0f)
        => BreedingRules.RollResistance(GameManager.Instance.currentPlant, GameManager.Instance.grid.Modifiers,
            traitType, genetics, p1Resistance, p2Resistance);


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
