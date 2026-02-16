using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Bad Guy More Rice (미운 놈 떡 하나 더 준다)", fileName = "BadGuyMoreRiceItemData")]
public class BadGuyMoreRiceItemData : ItemData
{
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "미운 놈 떡 하나 더 준다";
        if (string.IsNullOrEmpty(Description)) Description = "식물이 보유한 자연사/해충/바람/홍수 관련 우성 유전자(1등급 이하) 1개당 판매 가격이 증가합니다.";
        if (Price <= 0) Price = 500;
        Rarity = ItemRarity.Common; 

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 3; 
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
        ctx.Grid.AddBadGuyMoreRiceLevel(1);
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
