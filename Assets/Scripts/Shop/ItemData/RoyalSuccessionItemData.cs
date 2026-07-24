using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Royal Succession (왕위 계승)", fileName = "RoyalSuccessionItemData")]
public class RoyalSuccessionItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 계승되는 가격 배율 비율 (0.1 = 10%p)")]
    [SerializeField] private float inheritRatioPerPurchase = 0.1f;


    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "왕위 계승";
        if (string.IsNullOrEmpty(Description)) Description = "식물이 자가번식을 할 때 가격을 일부 계승합니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Rare;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 3;            // 최대 구매 제한 3회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 6;
        metaRequiredDawnPlant = "땅콩";
    }

    // 땅콩 전용 아이템 (새벽 6단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant("땅콩");

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

        ctx.Grid.AddSuccessionInheritRatio(inheritRatioPerPurchase);
    }
}
