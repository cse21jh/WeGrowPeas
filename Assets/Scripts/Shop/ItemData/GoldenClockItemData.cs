using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Golden Clock (황금 시계)", fileName = "GoldenClockItemData")]
public class GoldenClockItemData : ItemData
{

    [Header("Pricing")]
    [SerializeField] private int basePrice = 500;

    private void OnEnable()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        if (Price <= 0) Price = basePrice;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        if (ctx == null)
        {
            reason = Loc.T("shop.error.no_game_state");
            return false;
        }

        if (!ctx.Shop.PurchaseHistory.TryGetValue(PurchaseKey, out int bought))
            bought = 0;

        if (bought >= MaxPurchaseCount)
        {
            reason = Loc.T("shop.error.limit_reached");
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

        GameManager.Instance.grid.Breeder.AddMaxBreedTimer(10);
        Debug.Log("황금 시계 구매: 교배 시간이 10초 증가했습니다.");
    }

    public override void InitializePrice(ShopContext ctx)
    {
        Price = basePrice;
    }
}
