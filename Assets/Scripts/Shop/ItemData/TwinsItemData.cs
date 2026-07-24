using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Twins (쌍둥이)", fileName = "TwinsItemData")]
public class TwinsItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 증가하는 쌍둥이 발생 확률 (0.05 = 5%p)")]
    [SerializeField] private float probabilityPerPurchase = 0.05f;


    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "쌍둥이";
        if (string.IsNullOrEmpty(Description)) Description = "식물 교배 시 식물 2개가 생성될 확률이 증가합니다.";
        if (Price <= 0) Price = 2500;
        Rarity = ItemRarity.Special;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 4;            // 최대 구매 제한 4회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 11;
    }

    // 새벽 11단계 클리어 시 해금 (조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => (int)ItemRarity.Special;

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

        ctx.Grid.AddTwinBreedProbability(probabilityPerPurchase);
    }
}
