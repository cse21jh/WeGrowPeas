using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/High Quality Plant (고품질 식물)", fileName = "HighQualityPlantItemData")]
public class HighQualityPlantItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "고품질 식물";
        if (string.IsNullOrEmpty(Description)) Description = "선택한 웨이브에 강한 식물을 1개 추가합니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        FlowType = ShopFlowType.Instant;
    }

    /// <summary>
    /// 완두콩 전용 아이템. 등장하는 웨이브만 구매 가능 (별도 새벽 해금 조건 없음)
    /// </summary>
    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        if (!IsCurrentPlant("완두콩")) return false;

        // 현재 웨이브가 None이 아니면 등장하는 웨이브가 있음
        if (GameManager.Instance == null || GameManager.Instance.enemyController == null)
            return false;

        var currentWave = GameManager.Instance.enemyController.CurrentWave;
        return currentWave != null && currentWave.WaveType != WaveType.None;
    }

    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    {
        if (!CheckHasEmptyGrid(ctx, out reason))
            return false;
        
        // 현재 등장하는 웨이브가 있는지 확인
        if (GameManager.Instance == null || GameManager.Instance.enemyController == null)
        {
            reason = "게임 상태를 확인할 수 없습니다";
            return false;
        }
        
        var currentWave = GameManager.Instance.enemyController.CurrentWave;
        if (currentWave == null || currentWave.WaveType == WaveType.None)
        {
            reason = "등장하는 웨이브가 없습니다";
            return false;
        }
        
        reason = null;
        return true;
    }

    // 상세 패널 드롭다운용. 이 아이템은 "현재 등장 중인 웨이브"만 대상이라 그 웨이브 하나만 노출한다.
    public override string[] GetSelectableOptions()
    {
        var wave = GameManager.Instance != null && GameManager.Instance.enemyController != null
            ? GameManager.Instance.enemyController.CurrentWave
            : null;
        if (wave == null || wave.WaveType == WaveType.None) return null;
        return new[] { WaveSchedule.GetWaveDisplayName(wave.WaveType) };
    }

    public override void SetSelectedOption(int index)
    {
        var wave = GameManager.Instance != null && GameManager.Instance.enemyController != null
            ? GameManager.Instance.enemyController.CurrentWave
            : null;
        pendingWave = (index == 0 && wave != null && wave.WaveType != WaveType.None)
            ? wave.WaveType
            : (WaveType?)null;
    }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        if (!pendingWave.HasValue)
        {
            onError?.Invoke("등장하는 웨이브가 없습니다");
            return;
        }
        onReady?.Invoke();
    }

    public override void Commit(ShopContext ctx)
    {
        if (!pendingWave.HasValue)
        {
            Debug.LogError("[HighQualityPlant] Selected wave is null.");
            return;
        }

        if (!ValidateGrid(ctx, out _))
            return;

        WaveType targetWave = pendingWave.Value;
        TraitType targetTrait = (TraitType)targetWave; // WaveType을 TraitType으로 변환
        
        // 유전자를 2로 설정한 형질 생성 (고품질 = 유리한 유전자)
        List<GeneticTrait> traits = new List<GeneticTrait>
        {
            new GeneticTrait(targetTrait, Plant.GetResistanceBasedOnGenetics(targetTrait, 2), 2, 0.0f) // genetics = 2 (유리한 유전자)
        };

        // 완두콩 추가 (자동으로 가장 빠른 빈 칸에 설치)
        ctx.Grid.AddMovablePlant(traits);

        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: {targetWave} 웨이브에 강한 완두콩 추가");
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingWave = null;
    }
}
