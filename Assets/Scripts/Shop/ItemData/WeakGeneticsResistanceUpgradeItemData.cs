using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Weak Genetics Resistance Upgrade (약한 유전자 생존력 개선)", fileName = "WeakGeneticsResistanceUpgradeItemData")]
public class WeakGeneticsResistanceUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float resistanceBonus = 0.05f; // 5% 증가

    private string purchaseKey = "약한 유전자 생존력 개선";

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "약한 유전자 생존력 개선";
        if (string.IsNullOrEmpty(Description)) Description = "강하지 않은 웨이브에 대한 완두콩의 저항력을 증가시킵니다.";
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 2; // 최대 구매 제한 2회
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => 1;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
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
        ctx.Grid.AddWeakGeneticsResistanceBonus(resistanceBonus);
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
