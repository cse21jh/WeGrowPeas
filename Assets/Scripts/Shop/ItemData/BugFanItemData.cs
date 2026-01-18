using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Bug Fan (벌레 방해 선풍기)", fileName = "BugFanItemData")]
public class BugFanItemData : ItemData
{
    private string purchaseKey = "벌레 방해 선풍기";

    [Header("Effect")]
    [Range(0f, 1f)] public float reducePercent = 0.1f; // 10%

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 11;
    [Min(0)] public int rotationWeight = 3;

    [Header("Limit")]
    [Min(1)] public int maxTotalPurchase = 3;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        InitialStock = 1;
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "벌레 방해 선풍기";
        if (string.IsNullOrEmpty(Description)) Description = "벌레 이동속도 영구적으로 10% 감소 (중첩 시 합적용)";
        if (Price <= 0) Price = 500;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null)
        {
            reason = "컨텍스트 없음";
            return false;
        }

        if (!ctx.Shop.PurchaseHistory.TryGetValue(purchaseKey, out int bought))
            bought = 0;

        if (bought >= maxTotalPurchase)
        {
            reason = "구매 한도 도달";
            return false;
        }

        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        // 즉시 영구 적용: Grid의 벌레 속도 감소 값에 더함(중첩 가능)
        if (GameManager.Instance?.grid != null)
        {
            GameManager.Instance.grid.AddBugSpeedDcreasement(reducePercent);
            Debug.Log($"{DisplayName} 구매: 벌레 이동속도 {reducePercent * 100f}% 영구 감소 적용");
        }
        else
        {
            Debug.LogWarning("BugFan: GameManager.Instance.grid가 없습니다.");
        }
    }

    public override void Cancel(ShopContext ctx) { }
}
