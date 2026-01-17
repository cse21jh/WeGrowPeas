using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Pea (완두콩)", fileName = "PeaItemData")]
public class PeaItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private List<GeneticTrait> pendingTraits;
    private int? pendingGridIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "완두콩";
        if (Price <= 0) Price = 500;
        
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        FlowType = ShopFlowType.PlaceOnTile;
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

        traitSelectionUI.ShowTraitSelection(
            onConfirm: (selectedTraits) => {
                pendingTraits = selectedTraits;
                onReady?.Invoke(); // 형질 선택 후 onReady 호출 → PlaceOnTile 플로우 진행
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
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 객체가 없습니다";
            return false;
        }

        // 화면 좌표로 그리드 인덱스
        int? idx = ctx.Grid.GetGridIndexFromPosition(pos);
        if (!idx.HasValue)
        {
            reason = "유효하지 않은 위치입니다";
            return false;
        }

        // 빈 칸인지 확인
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "이미 식물이 있는 칸입니다";
            return false;
        }

        // 배치 인덱스 저장
        pendingGridIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldPos) 
    { 
        // 이미 ValidatePosition에서 pendingGridIndex에 저장됨
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

        if (pendingGridIndex.HasValue)
        {
            ctx.Grid.AddPea(pendingTraits, pendingGridIndex.Value);
        }
        else
        {
            ctx.Grid.AddPea(pendingTraits);
        }

        // 초기화
        pendingTraits = null;
        pendingGridIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingTraits = null;
        pendingGridIndex = null;
    }
}
