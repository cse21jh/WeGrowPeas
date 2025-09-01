using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum DeathCause { Generic, Bug, Flood, Cold, Other,Shovel }

// 형질이나 웨이브 추가 시 GetResistantValue 및 번식 시 Initialize Trait 에서 저항력 계산 추가 필요.

public abstract class Plant : MonoBehaviour
{
    //저장이 필요한 값들
    public string speciesname;
    protected List<GeneticTrait> traits = new List<GeneticTrait>();
    protected Dictionary<CompleteTraitType, float> additionalResistance = new Dictionary<CompleteTraitType, float>();
    public int gridIndex { get; private set; }
    protected int taste;
    
    protected Grid grid;


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

    [SerializeField] protected Canvas holdCanvas;

    [SerializeField] protected GameObject foamEffect;
    [SerializeField] protected SpriteRenderer[] snowRenderers;

    public virtual bool IsMovable => false;


    public virtual void Init(int gridIndex, Grid grid)
    {
        this.gridIndex = gridIndex;
        this.grid = grid;
        taste = UnityEngine.Random.Range(0, 7);

        holdCanvas.worldCamera = FindAnyObjectByType<UIAnimationManager>().camManagers[3].GetComponent<Camera>();

        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        childMaterials = new Material[childSpriteRenderers.Length];
        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            childMaterials[i] = childSpriteRenderers[i].material; 
        }

        HideSnow(0f, Ease.Linear);

        StartCoroutine(Appear());
    }

    public virtual void SetTrait(List<GeneticTrait> newTraits)
    {
        traits = newTraits;
    }

    public void SetTaste(int val)
    {
        taste = val;
    }

    public virtual List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }


    public void SetGridIndex(int idx)
    {
        gridIndex = idx;
    }

    public bool CanResist(WaveType wave) // if can't resist, Call Die()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100);
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
        float resistance = 0f;
        foreach (var g in traits)
        {
            if (traitNum == (int)g.traitType)
            {
                if (grid.HasFertilizerAt(gridIndex)) // 해당 타입에 해당하는 비료가 있다면 0.05 더해줌
                {
                    if ((int)grid.GetFertilizerColumns()[gridIndex/4] == traitNum)
                        resistance += 0.05f;
                }
                if (CheckChiliPepper() && g.genetics <= 1) // 고추가 주변에 있고, 우성인 경우 추가 저항력 20 제공
                    resistance += 0.2f;
                resistance += g.resistance + g.additionalResistance; // 기본 저항력과 추가 저항력(업그레이드 및 벌레잡기) 더해줌
                return resistance;
            }
        }

        return 0.1f; // 저항력 없는 경우 0.1 return
    }

    public virtual void Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        // 페트병이 막으면 true 리턴 → 사망 취소
        if (grid != null && grid.TryInterceptDeath(gridIndex, cause, killer))
            return;

        StartCoroutine(Vanish());
        //UIPlantStat.Instance.HideInfo();
        grid.ClearGridIndex(gridIndex);
        Destroy(this.gameObject, dissolveDuration);
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

            for(int i = 0; i < childMaterials.Length; i++)
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
        ChangeLayerOfAllChild(gameObject, "Outline"); // "Outline" 레이어로 변경
    }

    public virtual void MakeDefaultSprite()
    {
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


    public void AddAdditionalResistance(CompleteTraitType traitType, float value)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].traitType == traitType)
            {
                traits[i] = new GeneticTrait(traitType, traits[i].resistance, traits[i].genetics, traits[i].additionalResistance + value);

                if (traits[i].additionalResistance >= 0.15f) traits[i] = new GeneticTrait(traitType, traits[i].resistance, traits[i].genetics, 0.15f);

                return;
            }
        }
    }

    public void SetAdditionalResistances(List<float> additionalResistances)
    {
        int i = 0;
        foreach(var a in additionalResistances)
        {
            additionalResistance[traits[i].traitType] = a;
            i++;
        }
        return;
    }

    public virtual List<float> GetAdditionalResistances()
    {
        var list = new List<float>();
        foreach(var a in additionalResistance.Values)
        {
            list.Add(a);
        }
        return list;   
    }

    public abstract float GetResistanceBasedOnGenetics(int genetics);

    public abstract int GetSellingPrice();

    public bool CheckChiliPepper()
    {
        Plant chiliPepper;
        if ((gridIndex - 1) / 4 == gridIndex / 4) // 위칸
        {
            if (grid.plantGrid.TryGetValue(gridIndex - 1, out chiliPepper))
            {
                if (chiliPepper.GetType() == typeof(ChiliPepper))
                    return true;
            }
        }

        if ((gridIndex + 1) / 4 == gridIndex / 4) // 아래칸
        {
            if (grid.plantGrid.TryGetValue(gridIndex + 1, out chiliPepper))
            {
                if (chiliPepper.GetType() == typeof(ChiliPepper))
                    return true;
            }
        }

        if ((gridIndex - 4) >= 0) // 왼쪽칸
        {
            if (grid.plantGrid.TryGetValue(gridIndex - 4, out chiliPepper))
            {
                if (chiliPepper.GetType() == typeof(ChiliPepper))
                    return true;
            }
        }

        if ((gridIndex + 4) < grid.GetMaxCol() * 4) // 오른쪽칸
        {
            if (grid.plantGrid.TryGetValue(gridIndex + 4, out chiliPepper))
            {
                if (chiliPepper.GetType() == typeof(ChiliPepper))
                    return true;
            }
        }
        return false;
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
        return true;
    }
}
