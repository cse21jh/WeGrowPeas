using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Cooling Shield (냉각 방패)", fileName = "CoolingShieldItemData")]
public class CoolingShieldItemData : ItemData
{
    private void OnEnable()
    {
        if (Price <= 0) Price = 5000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
        metaRequiredEventId = UnlockManager.Ids.WinterReached;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => 1;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (!ValidateGrid(ctx, out reason))
            return false;

        // 이미 구매했는지 확인
        if (ctx.Grid.HasCoolingShield)
        {
            reason = "이미 냉각 방패를 구매했습니다. (이번 게임동안 1회만 구매 가능)";
            return false;
        }

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
        ctx.Grid.ActivateCoolingShield();
        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: 이번 게임동안 식물이 1개 이하가 될 경우 1회 보호됩니다.");
    }
}
