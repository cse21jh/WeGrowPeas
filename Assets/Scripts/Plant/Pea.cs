using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class Pea : MovablePlant
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] selectedSprite;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "완두콩";
        base.Init(gridIndex, grid);
        plantID = 0;
    }

    public override void SetTrait(List<GeneticTrait> newTraits)
    {
        base.SetTrait(newTraits);
        
        StemController stem = GetComponentInChildren<StemController>();
        if (stem != null)
        {
            stem.SetTraits(newTraits, PlayablePlantType.Pea);
        }
        else
        {
            Debug.LogWarning("StemController not found in Plant");
        }
    }

    public override List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }
    /*public override void Initialize(int gridNumber, Plant parent1, Plant parent2)
    {
        base.Initialize(gridNumber, parent1, parent2);
    }

    public override void InitializeCompleteTrait(Dictionary<CompleteTraitType, int> parent1, Dictionary<CompleteTraitType, int> parent2)
    {
        base.InitializeCompleteTrait(parent1, parent2);

        foreach (CompleteTraitType trait in Enum.GetValues(typeof(CompleteTraitType)))
        {
            if (trait == CompleteTraitType.None)
                break;
            if (completeGenetics[trait] == 0)
                completeResistances[trait] = 0.9f;
            else
                completeResistances[trait] = 0.5f;
        }

        // ????? ??? ?? ???? ???. ?????? ??????? ???? ??????? 0.9 ????? ????????
    }

    public override void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        base.InitializeIncompleteTrait(parent1, parent2);
        // ????? ??? ?? ???? ???
    }*/

    public override bool Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        return base.Die(cause, killer);
    }

    public override void MakeSelectedSprite()
    {
        base.MakeSelectedSprite();
        FenceUIManager.Instance.ToggleOn(plantID, this);
        /*
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[1];
        */
    }

    public override void MakeDefaultSprite()
    {
        base.MakeDefaultSprite();
        FenceUIManager.Instance.ToggleOff(plantID, this);
        /*
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[0];
        */
    }
    protected void OnMouseEnter()
    {
        //if (ClickRouter.Instance.IsBlockedByUI) return;

        //UIPlantStat.Instance.ShowInfo(speciesname, traits, this);
        FenceUIManager.Instance.SetFenceElements(plantID, this);
        priceSign.gameObject.SetActive(true);
        priceSign.SetPrice(GetSellingPrice());
    }

    protected void OnMouseExit()
    {
        //UIPlantStat.Instance.HideInfo();
        FenceUIManager.Instance.HideFenceElements();
        if (!grid.showingAllPrice)
            priceSign.gameObject.SetActive(false);
    }


    private static readonly int[] BasePrices = { 90, 130, 160, 180, 200, 230, 270 };

    public override int GetSellingPrice()
    {
        //if (isFrozen) return frozenPrice;

        int basePrice = (taste >= 0 && taste < BasePrices.Length) ? BasePrices[taste] : 0;

        if (grid == null)
        {
            return basePrice;
        }

        // 특수(임시완두B): 주변 4칸의 서로 다른 식물 1종마다 기본 가격 +50골드
        if (SpecialItemSystem.Has("pea_special_8"))
            basePrice += 50 * grid.CountDistinctNeighborSpecies(gridIndex);

        int price = CalculateSellingPrice(basePrice, grid.GetAdditionalPlantGoldMultiplier());

        // 특수(임시완두A): 저항력 평균 수치만큼 가격 추가 증가 (곱적용, 보유 형질만 평균)
        if (SpecialItemSystem.Has("pea_special_4") && traits.Count > 0)
        {
            float sum = 0f;
            foreach (var t in traits) sum += GetResistanceValue((int)t.traitType);
            price = Mathf.RoundToInt(price * (1f + sum / traits.Count));
        }

        return price;
    }



}
