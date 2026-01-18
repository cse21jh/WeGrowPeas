using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea (완두콩)", fileName = "PeaItemData")]
public class PeaItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private List<GeneticTrait> pendingTraits;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "완두콩";
        if (Price <= 0) Price = 500;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.Instant;  // 위치 선택 없이 즉시 설치
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 객체가 없습니다 (ShopContext.Grid 필요)";
            return false;
        }
        if (!ctx.Grid.HasEmptyGrid())
        {
            reason = "배치할 수 있는 공간이 없습니다";
            return false;
        }
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        // 형질 선택 UI 표시
        var traitSelectionUI = FindAnyObjectByType<TraitSelectionUIController>();
        if (traitSelectionUI == null)
        {
            onError?.Invoke("형질 선택 UI를 찾을 수 없습니다");
            return;
        }

        traitSelectionUI.ShowSingleTraitSelection(
            onConfirm: (selectedTraits) => {
                pendingTraits = selectedTraits;
                onReady?.Invoke(); // 형질 선택 후 onReady 호출 → Instant 플로우로 즉시 설치
            },
            onCancel: () => {
                pendingTraits = null;
                onError?.Invoke("구매 취소");
            }
        );
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
        if (ctx == null || ctx.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid 객체가 없습니다");
            return;
        }

        if (pendingTraits == null || pendingTraits.Count == 0)
        {
            // 형질이 선택되지 않은 경우 기본 형질 사용
            pendingTraits = new List<GeneticTrait>
            {
                new GeneticTrait(TraitType.NaturalDeath, 0.5f, 0, 0.0f)
            };
        }

        // grididx를 지정하지 않으면 Grid.AddPea가 자동으로 가장 빠른 빈 칸에 설치
        ctx.Grid.AddPea(pendingTraits);

        // 초기화
        pendingTraits = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingTraits = null;
    }
}
