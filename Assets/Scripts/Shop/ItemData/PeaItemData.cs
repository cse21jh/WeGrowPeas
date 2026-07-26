using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea (식물 모종)", fileName = "PeaItemData")]
public class PeaItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private List<GeneticTrait> pendingTraits;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "식물 모종";
        if (Price <= 0) Price = 500;
        Rarity = ItemRarity.Rare; // 희귀 등급
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;  // 위치 선택 없이 즉시 설치
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        return CheckHasEmptyGrid(ctx, out reason);
    }

    // 형질은 상세 패널의 드롭다운으로 고른다. (별도 형질 선택 UI를 쓰지 않음)
    public override string[] GetSelectableOptions() => TraitOptions.GetNames(CurrentStage);

    public override void SetSelectedOption(int index)
    {
        pendingTraits = TraitOptions.BuildTraits(index, CurrentStage);
    }

    private static int CurrentStage => GameManager.Instance != null ? GameManager.Instance.stage : 0;

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        // 드롭다운에서 고른 형질을 그대로 사용. 선택이 없으면 Commit에서 기본 형질로 보정된다.
        onReady?.Invoke();
    }

    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason) 
    { 
        reason = null; 
        return true; 
    }

    public override void SetPlacedPosition(Vector3 worldPos) 
    { 
        // 위치 선택 없음 - 사용 안 함
    }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;

        Debug.Log($"[PeaItemData] Commit 호출됨. pendingTraits: {(pendingTraits == null ? "null" : pendingTraits.Count.ToString())}개");

        // 형질이 없거나 빈 리스트인 경우 기본 형질 1개 보장
        if (pendingTraits == null || pendingTraits.Count == 0)
        {
            Debug.LogWarning("[PeaItemData] Commit에서 형질이 없어 기본 형질을 사용합니다.");
            pendingTraits = new List<GeneticTrait>
            {
                new GeneticTrait(TraitType.NaturalDeath, Plant.GetResistanceBasedOnGenetics(TraitType.NaturalDeath, 1), 1, 0.0f)
            };
        }

        // 형질이 최소 1개 이상인지 확인 (안전장치)
        if (pendingTraits.Count == 0)
        {
            Debug.LogWarning("[PeaItemData] 형질이 0개입니다. 기본 형질을 추가합니다.");
            pendingTraits.Add(new GeneticTrait(TraitType.NaturalDeath, Plant.GetResistanceBasedOnGenetics(TraitType.NaturalDeath, 1), 1, 0.0f));
        }

        Debug.Log($"[PeaItemData] 최종 형질 {pendingTraits.Count}개로 AddPea 호출");
        foreach (var trait in pendingTraits)
        {
            Debug.Log($"[PeaItemData] - {trait.traitType}, genetics: {trait.genetics}, resistance: {trait.resistance}");
        }

        // grididx를 지정하지 않으면 Grid.AddPea가 자동으로 가장 빠른 빈 칸에 설치
        ctx.Grid.AddMovablePlant(pendingTraits);

        // 초기화
        pendingTraits = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingTraits = null;
    }
}
