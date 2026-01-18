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
        speciesname = "땅콩";
        base.Init(gridIndex, grid);
        plantID = 1;
    }

    public override void SetTrait(List<GeneticTrait> newTraits)
    {
        base.SetTrait(newTraits);

        
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

    public override bool Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        return base.Die(cause, killer);
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
        FenceUIManager.Instance.SetFenceElements(plantID, this);
        priceSign.gameObject.SetActive(true);
        priceSign.SetPrice(GetSellingPrice());
    }

    protected void OnMouseExit()
    {
        //UIPlantStat.Instance.HideInfo();
        FenceUIManager.Instance.HideFenceElements();
        if(!grid.showingAllPrice)
            priceSign.gameObject.SetActive(false);
    }

    public override float GetResistanceBasedOnGenetics(TraitType traitType, int genetics)
    {
        if ((int)traitType >= (int)TraitType.HeavyRain) // ���� ������ �ִ� ���, 1���� ���׷� 60
        {
            switch (genetics)
            {
                case 0: return 0.4f;
                case 1: return 0.55f;
                case 2: return 0.7f;
            }
        }
        else
        {
            switch (genetics)
            {
                case 0: return 0.4f;
                case 1: return 0.4f;
                case 2: return 0.7f;
            }
        }
        return 0.1f;
    }

    public int FindEmptyGridToCopy()
    {
        List<int> emptyGrid = new List<int>();
        if ((gridIndex - 1) / 4 == gridIndex / 4) // ��ĭ
        {
            if (!grid.plantGrid.ContainsKey(gridIndex - 1))
                emptyGrid.Add(gridIndex - 1);

        }

        if ((gridIndex + 1) / 4 == gridIndex / 4) // �Ʒ�ĭ
        {
            if (!grid.plantGrid.ContainsKey(gridIndex + 1))
                emptyGrid.Add(gridIndex + 1);

        }

        if ((gridIndex - 4) >= 0) // ����ĭ
        {
            if (!grid.plantGrid.ContainsKey(gridIndex - 4))
                emptyGrid.Add(gridIndex - 4);

        }

        if ((gridIndex + 4) < grid.GetMaxCol() * 4) // ������ĭ
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
        if (Random.Range(0, 100) > 100 * (peanutCopyProbability + grid.GetAdditionalPeanutCopyProbability())) // 25���� Ȯ���� ����
            return;
        int spawnGridIdx = FindEmptyGridToCopy();

        if (spawnGridIdx == -1) // ������ �� �ִ� ��ġ�� ����
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
            case 0: return (int)((60 + grid.GetAdditionalPeanutGold()) * (1f + (0.2f * GetResistWaveCount())));
            case 1: return (int)((100 + grid.GetAdditionalPeanutGold()) *(1f + (0.2f * GetResistWaveCount())));
            case 2: return (int)((130 + grid.GetAdditionalPeanutGold()) *(1f + (0.2f * GetResistWaveCount())));
            case 3: return (int)((150 + grid.GetAdditionalPeanutGold()) *(1f + (0.2f * GetResistWaveCount())));
            case 4: return (int)((170 + grid.GetAdditionalPeanutGold()) *(1f + (0.2f * GetResistWaveCount())));
            case 5: return (int)((200 + grid.GetAdditionalPeanutGold()) *(1f + (0.2f * GetResistWaveCount())));
            case 6: return (int)((240 + grid.GetAdditionalPeanutGold()) *(1f + (0.2f * GetResistWaveCount())));
        }
        return 0;
    }
}
