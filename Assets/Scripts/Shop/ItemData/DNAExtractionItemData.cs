using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/DNA Extraction (DNA 추출)", fileName = "DNAExtractionItemData")]
public class DNAExtractionItemData : ItemData
{
    [Header("Effect")]
    [Min(1)] public int copyCount = 3;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 8;

    private int? selectedIdx;

    private void OnValidate()
    {
        FlowType = ShopFlowType.PlaceOnTile;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        Rarity = ItemRarity.Common;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "DNA 추출";
        if (Price <= 0) Price = 1000;
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
        if (!selectedIdx.HasValue)
        {
            Debug.LogError("[DNAExtraction] Selected index is null.");
            return;
        }
        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[DNAExtraction] Grid not found.");
            return;
        }
        Plant plant = g.plantGrid[selectedIdx.Value];

        List<GeneticTrait> trait;
        for (int i = 0; i < copyCount; i++)
        {
            trait = new List<GeneticTrait>();
            foreach(var r in plant.GetGeneticTrait())
            {
                int genetics = UnityEngine.Random.Range(0, 3);
                if(r.traitType == TraitType.Pest)
                    trait.Add(new GeneticTrait(r.traitType, plant.GetResistanceBasedOnGenetics(r.traitType, genetics), genetics, g.GetAdditionalPestResistance()));
                else
                    trait.Add(new GeneticTrait(r.traitType, plant.GetResistanceBasedOnGenetics(r.traitType, genetics), genetics, 0.0f));
            }

            if (plant.GetType() == typeof(Pea))
                g.AddPea(trait);
            else if (plant.GetType() == typeof(Peanut))
                g.AddPeanut(trait);
            else
                Debug.Log("식물 타입 오류");
        }

        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: 선택한 식물의 유전자를 가진 식물을 3개 추가했습니다");
    }

    public override void Cancel(ShopContext ctx)
    {
        selectedIdx = null;
    }

    public override bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason)
    {
        reason = null;
        var g = ctx.Grid;
        if (!g) { reason = "Grid 객체가 없습니다"; return false; }

        int? idx = g.GetGridIndexFromPosition(worldPos);
        if (!idx.HasValue) { reason = "유효한 위치가 아닙니다"; return false; }

        if (!g.HasBreedablePlantAt(idx.Value)) { reason = "교배 가능한 식물이 없습니다"; return false; }

        return true;
    }

    public override void SetPlacedPosition(Vector3 worldPos)
    {
        var g = FindAnyObjectByType<Grid>();
        int? idx = g ? g.GetGridIndexFromPosition(worldPos) : null;
        selectedIdx = idx;
    }
}
