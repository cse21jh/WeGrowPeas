using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Super Mutation (슈퍼 변종)", fileName = "SuperMutationItemData")]
public class SuperMutationItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 증가하는 변종 발생 확률(%p)")]
    [SerializeField] private float mutationChanceAddPercent = 2.5f;


    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "슈퍼 변종";
        if (string.IsNullOrEmpty(Description)) Description = "양성 변종이 더 잘 발생하게 됩니다. 추가로 변종 발생 확률이 증가합니다.";
        if (Price <= 0) Price = 2000;
        Rarity = ItemRarity.Special;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 4;            // 최대 구매 제한 4회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 10;
        metaRequiredDawnPlant = "완두콩";
    }

    // 완두콩 전용 아이템 (새벽 10단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant("완두콩");

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

        // 양성/악성 변종 확률 반전 + 변종 발생 확률 증가
        ctx.Grid.AddSuperMutation(mutationChanceAddPercent);
    }
}
