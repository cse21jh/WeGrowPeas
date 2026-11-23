using UnityEngine;
using System.Collections.Generic;

public class Sprinkler : Plant
{
    private Animator anim;
    public ParticleSystem waterParticle;
    public override PlantKind Kind => PlantKind.Sprinkler;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = PlantIds.Sprinkler;
        base.Init(gridIndex, grid);
        plantID = 4; // 식물 ID 4
        anim = GetComponentInChildren<Animator>();
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

        anim.Play(AnimParams.SprinklerState);
        waterParticle.Play();

        // 어느 식물이 받는지는 State가 고른다. 여기서는 받은 식물의 가격표와 물 이펙트만 처리한다.
        if (State.SprinklerAfterWave(out PlantState target, out int previousPrice)
            && grid.Board.Plants.TryGetValue(target.GridIndex, out Plant plant) && plant is MovablePlant p)
        {
            p.RefreshSellingPriceAndNotifyExternal(previousPrice, PlantValueChangeReason.SprinklerBonus);
            p.PlayWaterParticle();
            p.ShowPriceSign();
        }
    }
}
