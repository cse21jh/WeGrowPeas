using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Royal Succession (왕위 계승)", fileName = "RoyalSuccessionItemData")]
public class RoyalSuccessionItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 계승되는 가격 배율 비율 (0.1 = 10%p)")]
    [SerializeField] private float inheritRatioPerPurchase = 0.1f;


    private void OnEnable()
    {
        if (Price <= 0) Price = 1500;

        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 6;
        metaRequiredDawnPlant = PlantIds.Peanut;
    }

    // 땅콩 전용 아이템 (새벽 6단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant(PlantIds.Peanut);

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

        ctx.Grid.Modifiers.AddSuccessionInheritRatio(inheritRatioPerPurchase);
    }
}
