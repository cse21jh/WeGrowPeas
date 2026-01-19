using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PetBottle Supply Increase (페트병 납품량 증가)", fileName = "PetBottleSupplyIncreaseItemData")]
public class PetBottleSupplyIncreaseItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int stockBonus = 1; // 일일 최대 구매 횟수 +1

    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 8;

    private string purchaseKey = "페트병 납품량 증가";

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "페트병 납품량 증가";
        if (string.IsNullOrEmpty(Description)) Description = "페트병의 일일 최대 구매 횟수가 1 증가합니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Common; // 일반 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음 (X)
        FlowType = ShopFlowType.Instant;
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
        ctx.Grid.AddPetBottleInitialStockBonus(stockBonus);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        // 가격: 1000 * (구매횟수)
        int purchaseCount = GetTotalPurchaseCount();
        Price = 1000 * (purchaseCount + 1);
    }
}
