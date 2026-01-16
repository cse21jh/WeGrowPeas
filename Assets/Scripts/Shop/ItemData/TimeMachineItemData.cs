using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Item_TimeMachine", menuName = "Shop/Item/Time Machine")]
public class ItemData_TimeMachine : ItemData
{
    [Header("Effect")]
    [Min(1)] public int skipCount = 1;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 3;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = true;
        OnePerShopIfNotStackable = false;
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "타임 머신";
        if (Price <= 0) Price = 2000;
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
        GameManager.Instance.enemyController.AddWaveSkipCount(skipCount);
    }

    public override void Cancel(ShopContext ctx) { }
}
