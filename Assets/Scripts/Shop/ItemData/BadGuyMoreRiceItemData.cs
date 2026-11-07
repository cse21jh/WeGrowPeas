using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Bad Guy More Rice (미운 놈 떡 하나 더 준다)", fileName = "BadGuyMoreRiceItemData")]
public class BadGuyMoreRiceItemData : ItemData
{
    private void OnEnable()
    {
        if (Price <= 0) Price = 500;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => 8;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckMaxPurchaseLimit(out reason);
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        ctx.Grid.Modifiers.AddBadGuyMoreRiceLevel(1);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null) return;

        int purchaseCount = ctx.Shop.GetItemPurchaseCount(this);
        if (purchaseCount > 0)
        {
            Price = 500 + (purchaseCount * 500); 
        }
    }
}
