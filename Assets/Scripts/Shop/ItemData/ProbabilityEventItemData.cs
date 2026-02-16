using UnityEngine;

[CreateAssetMenu(fileName = "ProbabilityEventItemData", menuName = "Shop/Items/ProbabilityEvent")]
public class ProbabilityEventItemData : ItemData
{
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "확률 증가 이벤트";
        if (Price <= 0) Price = 2000;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1; // 일일 구매 제한 1회
        OnePerShopIfNotStackable = true; // 상점에 하나만 등장
        MaxPurchaseCount = 1; // 최대 구매 제한 1회 (이벤트는 한 번만 적용되면 됨)
        FlowType = ShopFlowType.Instant; // 즉시 사용
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        // 이미 이벤트가 적용 중이라면 구매 불가? -> 중복 적용해도 상관없긴 함 (돈만 날림)
        // 하지만 사용자 경험상 막는 게 좋을 수도 있음.
        if (ShopManager.Instance.isProbabilityEqualized)
        {
            reason = "이미 확률 증가 이벤트가 적용 중입니다.";
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
        // 확률 평탄화 활성화
        ShopManager.Instance.isProbabilityEqualized = true;
        Debug.Log("[ProbabilityEvent] Probability Equalized! All weights set to 8.");
        
        // 시각적/청각적 피드백 추가 가능
    }
}
