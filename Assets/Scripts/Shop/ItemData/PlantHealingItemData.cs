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
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "식물 치료제";
        if (string.IsNullOrEmpty(Description)) Description = "선택한 웨이브에 대응할 수 있는 모든 식물을 치료하고, 동시에 더 잘 버틸 수 있도록 도와줍니다.";
        if (Price <= 0) Price = 3000;
        Rarity = ItemRarity.Special; // 특수 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        FlowType = ShopFlowType.Instant;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        // 등장하는 웨이브만 구매 가능
        int stage = GameManager.Instance.stage;
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
    public override string[] GetSelectableOptions()
        => WaveSchedule.GetSelectableWaveNames(GameManager.Instance != null ? GameManager.Instance.stage : 0);

    public override void SetSelectedOption(int index)
        => pendingWave = WaveSchedule.GetSelectableWaveAt(index, GameManager.Instance != null ? GameManager.Instance.stage : 0);

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        if (!pendingWave.HasValue)
        {
            onError?.Invoke("웨이브를 선택해주세요");
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
        foreach (var plant in ctx.Grid.plantGrid.Values)
        {
            var traits = plant.GetGeneticTrait();
            bool hasTraitForWave = traits.Any(t => t.traitType == (TraitType)targetWave);

            if (hasTraitForWave)
            {
                // 해당 웨이브에 대한 저항력을 최대로 회복하고, 추가로 10%p 증가
                for (int i = 0; i < traits.Count; i++)
                {
                    if ((int)traits[i].traitType == (int)targetWave)
                    {
                        // 최대 회복 (1.0f) + 추가 보너스 (10%p)
                        float newResistance = Mathf.Clamp(1.0f + bonusResistancePercent, 0.1f, 1.0f);
                        traits[i] = new GeneticTrait(traits[i].traitType, newResistance, traits[i].genetics, traits[i].additionalResistance);
                    }
                }
                plant.SetTrait(traits);
                healedCount++;
            }
            else
            {
                // 형질이 없어도 기본 저항력(0.1f)은 있으므로 최대 회복
                // 형질을 추가하지 않고, 저항력만 회복하는 경우는 없으므로
                // 형질이 없는 식물은 치료하지 않음
            }
        }

        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: {targetWave} 웨이브에 대한 {healedCount}개 식물 치료 완료");
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingWave = null;
    }
}
