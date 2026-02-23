using UnityEngine;
using System.Collections.Generic;

public class Sprinkler : Plant
{
    private Animator anim;
    public ParticleSystem waterParticle;
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "스프링클러";
        base.Init(gridIndex, grid);
        plantID = 4; // 식물 ID 4
        anim = GetComponentInChildren<Animator>();
        waterParticle = transform.Find("Water").GetComponent<ParticleSystem>();
    }

    public override void ResistWave(WaveType waveType)
    {
        // 스프링클러는 웨이브 피해를 입지 않음 (기본 로직 무시)
        // 저항 성공 처리 (필요하다면) -> 하지만 base.ResistWave는 resistWaveCount를 올리므로 호출하지 않음

        ApplySprinklerBonus();
    }

    private void ApplySprinklerBonus()
    {
        if (grid == null) return;

        // 인접 4칸 식물 찾기 (상하좌우 + 범위 업그레이드 반영)
        List<Plant> neighbors = new List<Plant>();
        int maxIndex = grid.GetMaxCol() * 4;
        int range = 1 + grid.GetSprinklerRangeBonus();

        anim.Play("sprinkler");
        waterParticle.Play();

        // 1. 상 (Up)
        for (int i = 1; i <= range; i++)
        {
            if (gridIndex % 4 >= i) CheckAndAddNeighbor(gridIndex - i, neighbors);
        }
        // 2. 하 (Down)
        for (int i = 1; i <= range; i++)
        {
            if (gridIndex % 4 <= 3 - i) CheckAndAddNeighbor(gridIndex + i, neighbors);
        }
        // 3. 좌 (Left)
        for (int i = 1; i <= range; i++)
        {
            int target = gridIndex - (4 * i);
            if (target >= 0) CheckAndAddNeighbor(target, neighbors);
        }
        // 4. 우 (Right)
        for (int i = 1; i <= range; i++)
        {
            int target = gridIndex + (4 * i);
            if (target < maxIndex) CheckAndAddNeighbor(target, neighbors);
        }

        if (neighbors.Count > 0)
        {
            // 무작위 1개 식물 선택
            Plant luckyPlant = neighbors[Random.Range(0, neighbors.Count)];

            // 판매 골드 배수 2회 획득 효과
            // = 이미 로직상 1회 올랐을 것이므로, 여기서 1회 더 올려줌
            // 단, 저항 횟수(ResistWaveCount) 자체는 올리지 않고 보너스 배수만 추가
            if (luckyPlant is MovablePlant p) // 일반 판매 식물인 경우에만 골드 추가
            {
                p.AddBonusGoldMultiplier(1);
                // UI 갱신 (가격표)
                p.PlayWaterParticle();
                p.ShowPriceSign();
            }
        }
    }

    private void CheckAndAddNeighbor(int adjIndex, List<Plant> neighbors)
    {
        if (grid.plantGrid.TryGetValue(adjIndex, out Plant plant))
        {
            // 살아있고 판매 가능한 식물만 대상 (스프링클러, 돈나무 등 제외)
            if (plant != null && !plant.isDying && plant.GetSellingPrice() > 0)
            {
                neighbors.Add(plant);
            }
        }
    }

    public override float GetResistanceValue(int order)
    {
        // 무적
        return 1f;
    }

    public override int GetSellingPrice()
    {
        // 판매 불가 (MoneyTree와 동일하게 처리)
        // 1000골드짜리지만 판매 시 0골드 (전략적 배치 필요)
        return 0;
    }
}
