using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/Gene Extractor (유전자 추출기)", fileName = "GeneExtractorItemData")]
public class GeneExtractorItemData : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 8;

    private Plant selected;

    private void OnValidate()
    {
        FlowType = ShopFlowType.SelectExistingPlant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        Rarity = ItemRarity.Common;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "유전자 추출기";
        if (Price <= 0) Price = 1000;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        var g = ctx.Grid;
        if (!g) { reason = "Grid 객체가 없습니다"; return false; }

        int maxSlots = g.maxCol * 4;
        int empty = maxSlots - g.plantGrid.Count;
        if (empty <= 0) { reason = "빈 칸이 없습니다"; return false; }

        if (g.plantGrid.Count <= 0) { reason = "선택할 수 있는 식물이 없습니다"; return false; }

        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason)
    {
        if (target == null) { reason = "식물을 선택해주세요"; return false; }
        reason = null;
        return true;
    }

    public override void SetSelectedPlant(Plant plant)
    {
        selected = plant;
    }

    public override void Commit(ShopContext ctx)
    {
        if (!selected)
        {
            ctx.ShowError?.Invoke("선택한 식물이 없습니다");
            return;
        }

        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[GeneExtractor] Grid not found");
            return;
        }

        List<GeneticTrait> genes = selected.GetGeneticTrait();

        int maxSlots = g.maxCol * 4;
        int empty = maxSlots - g.plantGrid.Count;
        int toSpawn = Mathf.Clamp(3, 0, empty);

        int spawned = 0;
        for (int i = 0; i < toSpawn; i++)
        {
            g.AddMovablePlant(genes);
            spawned++;
        }

        if (spawned > 0)
            ctx.ShowInfo?.Invoke($"{DisplayName}: {spawned}개 생성 완료");
        else
            ctx.ShowError?.Invoke("빈 칸이 부족하여 생성할 수 없습니다");

        selected = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        selected = null;
    }
}
