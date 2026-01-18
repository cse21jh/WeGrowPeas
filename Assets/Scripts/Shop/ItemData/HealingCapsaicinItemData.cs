using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Healing Capsaicin (치료형 캡사이신)", fileName = "HealingCapsaicinItemData")]
public class HealingCapsaicinItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float healPercentPerPurchase = 0.03f; // 구매 횟수당 3% 회복
    [SerializeField] private float probabilityIncrease = 0.03f; // 등장 확률 3% 증가

    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 1;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "치료형 캡사이신";
        if (string.IsNullOrEmpty(Description)) Description = "웨이브가 끝날 때 고추의 영향 범위에 있는 식물이 모든 저항력을 소폭 회복합니다.";
        if (Price <= 0) Price = 3000;

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
        if (!CanPurchaseByLimit())
        {
            reason = "최대 구매 횟수를 초과했습니다.";
            return false;
        }
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (ctx?.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid 객체가 없습니다");
            return;
        }
        ctx.Grid.AddChiliPepperHealPercent(healPercentPerPurchase);
        ctx.Grid.AddChiliPepperSpawnProbability(probabilityIncrease);
    }
}
