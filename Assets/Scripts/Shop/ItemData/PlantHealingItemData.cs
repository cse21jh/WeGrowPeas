using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/Plant Healing (식물 치료제)", fileName = "PlantHealingItemData")]
public class PlantHealingItemData : ItemData
{
    [Header("Effect")]
    [SerializeField] private float bonusResistancePercent = 0.10f; // 형질 있을 때 추가로 10%p 증가

    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        if (Price <= 0) Price = 3000;

        IsStackable = false;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        // 등장하는 웨이브만 구매 가능
        int stage = ctx.Stage;
        // 최소 1웨이브는 해금되어 있어야 함
        return stage >= 1;
    }

    public override int GetRotationWeight(ShopContext ctx) => 2;

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
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!pendingWave.HasValue)
        {
            Debug.LogError("[PlantHealing] Selected wave is null.");
            return;
        }

        if (!ValidateGrid(ctx, out _))
            return;

        WaveType targetWave = pendingWave.Value;
        int healedCount = 0;

        // 모든 식물을 순회하며 해당 웨이브에 대한 저항력 회복
        foreach (PlantState plant in ctx.Board.StatesInOrder())
        {
            if (!plant.HasTrait(targetWave)) continue;

            List<GeneticTrait> traits = plant.Traits;
            for (int i = 0; i < traits.Count; i++)
            {
                if ((int)traits[i].traitType != (int)targetWave) continue;
                // 저항력을 100%로 설정 + 추가 (상한 100%)
                float healed = Mathf.Clamp(1.0f + bonusResistancePercent, 0.1f, 1.0f);
                traits[i] = new GeneticTrait(traits[i].traitType, healed, traits[i].genetics, traits[i].additionalResistance);
            }
            ctx.World.RefreshTraits(plant);
            healedCount++;
        }

        ctx.ShowInfo?.Invoke(Loc.T("shop.info.healed", ("name", LocalizedName), ("wave", WavePalette.GetName(targetWave)), ("count", healedCount)));
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        base.Cancel(ctx);
        pendingWave = null;
    }
}
