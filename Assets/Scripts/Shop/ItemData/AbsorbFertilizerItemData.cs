using UnityEngine;

[CreateAssetMenu(fileName = "AbsorbFertilizerItemData", menuName = "Shop/Items/AbsorbFertilizer")]
public class AbsorbFertilizerItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 희귀 등급

    private void OnEnable()
    {
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        OnePerShopIfNotStackable = false; // 소모품
        FlowType = ShopFlowType.PlaceOnTile;
        metaRequiredEventId = UnlockManager.Ids.FertilizerFourColumns;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidateCell(ShopContext ctx, int cell, out string reason)
    {
        bool exists = ctx.Soil.HasAbsorbFertilizer(cell);
        reason = exists ? Loc.T("shop.error.absorb_exists") : null;
        return !exists;
    }

    public override void Commit(ShopContext ctx)
    {
        if (TakeSelectedCell(ctx, out int cell))
            ctx.World.AddAbsorbFertilizer(cell);
    }
}
