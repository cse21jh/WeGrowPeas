using UnityEngine;
using System;
using System.Linq;

[CreateAssetMenu(menuName = "Shop/Items/Adrenaline (아드레날린)", fileName = "AdrenalineItemData")]
public class AdrenalineItemData : ItemData
{
    [Header("Effect")]
    [Min(1)] public int durationDays = 6;
    [Tooltip("벌레 스폰 간격 배수 (0.5 = 2배 빠름)")]
    [Range(0.05f, 2f)] public float spawnIntervalMul = 0.5f;
    [Tooltip("교배 단계 시간 배수 (2 = 2배 길게)")]
    [Range(0.1f, 5f)] public float breedingDurationMul = 2f;
    [Tooltip("교배 시도 횟수 배수 (2 = 2배)")]
    [Range(1f, 5f)] public float breedingAttemptsMul = 2f;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 1;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;
        Rarity = ItemRarity.Legendary;

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
        int days = durationDays;

        ExtendOrAddMod("Adrenaline_Spawn", StatId.BugSpawnIntervalMul, spawnIntervalMul, days);
        ExtendOrAddMod("Adrenaline_BreedTime", StatId.BreedingPhaseDurationMul, breedingDurationMul, days);
        ExtendOrAddMod("Adrenaline_BreedCount", StatId.BreedingAttemptsMul, breedingAttemptsMul, days);

        ctx.ShowInfo?.Invoke($"{DisplayName} 적용: {days}일간 벌레{(1f / spawnIntervalMul):0.#}배, 교배시간{breedingDurationMul:0.#}배, 교배횟수{breedingAttemptsMul:0.#}배");
    }

    private void ExtendOrAddMod(string sourceTag, StatId stat, float multiplier, int days)
    {
        var existingMod = ModManager.Instance.Mods.FirstOrDefault(m => m.sourceTag == sourceTag);
        
        if (existingMod != null)
        {
            existingMod.expireDay += days;
        }
        else
        {
            ModManager.Instance.AddTimedMultiplier(stat, -1, multiplier, days, sourceTag);
        }
    }

    public override void Cancel(ShopContext ctx) { }
}
