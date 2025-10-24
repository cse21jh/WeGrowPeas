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
        traits = newTraits;

        foreach (GeneticTrait g in traits)
        {
            additionalResistance.Add(g.traitType, 0f);
        }

        StemController stem = GetComponentInChildren<StemController>();
        if (stem != null)
        {
            stem.SetTraits(newTraits, PlantType.Pea);
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

        // 저항력 계산 및 삽입 필요. 지금은 러프하게 열성 만족하면 0.9 저항력 가지도록
    }

    public override void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        base.InitializeIncompleteTrait(parent1, parent2);
        // 저항력 계산 및 삽입 필요
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

    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        switch (genetics)
        {
            case 0: return 0.5f;
            case 1: return 0.5f;
            case 2: return 0.8f;
        }
        return 0.1f;
    }

    public override int GetSellingPrice()
    {
        switch (taste)
        {
            case 0: return (int)(90 * (1f + (0.2f * GetResistWaveCount())));
            case 1: return (int)(130 * (1f + (0.2f * GetResistWaveCount())));
            case 2: return (int)(160 * (1f + (0.2f * GetResistWaveCount())));
            case 3: return (int)(180 * (1f + (0.2f * GetResistWaveCount())));
            case 4: return (int)(200 * (1f + (0.2f * GetResistWaveCount())));
            case 5: return (int)(230 * (1f + (0.2f * GetResistWaveCount())));
            case 6: return (int)(270 * (1f + (0.2f * GetResistWaveCount())));
        }
        return 0;
    }



}
