using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Credit Card (신용카드)", fileName = "CreditCardItemData")]
public class CreditCardItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 증가하는 환급 비율 (0.1 = 10%p)")]
    [SerializeField] private float refundPercentPerPurchase = 0.1f;


    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "신용카드";
        if (string.IsNullOrEmpty(Description)) Description = "하루에 물품을 3개 이상 구매하면 자유시간에 소모 비용의 일부를 돌려받습니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Rare;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 3;            // 최대 구매 제한 3회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 7;
    }

    // 새벽 7단계 클리어 시 해금 (조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => (int)ItemRarity.Rare;

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

        ctx.Grid.AddCreditCardRefundPercent(refundPercentPerPurchase);
    }
}
