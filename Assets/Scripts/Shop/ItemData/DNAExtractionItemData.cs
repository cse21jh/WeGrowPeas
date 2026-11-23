using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/DNA Extraction (유전자 추출기)", fileName = "DNAExtractionItemData")]
public class DNAExtractionItemData : ItemData
{
    [Header("Effect")]
    [Min(1)] public int copyCount = 3;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 8;

    private void OnValidate()
    {
        FlowType = ShopFlowType.PlaceOnTile;
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
    { reason = null; return true; }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    { onReady?.Invoke(); }

    public override void Commit(ShopContext ctx)
    {
        if (!TakeSelectedCell(ctx, out int cell)) return;

        var source = new List<GeneticTrait>(ctx.Board.States[cell].Traits);
        for (int i = 0; i < copyCount; i++)
        {
            var traits = new List<GeneticTrait>();
            foreach (GeneticTrait r in source)
            {
                int genetics = GameRun.Rng.Item.Range(0, 3);
                traits.Add(new GeneticTrait(r.traitType, ctx.World.RollResistance(r.traitType, genetics), genetics,
                    r.traitType == TraitType.Pest ? ctx.Modifiers.AdditionalPestResistance : 0f));
            }
            ctx.World.PlantSeedling(traits);
        }
        ctx.ShowInfo?.Invoke(Loc.T("shop.info.dna", ("name", LocalizedName)));
    }

    public override bool ValidateCell(ShopContext ctx, int cell, out string reason)
    {
        bool ok = ctx.Board.HasBreedablePlantAt(cell);
        reason = ok ? null : Loc.T("shop.error.no_breedable");
        return ok;
    }
}
