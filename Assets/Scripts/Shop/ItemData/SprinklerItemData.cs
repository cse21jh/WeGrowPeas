using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Sprinkler (스프링클러)", fileName = "SprinklerItemData")]
public class SprinklerItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 희귀 등급

    private void OnEnable()
    {
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.PlaceOnTile;
        metaRequiredDawnStage = 5;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        return rotationWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckHasEmptyGrid(ctx, out reason);
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidateCell(ShopContext ctx, int cell, out string reason)
    {
        bool occupied = ctx.Board.States.ContainsKey(cell);
        reason = occupied ? Loc.T("shop.error.occupied") : null;
        return !occupied;
    }

    public override void Commit(ShopContext ctx)
    {
        if (TakeSelectedCell(ctx, out int cell))
            ctx.World.PlantFixed(PlantKind.Sprinkler, cell);
    }
}
