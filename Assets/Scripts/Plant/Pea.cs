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


    public override int GetSellingPrice()
    {
        if (grid == null)
        {
            // grid가 초기화되지 않은 경우 기본값 반환
            switch (taste)
            {
                case 0: return 90;
                case 1: return 130;
                case 2: return 160;
                case 3: return 180;
                case 4: return 200;
                case 5: return 230;
                case 6: return 270;
            }
            return 0;
        }

        float multiplier = grid.GetAdditionalPlantGoldMultiplier();
        int totalMultiplierCount = GetResistWaveCount() + GetBonusGoldMultiplierCount();

        switch (taste)
        {
            case 0: return (int)((90 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
            case 1: return (int)((130 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
            case 2: return (int)((160 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
            case 3: return (int)((180 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
            case 4: return (int)((200 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
            case 5: return (int)((230 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
            case 6: return (int)((270 + grid.GetAdditionalPlantGold()) * (1f + (multiplier * totalMultiplierCount)));
        }
        return 0;
    }



}
