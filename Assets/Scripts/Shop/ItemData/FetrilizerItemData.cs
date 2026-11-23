using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Dedicated Fertilizer (전용 비료)", fileName = "FertilizerItemData")]
public class FertilizerItemData : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1; // 웨이브별 해금 시기
    [Min(0)] public int rotationWeight = 8;

    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        FlowType = ShopFlowType.PlaceOnTile;
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (Price <= 0) Price = 1000;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = ctx.Stage;
        return stage >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        return true;
    }

    // 웨이브는 상세 패널의 드롭다운으로 고른다.
    public override string[] GetSelectableOptions(ShopContext ctx)
        => WaveSchedule.GetSelectableWaveNames(ctx.Stage, ctx.World.IsNight);

    public override void SetSelectedOption(ShopContext ctx, int index)
        => pendingWave = WaveSchedule.GetSelectableWaveAt(index, ctx.Stage, ctx.World.IsNight);

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        if (!pendingWave.HasValue)
        {
            onError?.Invoke(Loc.T("shop.error.select_wave"));
            return;
        }
        onReady?.Invoke(); // 이후 위치(토양) 선택으로 진행
    }

    public override void Commit(ShopContext ctx)
    {
        if (!pendingWave.HasValue || !TakeSelectedCell(ctx, out int cell)) return;

        if (!ctx.World.PlaceFertilizer(cell, pendingWave.Value))
        {
            ctx.ShowError?.Invoke(Loc.T("shop.error.fertilizer_exists"));
            return;
        }
        ctx.ShowInfo?.Invoke(Loc.T("shop.info.placed", ("name", LocalizedName)));
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        base.Cancel(ctx);
        pendingWave = null;
    }

    // === PlaceOnTile 타입 ===
    public override bool ValidateCell(ShopContext ctx, int cell, out string reason)
    {
        bool exists = ctx.Soil.HasFertilizerAt(cell);
        reason = exists ? Loc.T("shop.error.fertilizer_exists") : null;
        return !exists;
    }
}
