using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/ChiliPepper Range Upgrade (매운 고추)", fileName = "ChiliPepperRangeUpgradeItemData")]
public class ChiliPepperRangeUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int rangeLevelIncrease = 1; // 범위 레벨 +1
    [SerializeField] private float probabilityIncrease = 0.02f; // 등장 확률 2% 증가

    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4; // 매운 고추 (희귀)

    private void OnEnable()
    {
        if (Price <= 0) Price = 1500;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (!ValidateGrid(ctx, out reason))
            return false;

        // 최대 레벨(2) 체크
        if (ctx.Grid.ChiliPepperRangeLevel >= 2)
        {
            reason = Loc.T("shop.error.max_range");
            return false;
        }

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
        ctx.Grid.AddChiliPepperRangeLevel(rangeLevelIncrease);
        ctx.Grid.AddChiliPepperSpawnProbability(probabilityIncrease);
    }
}
