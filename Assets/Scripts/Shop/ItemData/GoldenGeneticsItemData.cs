using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Golden Genetics (황금 유전자)", fileName = "GoldenGeneticsItemData")]
public class GoldenGeneticsItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float probabilityBonus = 0.1f; // 10% 증가 (0.1 = 10%)

    private string purchaseKey = "황금 유전자";

    private void OnEnable()
    {
        if (Price <= 0) Price = 3000;

        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => 4;

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
