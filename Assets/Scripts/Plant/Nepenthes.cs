using DG.DemiLib;
using UnityEngine;
using UnityEngine.UI;


public class Nepenthes : Plant
{
    [SerializeField] private GameObject NepenthesPheromone;
    [SerializeField] private float pheromoneSize = 4f;
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "네펜데스";
        base.Init(gridIndex, grid);
        plantID = 2;
        UpdatePheromone();
        UpdatePheromoneSize();
    }
    public override float GetResistanceValue(int order)
    {
        return 1f;
    }


    public override void ResistWave(WaveType waveType)
    {
        // 특수(채식주의자): 웨이브를 버틸 때마다 가치 증가
        if (SpecialItemSystem.Has("vegetarian")) resistWaveCount++;
        return;
    }
    public override int GetSellingPrice()
    {
        // 특수(채식주의자): 판매가 500 통일 + 다른 식물과 동일한 가격 상승 공식 적용
        if (SpecialItemSystem.Has("vegetarian") && grid != null)
            return CalculateSellingPrice(500, grid.GetAdditionalPlantGoldMultiplier());
        return 0;
    }

    public void UpdatePheromone()
    {
        if (NepenthesPheromone != null && grid != null)
        {
            NepenthesPheromone.SetActive(grid.HasNepenthesPheromone);
        }
    }

    public void UpdatePheromoneSize()
    {
        if (NepenthesPheromone != null && grid != null)
        {
            float multiplier = grid.GetEffectiveNepenthesPheromoneSizeMultiplier();
            float finalSize = pheromoneSize * multiplier;
            NepenthesPheromone.transform.localScale = new Vector3(finalSize, finalSize, 1f);
        }
        else if (NepenthesPheromone != null)
        {
            NepenthesPheromone.transform.localScale = new Vector3(pheromoneSize, pheromoneSize, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, pheromoneSize/2);
    }

}
