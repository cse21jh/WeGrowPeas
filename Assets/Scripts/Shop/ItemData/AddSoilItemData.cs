using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Add Soil (농장 확장)", fileName = "AddSoilItemData")]
public class AddSoilItemData : ItemData
{
    private string purchaseKey = "���� Ȯ��";

    [Header("Pricing")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private int maxPurchase = 4; // ���� 4ȸ������

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
    }


    private void UpdatePrice(ShopContext ctx)
    {
        if (ctx.Shop.PurchaseHistory[purchaseKey] < maxPurchase)
            Price = basePrice * (ctx.Shop.PurchaseHistory[purchaseKey] + 1); // 1000,2000,3000,4000
        else
            Price = int.MaxValue; // �� �� ��
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx.Shop.PurchaseHistory.ContainsKey(purchaseKey) == false)
            ctx.Shop.PurchaseHistory[purchaseKey] = 0;

        if (ctx.Shop.PurchaseHistory[purchaseKey] >= maxPurchase)
        {
            reason = "�ִ� ���� Ƚ���� �����߽��ϴ�.";
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
            Debug.Log("�׸��尡 �����ϴ�.");
            return;
        }

        ctx.Grid.AddSoil();

        UpdatePrice(ctx);
    }
    public override void InitializePrice(ShopContext ctx)
    {
        int i;
        if (!ctx.Shop.PurchaseHistory.TryGetValue(purchaseKey, out i))
            i = 0;
        if (i < maxPurchase)
            Price = basePrice * (i + 1); // 1000,2000,3000,4000
        else
            Price = int.MaxValue; // �� �� ��
    }
}
