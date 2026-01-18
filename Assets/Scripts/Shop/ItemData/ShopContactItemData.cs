using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Shop Contact (상점 연락처)", fileName = "ShopContactItemData")]
public class ShopContactItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int rerollBonus = 1; // 매일 상점 무료 리롤 가능 횟수 +1

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "상점 연락처";
        if (string.IsNullOrEmpty(Description)) Description = "매일 상점의 물품을 무료로 갱신할 수 있는 기회를 추가로 얻습니다.";
        if (Price <= 0) Price = 2000;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
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
        if (ctx?.Shop == null)
        {
            ctx?.ShowError?.Invoke("ShopManager 객체가 없습니다");
            return;
        }
        ctx.Shop.AddDailyRerollCount(rerollBonus);
        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: 매일 무료 리롤 횟수 {rerollBonus}회 추가");
    }
}
