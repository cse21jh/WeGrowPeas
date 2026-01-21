using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Bug Spray (벌레 스프레이)", fileName = "BugSprayItemData")]
public class BugSprayItemData : ItemData
{
    [Header("Effect")]
    [Range(0f, 1f)] public float reducePercent = 0.5f;
    [Min(1)] public int durationDays = 6;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 8;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        Rarity = ItemRarity.Common;
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "벌레 스프레이";
        if (Price <= 0) Price = 500;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    { reason = null; return true; }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    { onReady?.Invoke(); }

    public override void Commit(ShopContext ctx)
    {
        ModManager.Instance.AddTimedMultiplier(StatId.BugSpeedMul, param: -1, multiplier: 1-reducePercent, durationDays: durationDays, sourceTag: "BugSpray");
    }

    public override void Cancel(ShopContext ctx) { }
}
