using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Golden Clock (황금 시계)", fileName = "GoldenClockItemData")]
public class GoldenClockItemData : ItemData
{
    private const string purchaseKey = "황금 시계";

    [Header("Pricing")]
    [SerializeField] private int basePrice = 500;

    [Header("Limit")]
    [SerializeField] private int maxTotalPurchase = 2; // 전체 게임 내 최대 구매 가능 횟수

    private void OnEnable()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        InitialStock = 1;
        MaxPurchaseCount = maxTotalPurchase;
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "황금 시계";
        if (string.IsNullOrEmpty(Description)) Description = "구매 시 교배 시간이 10초 증가";
        if (Price <= 0) Price = basePrice;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        if (ctx == null)
        {
            reason = "컨텍스트 없음";
            return false;
        }

        if (!ctx.Shop.PurchaseHistory.TryGetValue(purchaseKey, out int bought))
            bought = 0;

        if (bought >= maxTotalPurchase)
        {
            reason = "구매 한도 도달";
            return false;
        }

        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
        => onReady?.Invoke();

    public override void Commit(ShopContext ctx)
    {
        if (GameManager.Instance?.grid == null)
        {
            Debug.LogWarning("GoldenClock: GameManager.Instance.grid가 없습니다.");
            return;
        }

        GameManager.Instance.grid.AddMaxBreedTimer(10);
        Debug.Log("황금 시계 구매: 교배 시간이 10초 증가했습니다.");
    }

    public override void InitializePrice(ShopContext ctx)
    {
        Price = basePrice;
    }
}
