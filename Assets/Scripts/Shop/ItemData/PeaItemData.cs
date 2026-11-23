using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea (식물 모종)", fileName = "PeaItemData")]
public class PeaItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private List<GeneticTrait> pendingTraits;

    private void OnEnable()
    {
        if (Price <= 0) Price = 500;
        
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;  // 위치 선택 없이 즉시 설치
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckHasEmptyGrid(ctx, out reason);
    }

    // 형질은 상세 패널의 드롭다운으로 고른다. (별도 형질 선택 UI를 쓰지 않음)
    public override string[] GetSelectableOptions(ShopContext ctx) => TraitOptions.GetNames(ctx.Stage, ctx.World.IsNight);

    public override void SetSelectedOption(ShopContext ctx, int index)
    {
        pendingTraits = TraitOptions.BuildTraits(index, ctx);
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        // 드롭다운에서 고른 형질을 그대로 사용. 선택이 없으면 Commit에서 기본 형질로 보정된다.
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _)) return;

        // 고른 형질이 없으면 자연사 형질 하나
        if (pendingTraits == null || pendingTraits.Count == 0)
            pendingTraits = new List<GeneticTrait>
            {
                new GeneticTrait(TraitType.NaturalDeath, ctx.World.RollResistance(TraitType.NaturalDeath, 1), 1, 0.0f)
            };

        ctx.World.PlantSeedling(pendingTraits); // 가장 앞의 빈 칸에 심긴다
        pendingTraits = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingTraits = null;
    }
}
