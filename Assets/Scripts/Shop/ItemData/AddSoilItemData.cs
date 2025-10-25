using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddSoilItem", menuName = "Items/Grid/Add Soil")]
public class AddSoilItemData : ItemData
{
    private string purchaseKey = "농장 확장";

    [Header("Pricing")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private int maxPurchase = 4; // 밭은 4회까지만

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
    }


    private void UpdatePrice(ShopContext ctx)
    {
        if (ctx.Shop.PurchaseHistory[purchaseKey] < maxPurchase)
            Price = basePrice * (ctx.Shop.PurchaseHistory[purchaseKey] + 1); // 1000,2000,3000,4000
        else
            Price = int.MaxValue; // 더 못 삼
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx.Shop.PurchaseHistory.ContainsKey(purchaseKey) == false)
            ctx.Shop.PurchaseHistory[purchaseKey] = 0;

        if (ctx.Shop.PurchaseHistory[purchaseKey] >= maxPurchase)
        {
            reason = "최대 구매 횟수에 도달했습니다.";
            return false;
        }
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
        => onReady?.Invoke();

    public override void Commit(ShopContext ctx)
    {
        if (ctx?.Grid == null)
        {
            Debug.Log("그리드가 없습니다.");
            return;
        }

        ctx.Grid.AddSoil();

        UpdatePrice(ctx);
    }
}
