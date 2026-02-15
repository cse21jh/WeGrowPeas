using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Time Is Gold (시간은 금이다)", fileName = "TimeIsGoldItemData")]
public class TimeIsGoldItemData : ItemData
{
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "시간은 금이다";
        if (string.IsNullOrEmpty(Description)) Description = "웨이브 스킵 시 (남은 시간 * 10골드)를 획득합니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Rare; 

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 5; 
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
        ctx.Grid.AddTimeIsGoldLevel(1);
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
