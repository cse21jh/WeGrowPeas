using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/Breed Count Increase (교배 횟수 증가)", fileName = "BreedCountItemData")]
public class BreedCountItemData : ItemData
{
    private string purchaseKey = "교배 키트";

    [Header("Pricing (exponential)")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private float factor = 2f;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
    }

    private void UpdatePrice(ShopContext ctx)
    {
        // TryPurchase에서 DisplayName을 키로 사용하므로 DisplayName 사용
        var key = string.IsNullOrEmpty(DisplayName) ? purchaseKey : DisplayName;
        int i;
        if (!ctx.Shop.PurchaseHistory.TryGetValue(key, out i))
            i = 0;
        double priceD = basePrice * Math.Pow(factor, i);
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
        if (!ValidateGrid(ctx, out _))
            return;

        ctx.Grid.AddMaxBreedCount(1);

        UpdatePrice(ctx);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        // TryPurchase에서 DisplayName을 키로 사용하므로 DisplayName 사용
        var key = string.IsNullOrEmpty(DisplayName) ? purchaseKey : DisplayName;
        int i;
        if (!ctx.Shop.PurchaseHistory.TryGetValue(key, out i))
            i = 0;
        double priceD = basePrice * Math.Pow(factor, i);
        if (priceD > int.MaxValue) Price = int.MaxValue;
        else Price = (int)priceD;
    } 
}
