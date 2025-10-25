using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BreedCountItem", menuName = "Items/Breed/Max Count +1")]
public class BreedCountItemData : ItemData
{
    private string purchaseKey = "교배 횟수 증가";

    [Header("Pricing (exponential)")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private float factor = 2f;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
    }

    private void UpdatePrice(ShopContext ctx)
    {
        double priceD = basePrice * Math.Pow(factor, (ctx.Shop.PurchaseHistory[purchaseKey]));
        if (priceD > int.MaxValue) Price = int.MaxValue;
        else Price = (int)priceD;
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null; // 무제한 가능
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

        ctx.Grid.AddMaxBreedCount(1);

        UpdatePrice(ctx);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        int i;
        if (!ctx.Shop.PurchaseHistory.TryGetValue(purchaseKey, out i))
            i = 0;
        double priceD = basePrice * Math.Pow(factor, i);
        if (priceD > int.MaxValue) Price = int.MaxValue;
        else Price = (int)priceD;
    } 
}
