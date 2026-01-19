using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea Maturation Upgrade (식물 숙성도 개선)", fileName = "PeaMaturationUpgradeItemData")]
public class PeaMaturationUpgradeItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float multiplierBonus = 0.1f; // 웨이브 저항 횟수당 골드 배수 증가량

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "식물 숙성도 개선";
        if (string.IsNullOrEmpty(Description)) Description = "완두콩이 웨이브를 버틸 때마다 더 빠르게 비싸집니다.";
        if (Price <= 0) Price = 1000;
        Rarity = ItemRarity.Common; // 일반 등급
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true; // 일일 구매 제한 1회
        MaxPurchaseCount = 5; // 최대 구매 제한 5회
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => 8;

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

        // 완두콩 웨이브 저항 횟수당 골드 배수 0.1 증가
        ctx.Grid.AddAdditionalPeaGoldMultiplier(multiplierBonus);
    }
}
