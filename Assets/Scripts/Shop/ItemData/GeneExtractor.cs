using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/Gene Extractor (유전자 추출기)", fileName = "GeneExtractorItemData")]
public class GeneExtractorItemData : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 8;

    private void OnValidate()
    {
        FlowType = ShopFlowType.SelectExistingPlant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        Rarity = ItemRarity.Common;

        if (Price <= 0) Price = 1000;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = ctx.Stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        if (!ctx.Board.HasEmptyCell()) reason = Loc.T("shop.error.no_empty_cell");
        else if (ctx.Board.States.Count <= 0) reason = Loc.T("shop.error.no_selectable_plant");
        return reason == null;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidateTarget(ShopContext ctx, PlantState target, out string reason)
    {
        reason = target == null ? Loc.T("shop.error.select_plant") : null;
        return target != null;
    }

    public override void Commit(ShopContext ctx)
    {
        if (!TakeSelectedCell(ctx, out int cell)) return;

        List<GeneticTrait> genes = ctx.Board.States[cell].Traits;
        int spawned = 0;
        for (; spawned < 3 && ctx.Board.HasEmptyCell(); spawned++)
            ctx.World.PlantSeedling(new List<GeneticTrait>(genes));

        if (spawned > 0)
            ctx.ShowInfo?.Invoke(Loc.T("shop.info.gene_extracted", ("name", LocalizedName), ("count", spawned)));
        else
            ctx.ShowError?.Invoke(Loc.T("shop.error.not_enough_empty"));
    }
}
