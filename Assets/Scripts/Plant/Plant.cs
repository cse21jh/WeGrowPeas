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


    protected Grid grid;

    public bool isDying = false;
    
    // 급속 냉각기 관련
    protected bool isFrozen = false;
    protected int frozenPrice = 0;

    //각종 효과 관련
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

    protected int plantID = -1;

    protected float bonusGoldRatio = 0.2f; // 웨이브를 버틸 수록 추가되는 골드 비율

    public virtual void Init(int gridIndex, Grid grid)
    {
        this.gridIndex = gridIndex;
        this.grid = grid;
        taste = UnityEngine.Random.Range(0, 7);

        holdCanvas.worldCamera = FindAnyObjectByType<UIAnimationManager>().camManagers[3].GetComponent<Camera>();

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
        return GetComponentsInChildren<PeaSpriteController>()
           .Select(peaSC => peaSC.pairData_TraitFace)
           .ToList();
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
        if(isOnGoldenSoil())
        {
            stemController.SetGold(true);
        }
    }

    public bool CanResist(WaveType wave) // if can't resist, Call Die()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100);
        if (isFrozen) return true; // 얼어있으면 무조건 저항 성공

        if (randomNumber <= (int)(GetResistanceValue((int)wave) * 100))
        {
            return true;
        }
        else
        {
            return false;
        }
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
                resistance += g.resistance + g.additionalResistance; // 기본 저항력과 추가 저항력(업그레이드 및 벌레잡기) 더해줌
                return resistance = resistance >= 1f ? 1f : resistance;
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
        if(cause == DeathCause.Shovel || cause == DeathCause.Bug) // 웨이브로 인해 단체로 죽을 때는 최종 한 번만 갱신하도록
            grid.UpdateGoldScouterImageInGrid();
        if(this is ChiliPepper)
            grid.UpdateResistanceScouterImageInGrid(GameManager.Instance.enemyController.CurrentWave.WaveType);
        if (FenceUIManager.Instance.CheckFenceIsShowingMe(this.gridIndex))
            FenceUIManager.Instance.HideFenceElements();
        if(GameManager.Instance != null)
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
                    if(stemController.CheckGold(traits))
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
            ChangeResistance((int)traits[i].traitType,bonus);
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

    public static float GetResistanceBasedOnGenetics(TraitType traitType, int genetics)
    {
        float resistance = 0.0f;
        switch (GameManager.Instance.currentPlant)
        {
            case "완두콩":
                if ((int)traitType >= (int)TraitType.HeavyRain) 
                {
                    switch (genetics)
                    {
                        case 0: resistance += 0.5f; break;
                        case 1: resistance += 0.65f; break;
                        case 2: resistance += 0.8f; break;
                    }
                }
                else
                {
                    switch (genetics)
                    {
                        case 0:  resistance += 0.5f; break;
                        case 1:  resistance += 0.5f; break;
                        case 2:  resistance += 0.8f; break;
                    }
                }
                break;
            case "땅콩":
                if ((int)traitType >= (int)TraitType.HeavyRain)
                {
                    switch (genetics)
                    {
                        case 0:  resistance += 0.4f; break;
                        case 1:  resistance += 0.55f; break;
                        case 2:  resistance += 0.7f; break;
                    }
                }
                else
                {
                    switch (genetics)
                    {
                        case 0:  resistance += 0.4f; break;
                        case 1:  resistance += 0.4f; break;
                        case 2:  resistance += 0.7f; break;
                    }
                }
                break;
        }

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

        return resistance;
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

                if (var < 0.1f)
                    var = 0.1f;

                traits[i] = new GeneticTrait((TraitType)(int)traitNum, var, traits[i].genetics, traits[i].additionalResistance);
                if (stemController != null)
                {
                    if (stemController.CheckGold(traits))
                        stemController.SetGold(true);
                }
                if(this is MovablePlant p && traitNum == (int)GameManager.Instance.enemyController.CurrentWave.WaveType) // 바뀐 저항력이 현 웨이브랑 동일한 저항력이면 스카우터 체크 
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

        return (int)((basePrice + grid.GetAdditionalPlantGold() + badGeneBonus) * (1f + (multiplier * totalMultiplierCount)));
    }

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
        for(int i = 0; i < traits.Count; i++)
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
