using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Item_GeneExtractor", menuName = "Shop/Item/Gene Extractor")]
public class ItemData_GeneExtractor : ItemData
{
    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1; // 1일차부터 등장
    [Min(0)] public int rotationWeight = 8;

    private Plant selected; // 사용자가 선택한 기준 식물

    private void OnValidate()
    {
        FlowType = ShopFlowType.SelectExistingPlant; // 식물 선택 플로우
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "유전자 추출기";
        if (Price <= 0) Price = 1000;
    }

    // ── 로테이션 후보 조건/가중치 ─────────────────────────────
    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    // ── 구매 가능여부(미리) ───────────────────────────────────
    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        reason = null;
        var g = ctx.Grid;
        if (!g) { reason = "Grid 없음"; return false; }

        // 최소 1칸 이상 빈 칸이 있어야 의미 있음
        int maxSlots = g.maxCol * 4;
        int empty = maxSlots - g.plantGrid.Count;
        if (empty <= 0) { reason = "빈 칸이 없습니다"; return false; }

        // 선택 대상이 한 그루도 없다면 사용 불가
        if (g.plantGrid.Count <= 0) { reason = "선택할 식물이 없습니다"; return false; }

        return true;
    }

    // ── 선택 플로우 훅 ────────────────────────────────────────
    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // ShopUI가 BeginPlantSelection으로 유도해 줄 것 → 여기선 바로 ok
        onReady?.Invoke();
    }

    public override bool ValidateTarget(ShopContext ctx, Plant target, out string reason)
    {
        if (target == null) { reason = "식물을 선택하세요"; return false; }
        reason = null;
        return true;
    }

    public override void SetSelectedPlant(Plant plant)
    {
        selected = plant;
    }

    // ── 적용(확정) ────────────────────────────────────────────
    public override void Commit(ShopContext ctx)
    {
        if (!selected)
        {
            ctx.ShowError?.Invoke("선택된 식물이 없습니다");
            return;
        }

        var g = ctx.Grid;
        if (!g)
        {
            Debug.LogError("[GeneExtractor] Grid not found");
            return;
        }

        // 기준 유전 정보 복사
        List<GeneticTrait> genes = selected.GetGeneticTrait(); // Plant에 이미 존재

        // 생성 가능한 수 = min(3, 빈 칸 수)
        int maxSlots = g.maxCol * 4;
        int empty = maxSlots - g.plantGrid.Count;
        int toSpawn = Mathf.Clamp(3, 0, empty); // 빈칸이 0~2면 그만큼만

        int spawned = 0;
        for (int i = 0; i < toSpawn; i++)
        {
            // “무작위 식물”은 유전 세팅이 확실한 종으로 제한 (Pea/Peanut)
            // (Nepenthes/ChiliPepper는 SetTrait 흐름이 다를 수 있어요)
            if (UnityEngine.Random.Range(0, 2) == 0)
                g.AddPea(genes);
            else
                g.AddPeanut(genes);

            spawned++;
        }

        if (spawned > 0)
            ctx.ShowInfo?.Invoke($"{DisplayName}: {spawned}개 생성 완료");
        else
            ctx.ShowError?.Invoke("빈 칸이 없어 생성되지 않았습니다");

        // 정리
        selected = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        selected = null;
    }
}