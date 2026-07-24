using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Land And Bean (땅과 콩)", fileName = "LandAndBeanItemData")]
public class LandAndBeanItemData : ItemData
{

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "땅과 콩";
        if (string.IsNullOrEmpty(Description)) Description = "식물이 웨이브가 지나간 후 뿌리를 내릴 확률이 증가하고, 뿌리를 내린 식물의 가격이 증가합니다.";
        if (Price <= 0) Price = 2500;
        Rarity = ItemRarity.Special;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 4;            // 최대 구매 제한 4회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 10;
        metaRequiredDawnPlant = "땅콩";
    }

    // 땅콩 전용 아이템 (새벽 10단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant("땅콩");

    public override int GetRotationWeight(ShopContext ctx) => (int)ItemRarity.Special;

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

        // 웨이브 후 뿌리 확률 +10%p + 뿌리내린 식물의 가격 +10%p
        ctx.Grid.AddLandAndBeanLevel(1);
    }
}
