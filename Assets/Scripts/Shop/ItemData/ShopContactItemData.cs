using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Shop Contact (상점 연락처)", fileName = "ShopContactItemData")]
public class ShopContactItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int rerollBonus = 1; // 매일 상점 무료 리롤 가능 횟수 +1

    private void OnEnable()
    {
        if (Price <= 0) Price = 2000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => 2;

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
        if (ctx?.Shop == null)
        {
            ctx?.ShowError?.Invoke("ShopManager 객체가 없습니다");
            return;
        }

        // 상점 연락처는 구매 횟수가 purchaseHistory에 기록되며,
        // 매일 초기화 시 ResetDailyRerollCount()에서 구매 횟수만큼 무료 리롤이 추가됩니다.
        // 구매 직후에도 즉시 적용하기 위해 현재 무료 리롤 횟수에 추가합니다.
        ctx.Shop.AddDailyRerollCount(rerollBonus);
        ctx.ShowInfo?.Invoke(Loc.T("shop.info.contact", ("name", LocalizedName), ("count", rerollBonus)));
    }
}
