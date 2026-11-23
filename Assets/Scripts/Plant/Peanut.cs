using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Peanut : MovablePlant
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] selectedSprite;

    public override PlantKind Kind => PlantKind.Peanut;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = PlantIds.Peanut;
        base.Init(gridIndex, grid);
        plantID = 1;
    }

    public override void SetTrait(List<GeneticTrait> newTraits)
    {
        base.SetTrait(newTraits);
        StemController stem = GetComponentInChildren<StemController>();

        if (stem != null)
        {
            stem.SetTraits(newTraits, PlayablePlantType.Peanut);
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
        if(!grid.Upgrades.showingAllPrice)
            priceSign.gameObject.SetActive(false);
    }
}
