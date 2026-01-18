using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/PetBottle Material Upgrade (페트병 재질 강화)", fileName = "PetBottleMaterialUpgradeItemData")]
public class PetBottleMaterialUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private int blockCountBonus = 1; // 페트병이 막아주는 웨이브 횟수 +1
    [SerializeField] private float probabilityIncrease = 0.03f; // 등장 확률 3% 증가

    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 1;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "페트병 재질 강화";
        if (string.IsNullOrEmpty(Description)) Description = "페트병을 강하게 만들어 웨이브를 1회 더 막을 수 있게 해줍니다.";
        if (Price <= 0) Price = 3000;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = 2; // 최대 구매 제한 2회
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
        ctx.Grid.AddPetBottleBlockCount(blockCountBonus);
        ctx.Grid.AddPetBottleSpawnProbability(probabilityIncrease);
    }
}
