using UnityEngine;

[CreateAssetMenu(fileName = "AddSoilItem", menuName = "Items/Grid/Add Soil")]
public class AddSoilItemData : ItemData
{
    [Header("Pricing")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private int maxPurchase = 4; // 밭은 4회까지만

    private int purchaseCount = 0;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        UpdatePrice();
    }

    private void OnEnable()
    {
        purchaseCount = 0;
        UpdatePrice();
    }

    private void UpdatePrice()
    {
        if (purchaseCount < maxPurchase)
            Price = basePrice * (purchaseCount + 1); // 1000,2000,3000,4000
        else
            Price = int.MaxValue; // 더 못 삼
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (purchaseCount >= maxPurchase)
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

        purchaseCount++;
        UpdatePrice();
    }
}
