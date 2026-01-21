using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Shop/Items/High Quality Plant (고품질 완두콩)", fileName = "HighQualityPlantItemData")]
public class HighQualityPlantItemData : ItemData
{
    [Header("Rotation")]
    [Min(0)] public int rotationWeight = 4;

    private WaveType? pendingWave = null;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "고품질 완두콩";
        if (string.IsNullOrEmpty(Description)) Description = "선택한 웨이브에 강한 완두콩을 1개 추가합니다.";
        if (Price <= 0) Price = 1500;
        Rarity = ItemRarity.Rare; // 희귀 등급

        IsStackable = false;
        InitialStock = 1;
        OnePerShopIfNotStackable = true;
        MaxPurchaseCount = -1; // 최대 구매 제한 없음
        FlowType = ShopFlowType.Instant;
    }

    /// <summary>
    /// 등장하는 웨이브만 구매 가능
    /// </summary>
    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
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

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    {
        // 웨이브 선택 UI 표시 (현재 등장하는 웨이브만 표시)
        if (TraitSelectionUIController.Instance == null)
        {
            onError?.Invoke("웨이브 선택 UI를 찾을 수 없습니다");
            return;
        }

        // 현재 등장하는 웨이브만 필터링하여 표시
        // ShowWaveSelection은 해금된 웨이브만 표시하므로, 
        // 현재 웨이브가 등장 중인지 확인은 TraitSelectionUIController에서 처리
        
        TraitSelectionUIController.Instance.ShowWaveSelection(
            onConfirm: (selectedWave) => {
                // 현재 등장하는 웨이브인지 확인
                if (GameManager.Instance != null && GameManager.Instance.enemyController != null)
                {
                    var currentWave = GameManager.Instance.enemyController.CurrentWave;
                    if (currentWave != null && currentWave.WaveType == selectedWave)
                    {
                        pendingWave = selectedWave;
                        onReady?.Invoke();
                    }
                    else
                    {
                        onError?.Invoke("현재 등장하는 웨이브만 선택할 수 있습니다");
                    }
                }
                else
                {
                    onError?.Invoke("게임 상태를 확인할 수 없습니다");
                }
            },
            onCancel: () => {
                pendingWave = null;
                onError?.Invoke("구매 취소");
            },
            title: "고품질 식물: 등장하는 웨이브를 선택하세요"
        );
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
            new GeneticTrait(targetTrait, 0.8f, 2, 0.0f) // genetics = 2 (유리한 유전자)
        };

        // 완두콩 추가 (자동으로 가장 빠른 빈 칸에 설치)
        ctx.Grid.AddPea(traits);

        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: {targetWave} 웨이브에 강한 완두콩 추가");
        pendingWave = null;
    }

    public override void Cancel(ShopContext ctx)
    {
        pendingWave = null;
    }
}
