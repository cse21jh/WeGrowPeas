using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Add Soil (땅문서)", fileName = "AddSoilItemData")]
public class AddSoilItemData : ItemData
{
    private string purchaseKey = "땅문서";

    [Header("Pricing")]
    [SerializeField] private int basePrice = 1000;
    [SerializeField] private int maxPurchase = 4; // 최대 4회까지 구매 가능 (maxCol 4 -> 8)

    private const int MAX_COL = 8; // 최대 열 수

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        // 구매 한도는 CanPurchase(EffectiveMaxPurchase)가 담당 — 땅부자 보유 시 +8이라 공통 캡은 무제한으로
        MaxPurchaseCount = -1;
    }

    // 특수(땅부자): 추가 구매 가능 횟수 +8 (구매마다 무작위 세로줄 고속 숙성)
    private int EffectiveMaxPurchase => maxPurchase + (SpecialItemSystem.Has("land_rich") ? 8 : 0);

    private void UpdatePrice(ShopContext ctx)
    {
        // TryPurchase에서 DisplayName을 키로 사용하므로 DisplayName 사용
        var key = string.IsNullOrEmpty(DisplayName) ? purchaseKey : DisplayName;

        if (!ctx.Shop.PurchaseHistory.ContainsKey(key))
            ctx.Shop.PurchaseHistory[key] = 0;

        int purchaseCount = ctx.Shop.PurchaseHistory[key];
        // Commit 시점에는 이미 구매 이력이 증가했으므로, 다음 구매 가격 계산
        if (purchaseCount < EffectiveMaxPurchase)
            Price = basePrice * (purchaseCount + 1); // 1000, 2000, 3000, 4000 ...
        else
            Price = int.MaxValue; // 더 이상 구매 불가
    }

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;

        if (ctx?.Grid == null)
        {
            reason = "그리드가 없습니다.";
            return false;
        }

        // maxCol이 이미 최대치인지 확인 — 땅부자 보유 시엔 확장 없이도 구매 가능(고속 숙성 부여)
        if (ctx.Grid.maxCol >= MAX_COL && !SpecialItemSystem.Has("land_rich"))
        {
            reason = "최대 확장 횟수에 도달했습니다.";
            return false;
        }

        // 구매 이력 확인 (TryPurchase에서 DisplayName을 키로 사용)
        var key = string.IsNullOrEmpty(DisplayName) ? purchaseKey : DisplayName;
        if (!ctx.Shop.PurchaseHistory.ContainsKey(key))
            ctx.Shop.PurchaseHistory[key] = 0;

        if (ctx.Shop.PurchaseHistory[key] >= EffectiveMaxPurchase)
        {
            reason = "최대 구매 횟수에 도달했습니다.";
            return false;
        }

        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
        => onReady?.Invoke();

    public override void Commit(ShopContext ctx)
    {
        if (ctx?.Grid == null)
        {
            Debug.Log("그리드가 없습니다.");
            return;
        }

        // 땅 확장은 최대 크기(8열) 한도 내에서만
        if (ctx.Grid.maxCol < MAX_COL)
            ctx.Grid.AddSoil();

        // 특수(땅부자): 구매할 때마다 무작위 세로줄에 고속 숙성 효과 추가
        if (SpecialItemSystem.Has("land_rich"))
            ctx.Grid.AddLandRichColumn();

        // 가격 업데이트 (구매 이력이 업데이트된 후)
        UpdatePrice(ctx);
    }

    public override void InitializePrice(ShopContext ctx)
    {
        if (ctx?.Shop == null)
        {
            Price = basePrice;
            return;
        }

        // TryPurchase에서 DisplayName을 키로 사용하므로 DisplayName 사용
        var key = string.IsNullOrEmpty(DisplayName) ? purchaseKey : DisplayName;
        
        int purchaseCount = 0;
        if (!ctx.Shop.PurchaseHistory.TryGetValue(key, out purchaseCount))
            purchaseCount = 0;

        // 다음 구매 가격 계산
        if (purchaseCount < EffectiveMaxPurchase)
            Price = basePrice * (purchaseCount + 1); // 1000, 2000, 3000, 4000 ...
        else
            Price = int.MaxValue; // 더 이상 구매 불가
    }
}
