using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Sprinkler Fertilizer Synergy (스프링클러 전용비료 시너지)", fileName = "SprinklerFertilizerSynergyItemData")]
public class SprinklerFertilizerSynergyItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float resistanceBonus = 0.01f; // 1%p

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "스프링클러 전용비료 시너지";
        if (string.IsNullOrEmpty(Description)) Description = "스프링클러의 범위 내에 있는 전용비료 칸이 상승시키는 저항력을 1%p 증가시킵니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Common; 

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 5; 
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
        ctx.Grid.AddSprinklerFertilizerSynergyBonus(resistanceBonus);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null) return;

        int purchaseCount = ctx.Shop.GetItemPurchaseCount(this);
        if (purchaseCount > 0)
        {
            Price = 1500 + (purchaseCount * 500); 
        }
    }
}
