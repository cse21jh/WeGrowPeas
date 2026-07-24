using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Sprinkler Performance Improvement (스프링클러 성능 향상)", fileName = "SprinklerRangeUpgradeItemData")]
public class SprinklerRangeUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int rangeBonus = 1; 

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "스프링클러 성능 향상";
        if (string.IsNullOrEmpty(Description)) Description = "매 웨이브마다 스프링클러 효과가 발동하는 작동 범위를 1칸 추가합니다.";
        if (Price <= 0) Price = 2000;
        Rarity = ItemRarity.Rare; 

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 3; 
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
