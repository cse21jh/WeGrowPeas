using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Active Shell (활성형 껍질)", fileName = "ActiveShellItemData")]
public class ActiveShellItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 증가하는 자가번식 확률 (0.05 = 5%p)")]
    [SerializeField] private float probabilityPerPurchase = 0.05f;


    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "활성형 껍질";
        if (string.IsNullOrEmpty(Description)) Description = "한 번도 교배를 시도하지 않은 식물의 자가번식 확률이 증가합니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Common;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 5;            // 최대 구매 제한 5회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 2;
        metaRequiredDawnPlant = "땅콩";
    }

    // 땅콩 전용 아이템 (새벽 2단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant("땅콩");

    public override int GetRotationWeight(ShopContext ctx) => (int)ItemRarity.Common;

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

        ctx.Grid.AddActiveShellProbability(probabilityPerPurchase);
    }
}
