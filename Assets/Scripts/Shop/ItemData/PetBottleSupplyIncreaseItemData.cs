using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PetBottle Supply Increase (페트병 납품량 증가)", fileName = "PetBottleSupplyIncreaseItemData")]
public class PetBottleSupplyIncreaseItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int stockBonus = 1; // 일일 최대 구매 횟수 +1

    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;


    private void OnEnable()
    {
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 9;
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

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        ctx.Grid.Modifiers.AddPetBottleInitialStockBonus(stockBonus);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        // 가격: 1000 * (구매횟수)
        int purchaseCount = GetTotalPurchaseCount();
        Price = 1000 * (purchaseCount + 1);
    }
}
