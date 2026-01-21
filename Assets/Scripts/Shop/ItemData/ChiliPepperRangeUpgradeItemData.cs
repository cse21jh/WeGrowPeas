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
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "매운 고추";
        if (string.IsNullOrEmpty(Description)) Description = "고추의 영향 범위를 증가시킵니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 3; // 최대 구매 제한 3회
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
            reason = "이미 최대 범위 레벨에 도달했습니다.";
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
