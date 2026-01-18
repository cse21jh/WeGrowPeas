using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Shop/Items/Dedicated Fertilizer (전용 비료)", fileName = "FertilizerItemData")]
public class FertilizerItemData : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1; // 웨이브별 해금 시기
    [Min(0)] public int rotationWeight = 2;

    private int? selectedIdx;
    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        FlowType = ShopFlowType.PlaceOnTile;
        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음

        if (string.IsNullOrEmpty(DisplayName))
            DisplayName = "전용 비료";
        if (string.IsNullOrEmpty(Description))
            Description = "세로 한 줄의 토양에 비료를 뿌려 선택한 웨이브에서 잘 살아남을 수 있게 하고, 그 웨이브에 대한 저항력이 감소하지 않게 합니다.";
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

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // 형질 선택 UI를 재사용하여 웨이브 선택 UI 표시
        var selectionUI = FindAnyObjectByType<TraitSelectionUIController>();
        if (selectionUI == null)
        {
            onError?.Invoke("웨이브 선택 UI를 찾을 수 없습니다");
            return;
        }

        selectionUI.ShowWaveSelection(
            onConfirm: (selectedWave) => {
                pendingWave = selectedWave;
                // 웨이브 선택 후 위치 선택으로 진행
                onReady?.Invoke();
            },
            onCancel: () => {
                pendingWave = null;
                onError?.Invoke("구매 취소");
            },
            title: "전용 비료: 웨이브를 선택하세요"
        );
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
        ctx.ShowInfo?.Invoke($"{DisplayName} 배치 완료 (idx={selectedIdx.Value})");
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
