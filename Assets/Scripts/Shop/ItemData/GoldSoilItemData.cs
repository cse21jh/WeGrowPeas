using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Items/Gold Soil (황금 비료)", fileName = "GoldSoilItemData")]
public class GoldSoilItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 1;

    // 배치 확정 시 사용할 그리드 인덱스
    private int? pendingIndex;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "황금 비료";
        if (string.IsNullOrEmpty(Description)) Description = "원하는 토양 1칸에 황금 비료를 추가합니다. 황금 비료에 심은 식물은 옮길 수 없지만, 특별한 일이 발생합니다.";
        if (Price <= 0) Price = 5000;

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        FlowType = ShopFlowType.PlaceOnTile;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx) => true;
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, System.Action onReady, System.Action<string> onError)
    {
        onReady?.Invoke();
    }

    public override bool ValidatePosition(ShopContext ctx, Vector3 pos, out string reason)
    {
        // 화면 좌표를 그리드 인덱스로 변환
        if (!TryGetGridIndexFromPosition(ctx, pos, out int? idx, out reason))
            return false;

        // 이미 황금 비료가 있는 칸인지 확인
        if (ctx.Grid.HasGoldSoil(idx.Value))
        {
            reason = "이미 황금 비료가 있는 칸입니다";
            return false;
        }

        // 식물이 있는 칸인지 확인
        if (ctx.Grid.plantGrid.ContainsKey(idx.Value))
        {
            reason = "식물이 있는 칸에는 황금 비료를 뿌릴 수 없습니다";
            return false;
        }

        pendingIndex = idx.Value;
        return true;
    }

    public override void SetPlacedPosition(Vector3 worldPos)
    {
        // ValidatePosition에서 이미 처리됨
    }

    public override void Commit(ShopContext ctx)
    {
        if (!ValidateGrid(ctx, out _))
            return;
        if (!ValidatePendingIndex(pendingIndex, ctx, out _))
            return;

        if (!ctx.Grid.TryPlaceGoldSoil(pendingIndex.Value))
        {
            ctx.ShowError?.Invoke("황금 비료를 뿌릴 수 없습니다");
            return;
        }

        ctx.ShowInfo?.Invoke($"{DisplayName} 배치 완료");
        pendingIndex = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingIndex = null;
    }
}
