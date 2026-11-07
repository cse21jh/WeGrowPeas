using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Time Is Gold (시간은 금이다)", fileName = "TimeIsGoldItemData")]
public class TimeIsGoldItemData : ItemData
{
    private void OnEnable()
    {
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => 5;

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
        ctx.Grid.Modifiers.AddTimeIsGoldLevel(1);
        Debug.Log($"[TimeIsGold] Commit: Level increased to {ctx.Grid.Modifiers.TimeIsGoldLevel}");
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null) return;

        int purchaseCount = ctx.Shop.GetItemPurchaseCount(this);
        if (purchaseCount > 0)
        {
            Price = 1000 + (purchaseCount * 500); 
        }
    }
}
