using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Peanut : MovablePlant
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] selectedSprite;

    private float peanutCopyProbability = 0.25f;
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "¶¥Äá";
        base.Init(gridIndex, grid);
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
            stem.SetTraits(newTraits, PlantType.Peanut);
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

    public override void Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        base.Die(cause, killer);
    }

    public override void MakeSelectedSprite()
    {
        base.MakeSelectedSprite();
        /*
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[1];
        */
    }

    public override void MakeDefaultSprite()
    {
        base.MakeDefaultSprite();
        /*
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[0];
        */
    }

    protected void OnMouseEnter()
    {
        //if (ClickRouter.Instance.IsBlockedByUI) return;

        //UIPlantStat.Instance.ShowInfo(speciesname, traits, this);
        FenceUIManager.Instance.SetFenceElements(1, this);
        priceSign.gameObject.SetActive(true);
        priceSign.SetPrice(GetSellingPrice());
    }

    protected void OnMouseExit()
    {
        //UIPlantStat.Instance.HideInfo();
        FenceUIManager.Instance.HideFenceElements();
        priceSign.gameObject.SetActive(false);
    }

    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        switch (genetics)
        {
            case 0: return 0.4f;
            case 1: return 0.4f;
            case 2: return 0.7f;
        }
        return 0.1f;
    }

    public int FindEmptyGridToCopy()
    {
        List<int> emptyGrid = new List<int>();
        if ((gridIndex - 1) / 4 == gridIndex / 4) // À§Ä­
        {
            if (!grid.plantGrid.ContainsKey(gridIndex - 1))
                emptyGrid.Add(gridIndex - 1);

        }

        if ((gridIndex + 1) / 4 == gridIndex / 4) // ¾Æ·¡Ä­
        {
            if (!grid.plantGrid.ContainsKey(gridIndex + 1))
                emptyGrid.Add(gridIndex + 1);

        }

        if ((gridIndex - 4) >= 0) // ¿ÞÂÊÄ­
        {
            if (!grid.plantGrid.ContainsKey(gridIndex - 4))
                emptyGrid.Add(gridIndex - 4);

        }

        if ((gridIndex + 4) < grid.GetMaxCol() * 4) // ¿À¸¥ÂÊÄ­
        {
            if (!grid.plantGrid.ContainsKey(gridIndex + 4))
                emptyGrid.Add(gridIndex + 4);

        }

        if (emptyGrid.Count == 0)
            return -1;

        return emptyGrid[Random.Range(0, emptyGrid.Count)];
    }

    public void TrySpawnCopy()
    {
        if (Random.Range(0, 100) > 100 * (peanutCopyProbability + grid.GetAdditionalPeanutCopyProbability())) // 25ÇÁ·Î È®·ü·Î ½ºÆù
            return;
        int spawnGridIdx = FindEmptyGridToCopy();

        if (spawnGridIdx == -1) // ½ºÆùÇÒ ¼ö ÀÖ´Â À§Ä¡°¡ ¾øÀ½
            return;

        List<GeneticTrait> copyTriats = traits.ToList();
        grid.AddPeanut(copyTriats, spawnGridIdx);
        grid.totalPeanutBreedCount++;
        return;
    }
    public override int GetSellingPrice()
    {
        switch (taste)
        {
            case 0: return 60 + grid.GetAdditionalPeanutGold();
            case 1: return 100 + grid.GetAdditionalPeanutGold();
            case 2: return 130 + grid.GetAdditionalPeanutGold();
            case 3: return 150 + grid.GetAdditionalPeanutGold();
            case 4: return 170 + grid.GetAdditionalPeanutGold();
            case 5: return 200 + grid.GetAdditionalPeanutGold(); 
            case 6: return 240 + grid.GetAdditionalPeanutGold();
        }
        return 0;
    }
}
