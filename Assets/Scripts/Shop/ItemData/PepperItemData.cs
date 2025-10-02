using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/ChiliPepper (고추)", fileName = "ChiliPepperItemData")]
public class ChiliPepperItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "고추";
        if (Price <= 0) Price = 1500;

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
            reason = "Grid 참조가 없습니다 (ShopContext.Grid 주입 필요)";
            return false;
        }
        if (!ctx.Grid.HasEmptyGrid())
        {
            reason = "설치할 수 있는 빈칸이 없습니다";
            return false;
        }
        reason = null;
        return true;
    }

    // 배치 모드 진입: 별도 준비 없음
    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason)
    {
        reason = null;
        if (ctx == null || ctx.Grid == null)
        {
            reason = "Grid 참조가 없습니다";
            return false;
        }

        // 스크린 좌표 → 그리드 인덱스
        int? idx = ctx.Grid.GetGridIndexFromPosition(pos);
        if (!idx.HasValue)
        {
            reason = "유효한 토양이 아닙니다";
            return false;
        }

        // 빈 칸인지 확인
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "이미 식물이 있는 칸입니다";
            return false;
        }

        // 문제 없으면 확정 후보 저장
        pendingIndex = idx.Value;
        return true;
    }
    public override void SetPlacedPosition(Vector3 worldOrScreenPos) { /* no-op */ }

    public override void Commit(ShopContext ctx)
    {
        if (ctx == null || ctx.Grid == null)
        {
            ctx?.ShowError?.Invoke("Grid 참조가 없습니다");
            return;
        }
        if (!pendingIndex.HasValue)
        {
            ctx.ShowError?.Invoke("배치 위치가 유효하지 않습니다");
            return;
        }

        // 실제 배치
        ctx.Grid.AddChiliPepper(pendingIndex.Value);

        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
