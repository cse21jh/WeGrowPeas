using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Golden Genetics (황금 유전자)", fileName = "GoldenGeneticsItemData")]
public class GoldenGeneticsItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float probabilityBonus = 0.1f; // 10% 증가 (0.1 = 10%)

    private string purchaseKey = "황금 유전자";

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "황금 유전자";
        if (string.IsNullOrEmpty(Description)) Description = "교배 시 금색 유전자가 나올 확률이 증가합니다.";
        if (Price <= 0) Price = 3000;

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
        ctx.Grid.AddGoldenGeneticsProbabilityBonus(probabilityBonus);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null) return;

        int purchaseCount = ctx.Shop.GetItemPurchaseCount(this);
        if (purchaseCount > 0)
        {
            Price = 3000 + (purchaseCount * 1000); // 구매 횟수당 1000골드 증가
        }
    }
}
