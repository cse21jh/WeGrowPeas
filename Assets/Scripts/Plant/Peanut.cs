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
        if(!grid.showingAllPrice)
            priceSign.gameObject.SetActive(false);
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
        float copyProbability = peanutCopyProbability + grid.GetAdditionalPeanutCopyProbability();
        // 활성형 껍질: 한 번도 교배를 시도하지 않은 식물은 자가번식 확률 증가
        if (!HasTriedBreed)
            copyProbability += grid.GetActiveShellProbability();

        if (Random.Range(0f, 100f) >= 100f * copyProbability) // 25% 확률로 복사
            return;
        int spawnGridIdx = FindEmptyGridToCopy();

        if (spawnGridIdx == -1) // 복사할 수 있는 위치가 없음
            return;

        List<GeneticTrait> copyTriats = traits.ToList();

        // 특수(임시땅콩B): 자가번식 시 양성 변종만 등장 (변종 확률로 판정, 유전자 유지 + 모든 저항 90~100%)
        if (SpecialItemSystem.Has("peanut_special_8")
            && Random.Range(0f, 100f) < Plant.GetMutationChancePercent())
        {
            Plant.ApplyBenignResistance(copyTriats);
            Debug.Log("[변종] 자가번식 양성 변종 발생!"); // TODO: 변종 이펙트/사운드
        }

        Plant child = grid.AddMovablePlant(copyTriats, spawnGridIdx);

        // 왕위 계승: 자가번식한 자식이 부모 가격 배율의 일부를 계승 (유전자로 인한 최종 가격은 계승 X)
        float inheritRatio = grid.GetSuccessionInheritRatio();
        if (child != null && inheritRatio > 0f)
        {
            int inherited = Mathf.FloorToInt((GetResistWaveCount() + GetBonusGoldMultiplierCount()) * inheritRatio);
            if (inherited > 0) child.AddBonusGoldMultiplier(inherited);
        }

        grid.totalPeanutBreedCount++;
        return;
    }
    private static readonly int[] BasePrices = { 60, 100, 130, 150, 170, 200, 240 };

    public override int GetSellingPrice()
    {
        //if (isFrozen) return frozenPrice;

        int basePrice = (taste >= 0 && taste < BasePrices.Length) ? BasePrices[taste] : 0;

        if (grid == null)
        {
            return basePrice;
        }

        return CalculateSellingPrice(basePrice, grid.GetAdditionalPlantGoldMultiplier()); 
    }
}
