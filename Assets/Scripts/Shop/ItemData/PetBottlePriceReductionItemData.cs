using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PetBottle Price Reduction (페트병 원가 감소)", fileName = "PetBottlePriceReductionItemData")]
public class PetBottlePriceReductionItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int priceReduction = 100; // 페트병 가격 -100골드
    [SerializeField] private float probabilityIncrease = 0.02f; // 등장 확률 2% 증가

    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "페트병 원가 감소";
        if (string.IsNullOrEmpty(Description)) Description = "페트병의 가격이 100골드 감소합니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 3; // 최대 구매 제한 3회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 9;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

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
        ctx.Grid.AddPetBottlePriceReduction(priceReduction);
        ctx.Grid.AddPetBottleSpawnProbability(probabilityIncrease);
    }
}
