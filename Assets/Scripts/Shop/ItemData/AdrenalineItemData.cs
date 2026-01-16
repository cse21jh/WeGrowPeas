// Assets/Scripts/Shop/Items/ItemData_Adrenaline.cs
using UnityEngine;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "Item_Adrenaline", menuName = "Shop/Item/Adrenaline")]
public class ItemData_Adrenaline : ItemData
{
    [Header("Effect")]
    [Min(1)] public int durationDays = 6;  // “상점 빈도” 따를 거면 Commit에서 스케일
    [Tooltip("스폰 간격 배수 (0.5 = 2배 빈도)")]
    [Range(0.05f, 2f)] public float spawnIntervalMul = 0.5f; // = 2x frequency
    [Tooltip("교배 단계 시간 배수 (2 = 2배 길게)")]
    [Range(0.1f, 5f)] public float breedingDurationMul = 2f;
    [Tooltip("교배 가능 횟수 배수 (2 = 2배)")]
    [Range(1f, 5f)] public float breedingAttemptsMul = 2f;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 2;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "아드레날린";
        if (Price <= 0) Price = 10000;
    }

    public override bool IsRotationUnlockOk(ShopContext ctx)
    {
        int stage = GameManager.Instance.stage;
        return (stage + 1) >= unlockStageDay;
    }
    public override int GetRotationWeight(ShopContext ctx) => rotationWeight;

    public override bool CanPurchase(ShopContext ctx, out string reason)
    { reason = null; return true; }

    public override void StartEffect(ShopContext ctx, Action onReady, Action<string> onError)
    { onReady?.Invoke(); }

    public override void Commit(ShopContext ctx)
    {
        // “상점 빈도를 따라 6일” 해석을 정확히 하려면 아래처럼 주기 스케일:
        int days = durationDays;
        // var sm = ShopManager.Instance;
        // if (sm) days *= Mathf.Max(1, sm.ShopOpenDay);

        // 각 효과별로 기존 모드가 있으면 기간 연장, 없으면 새로 추가
        ExtendOrAddMod("Adrenaline_Spawn", StatId.BugSpawnIntervalMul, spawnIntervalMul, days);
        ExtendOrAddMod("Adrenaline_BreedTime", StatId.BreedingPhaseDurationMul, breedingDurationMul, days);
        ExtendOrAddMod("Adrenaline_BreedCount", StatId.BreedingAttemptsMul, breedingAttemptsMul, days);

        ctx.ShowInfo?.Invoke($"{DisplayName} 발동: {days}일간 스폰×{(1f / spawnIntervalMul):0.#}, 교배시간×{breedingDurationMul:0.#}, 교배횟수×{breedingAttemptsMul:0.#}");
    }

    private void ExtendOrAddMod(string sourceTag, StatId stat, float multiplier, int days)
    {
        var existingMod = ModManager.Instance.Mods.FirstOrDefault(m => m.sourceTag == sourceTag);
        
        if (existingMod != null)
        {
            // 기존 모드가 있으면 기간을 더함
            existingMod.expireDay += days;
        }
        else
        {
            // 새로운 모드 추가
            ModManager.Instance.AddTimedMultiplier(stat, -1, multiplier, days, sourceTag);
        }
    }

    public override void Cancel(ShopContext ctx) { }
}
