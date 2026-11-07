using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea Coffee (완두커피)", fileName = "PeaCoffeeItemData")]
public class PeaCoffeeItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 증가하는, 자유시간 경과당 판매 골드 배수")]
    [SerializeField] private float multiplierPerPurchase = 0.15f;


    private void OnEnable()
    {
        if (Price <= 0) Price = 1500;

        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 6;
        metaRequiredDawnPlant = PlantIds.Pea;
    }

    // 완두콩 전용 아이템 (새벽 6단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant(PlantIds.Pea);

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

        ctx.Grid.Upgrades.AddPeaCoffeeMultiplier(multiplierPerPurchase);
    }
}
