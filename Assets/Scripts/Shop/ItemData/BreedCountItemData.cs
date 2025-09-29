using UnityEngine;

[CreateAssetMenu(fileName = "BreedCountItem", menuName = "Items/Breed/Max Count +1")]
public class BreedCountItemData : ItemData, IDynamicPricedItem
{
    [Header("BreedCount Settings")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private float priceFactor = 2f;

    // 여러 슬롯에서 같은 아이템을 공유할 수 있으니 키 명시(미지정 시 SO 이름)
    [SerializeField] private string priceKeyOverride = "";

    private string PriceKey => string.IsNullOrEmpty(priceKeyOverride) ? name : priceKeyOverride;

    private void OnValidate()
    {
        // 즉시형 아이템으로 고정
        FlowType = ShopFlowType.Instant;
    }

    // ---- IDynamicPricedItem ----
    public int GetCurrentPrice()
        => PriceTracker.GetPrice(PriceKey, basePrice, priceFactor);

    public void OnPurchased()
        => PriceTracker.Inc(PriceKey);

    // ---- Rotation / Weight (필요시 조절) ----
    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => 1;

    // ---- 구매 가능 여부 ----
    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;

        return true;
    }

    // ---- 시작/커밋/취소 ----
    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        // 즉시형 → 바로 커밋 준비 완료
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        // 실제 효과 적용
        var g = GameManager.Instance;
        if (g?.grid == null)
        {
            ctx?.ShowError?.Invoke("그리드가 없습니다.");
            return;
        }

        g.grid.AddMaxBreedCount(1);
        ctx?.ShowInfo?.Invoke("교배 최대 횟수 +1!");

        // ShopUI에서 골드 차감이 끝난 직후 이 Commit이 호출된다고 했으니,
        // 여기서 '구매 완료'를 알리고 다음 가격(2배)을 준비합니다.
        OnPurchased();
    }

    public override void Cancel(ShopContext ctx) { /* 즉시형이라 할 일 없음 */ }
}
