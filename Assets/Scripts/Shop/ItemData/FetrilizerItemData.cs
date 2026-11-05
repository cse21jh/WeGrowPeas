using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Dedicated Fertilizer (전용 비료)", fileName = "FertilizerItemData")]
public class FertilizerItemData : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1; // 웨이브별 해금 시기
    [Min(0)] public int rotationWeight = 8;

    private int? selectedIdx;
    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        FlowType = ShopFlowType.PlaceOnTile;
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (Price <= 0) Price = 1000;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return stage >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

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
        onReady?.Invoke(); // 이후 위치(토양) 선택으로 진행
    }

    public override void Commit(ShopContext ctx)
    {
        if (!selectedIdx.HasValue)
        {
            Debug.LogError("[Fertilizer] Selected index is null.");
            return;
        }
        if (!pendingWave.HasValue)
        {
            Debug.LogError("[Fertilizer] Selected wave is null.");
            return;
        }
        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[Fertilizer] Grid not found.");
            return;
        }
        if (!g.TryPlaceFertilizer(selectedIdx.Value, pendingWave.Value))
        {
            ctx.ShowError?.Invoke("이미 비료가 있어 뿌릴 수 없습니다!");
            return;
        }
        ctx.ShowInfo?.Invoke($"{LocalizedName} 배치 완료 (idx={selectedIdx.Value})");
        selectedIdx = null;
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        selectedIdx = null;
        pendingWave = null;
    }

    // === PlaceOnTile 타입 ===
    public override bool ValidatePosition(ShopContext ctx, Vector3 worldPos, out string reason)
    {
        reason = null;
        var g = ctx.Grid;
        if (!g) { reason = "Grid 없음"; return false; }

        int? idx = g.GetGridIndexFromPosition(worldPos);
        if (!idx.HasValue) { reason = "유효한 위치가 아닙니다"; return false; }

        if (g.HasFertilizerAt(idx.Value)) { reason = "이미 비료가 있어 뿌릴 수 없습니다!"; return false; }

        return true;
    }

    public override void SetPlacedPosition(Vector3 worldPos)
    {
        var g = FindAnyObjectByType<Grid>();
        int? idx = g ? g.GetGridIndexFromPosition(worldPos) : null;
        selectedIdx = idx;
    }
}
