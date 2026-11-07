using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Active Shell (활성형 껍질)", fileName = "ActiveShellItemData")]
public class ActiveShellItemData : ItemData
{
    [Header("Effect")]
    [Tooltip("구매 1회당 증가하는 자가번식 확률 (0.05 = 5%p)")]
    [SerializeField] private float probabilityPerPurchase = 0.05f;


    private void OnEnable()
    {
        if (Price <= 0) Price = 1000;

        IsStackable = false;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        FlowType = ShopFlowType.Instant;
        metaRequiredDawnStage = 2;
        metaRequiredDawnPlant = PlantIds.Peanut;
    }

    // 땅콩 전용 아이템 (새벽 2단계 클리어 조건은 metaRequiredDawnStage)
    public override bool IsRotationUnlockOk(ShopContext ctx) => IsCurrentPlant(PlantIds.Peanut);

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

        ctx.Grid.Modifiers.AddActiveShellProbability(probabilityPerPurchase);
    }
}
