using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea Taste Upgrade (식물 맛 개선)", fileName = "PeaTasteUpgradeItemData")]
public class PeaTasteUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int goldBonus = 40;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "식물 맛 개선";
        if (string.IsNullOrEmpty(Description)) Description = "완두콩의 기본 가격이 40골드 증가합니다.";
        if (Price <= 0) Price = 1000;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 3; // 최대 구매 제한 3회
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => 1;

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

        // 완두콩 기본 가격 40골드 증가
        ctx.Grid.AddAdditionalPeaGold(goldBonus);
    }
}
