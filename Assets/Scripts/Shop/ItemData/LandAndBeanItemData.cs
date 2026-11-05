using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Land And Bean (땅과 콩)", fileName = "LandAndBeanItemData")]
public class LandAndBeanItemData : ItemData
{

    private void OnEnable()
    {
        if (Price <= 0) Price = 2500;

        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
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
