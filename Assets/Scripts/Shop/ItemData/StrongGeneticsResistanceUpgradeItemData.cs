using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Strong Genetics Resistance Upgrade (강자생존)", fileName = "StrongGeneticsResistanceUpgradeItemData")]
public class StrongGeneticsResistanceUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float resistanceBonus = 0.01f; // 1% 증가

    private string purchaseKey = "강자생존";

    private void OnEnable()
    {
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
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
        ctx.Grid.AddStrongGeneticsResistanceBonus(resistanceBonus);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null) return;

        int purchaseCount = ctx.Shop.GetItemPurchaseCount(this);
        if (purchaseCount > 0)
        {
            Price = 1000 + (purchaseCount * 500); // 구매 횟수당 500골드 증가
        }
    }
}
