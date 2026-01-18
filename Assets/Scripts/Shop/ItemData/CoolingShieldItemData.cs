using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Cooling Shield (냉각 방패)", fileName = "CoolingShieldItemData")]
public class CoolingShieldItemData : ItemData
{
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "냉각 방패";
        if (string.IsNullOrEmpty(Description)) Description = "웨이브로 식물이 모두 죽을 경우 1회에 한해 적어도 1개의 식물은 살아남습니다.\n(이번 게임동안 1회만 구매할 수 있습니다!)";
        if (Price <= 0) Price = 5000;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 1; // 이번 게임동안 단 1회만 구매 가능
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => 1;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx?.Grid == null)
        {
            reason = "Grid 객체가 없습니다";
            return false;
        }

        // 이미 구매했는지 확인
        if (ctx.Grid.HasCoolingShield)
        {
            reason = "이미 냉각 방패를 구매했습니다. (이번 게임동안 1회만 구매 가능)";
            return false;
        }

        if (!CanPurchaseByLimit())
        {
            reason = "최대 구매 횟수를 초과했습니다.";
            return false;
        }

        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (ctx?.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid 객체가 없습니다");
            return;
        }
        ctx.Grid.ActivateCoolingShield();
        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: 이번 게임동안 식물이 1개 이하가 될 경우 1회 보호됩니다.");
    }
}
