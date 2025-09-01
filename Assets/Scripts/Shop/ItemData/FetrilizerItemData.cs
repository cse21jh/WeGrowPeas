using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Item_Fertilizer", menuName = "Shop/Item/Fertilizer")]
public class ItemData_Fertilizer : ItemData
{
    [Header("Fertilizer")]
    public WaveType targetWave = WaveType.Pest;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1; // 웨이브별로 설정(자연사=0 → stage+1>=0 항상 true)
    [Min(0)] public int rotationWeight = 2;

    private int? selectedIdx;

    private void OnValidate()
    {
        FlowType = ShopFlowType.PlaceOnTile;
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (string.IsNullOrEmpty(DisplayName))
            DisplayName = $"전용 비료: {targetWave}";
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
        if (!ctx.Grid.HasEmptyFetrilizerGrid())
        {
            reason = "설치할 수 있는 빈칸이 없습니다";
            return false;
        }
        reason = null;
        return true;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // PlaceOnTile: ShopUI가 ValidatePosition/SetPlacedPosition을 호출하고 onReady를 불러줄 것.
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!selectedIdx.HasValue)
        {
            Debug.LogError("[Fertilizer] Selected index is null.");
            return;
        }
        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[Fertilizer] Grid not found.");
            return;
        }
        if (!g.TryPlaceFertilizer(selectedIdx.Value, targetWave))
        {
            ctx.ShowError?.Invoke("이미 다른 전용 비료가 있습니다");
            return;
        }
        ctx.ShowInfo?.Invoke($"{DisplayName} 설치 완료 (idx={selectedIdx.Value})");
        selectedIdx = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        selectedIdx = null;
    }

    // === PlaceOnTile 훅 ===
    public override bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason)
    {
        reason = null;
        var g = ctx.Grid;
        if (!g) { reason = "Grid 없음"; return false; }

        int? idx = g.GetGridIndexFromPosition(worldPos);
        if (!idx.HasValue) { reason = "토양이 아닙니다"; return false; }

        if (g.HasFertilizerAt(idx.Value)) { reason = "이미 전용 비료가 있습니다"; return false; }

        return true;
    }

    public override void SetPlacedPosition(Vector3 worldPos)
    {
        var g = FindAnyObjectByType<Grid>();
        int? idx = g ? g.GetGridIndexFromPosition(worldPos) : null;
        selectedIdx = idx;
    }
}