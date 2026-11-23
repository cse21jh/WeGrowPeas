using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Gold Soil (황금 비료)", fileName = "GoldSoilItemData")]
public class GoldSoilItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 1;

    // 배치 확정 시 사용할 그리드 인덱스

    private void OnEnable()
    {
        if (Price <= 0) Price = 5000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.PlaceOnTile;
        metaRequiredEventId = UnlockManager.Ids.GoldenPlantCreated;
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
        reason = null;
        if (ctx.Soil.HasGoldSoil(cell)) reason = Loc.T("shop.error.gold_soil_exists");
        else if (ctx.Board.States.ContainsKey(cell)) reason = Loc.T("shop.error.gold_soil_occupied");
        return reason == null;
    }

    public override void Commit(ShopContext ctx)
    {
        if (!TakeSelectedCell(ctx, out int cell)) return;

        if (ctx.World.PlaceGoldSoil(cell))
            ctx.ShowInfo?.Invoke(Loc.T("shop.info.placed", ("name", LocalizedName)));
        else
            ctx.ShowError?.Invoke(Loc.T("shop.error.gold_soil_failed"));
    }
}
