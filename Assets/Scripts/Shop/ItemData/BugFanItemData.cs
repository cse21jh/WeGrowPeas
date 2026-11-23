using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Bug Fan (벌레 방해용 선풍기)", fileName = "BugFanItemData")]
public class BugFanItemData : ItemData
{

    [Header("Effect")]
    [Range(0f, 1f)] public float reducePercent = 0.1f; // 10%

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 11;
    [Min(0)] public int rotationWeight = 8;

    private void OnEnable()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        if (Price <= 0) Price = 500;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = ctx.Stage;
        return (stage + 1) >= unlockStageDay;
    }

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null)
        {
            reason = Loc.T("shop.error.no_game_state");
            return false;
        }

        if (!ctx.Shop.PurchaseHistory.TryGetValue(PurchaseKey, out int bought))
            bought = 0;

        if (bought >= MaxPurchaseCount)
        {
            reason = Loc.T("shop.error.limit_reached");
            return false;
        }

        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx) => ctx.Modifiers.AddBugSpeedDcreasement(reducePercent);
}
