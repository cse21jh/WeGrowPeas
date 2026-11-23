using UnityEngine;

[CreateAssetMenu(fileName = "RapidFreezerItemData", menuName = "Shop/Items/RapidFreezer")]
public class RapidFreezerItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 희귀 등급

    private void OnEnable()
    {
        if (Price <= 0) Price = 2000;

        IsStackable = false;
        OnePerShopIfNotStackable = false; // 소모품이므로 상점에 여러 번 뜰 수 있음 (일일 제한은 별도)
        FlowType = ShopFlowType.PlaceOnTile;
        metaRequiredEventId = UnlockManager.Ids.WinterReached;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx)
    {
        return rotationWeight;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        // 구매 자체는 항상 가능 (타일 지정 시 사용)
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
        return true;
    }

    public override void Commit(ShopContext ctx)
    {
        if (TakeSelectedCell(ctx, out int cell))
            ctx.World.FreezeArea(cell);
    }
}
