using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Sprinkler Performance Improvement (스프링클러 성능 향상)", fileName = "SprinklerRangeUpgradeItemData")]
public class SprinklerRangeUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int rangeBonus = 1; 

    private void OnEnable()
    {
        if (Price <= 0) Price = 2000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 5;
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
        ctx.Grid.AddSprinklerRangeBonus(rangeBonus);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null) return;

        int purchaseCount = ctx.Shop.GetItemPurchaseCount(this);
        if (purchaseCount > 0)
        {
            Price = 2000 + (purchaseCount * 1000); 
        }
    }
}
