// Assets/Scripts/Shop/Items/ItemData_Adrenaline.cs
using UnityEngine;
using System;
using System.Linq;

[CreateAssetMenu(menuName = "Shop/Items/Adrenaline (아드레날린)", fileName = "AdrenalineItemData")]
public class AdrenalineItemData : ItemData
{
    [Header("Effect")]
    [Min(1)] public int durationDays = 6;  // ������ �󵵡� ���� �Ÿ� Commit���� ������
    [Tooltip("���� ���� ��� (0.5 = 2�� ��)")]
    [Range(0.05f, 2f)] public float spawnIntervalMul = 0.5f; // = 2x frequency
    [Tooltip("���� �ܰ� �ð� ��� (2 = 2�� ���)")]
    [Range(0.1f, 5f)] public float breedingDurationMul = 2f;
    [Tooltip("���� ���� Ƚ�� ��� (2 = 2��)")]
    [Range(1f, 5f)] public float breedingAttemptsMul = 2f;

    [Header("Rotation")]
    [Min(1)] public int unlockStageDay = 1;
    [Min(0)] public int rotationWeight = 2;

    private void OnValidate()
    {
        FlowType = ShopFlowType.Instant;
        IsStackable = false;
        OnePerShopIfNotStackable = true;

        if (string.IsNullOrEmpty(DisplayName)) DisplayName = "�Ƶ巹����";
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
        // ������ �󵵸� ���� 6�ϡ� �ؼ��� ��Ȯ�� �Ϸ��� �Ʒ�ó�� �ֱ� ������:
        int days = durationDays;
        // var sm = ShopManager.Instance;
        // if (sm) days *= Mathf.Max(1, sm.ShopOpenDay);

        // �� ȿ������ ���� ��尡 ������ �Ⱓ ����, ������ ���� �߰�
        ExtendOrAddMod("Adrenaline_Spawn", StatId.BugSpawnIntervalMul, spawnIntervalMul, days);
        ExtendOrAddMod("Adrenaline_BreedTime", StatId.BreedingPhaseDurationMul, breedingDurationMul, days);
        ExtendOrAddMod("Adrenaline_BreedCount", StatId.BreedingAttemptsMul, breedingAttemptsMul, days);

        ctx.ShowInfo?.Invoke($"{DisplayName} �ߵ�: {days}�ϰ� ������{(1f / spawnIntervalMul):0.#}, ����ð���{breedingDurationMul:0.#}, ����Ƚ����{breedingAttemptsMul:0.#}");
    }

    private void ExtendOrAddMod(string sourceTag, StatId stat, float multiplier, int days)
    {
        var existingMod = ModManager.Instance.Mods.FirstOrDefault(m => m.sourceTag == sourceTag);
        
        if (existingMod != null)
        {
            // ���� ��尡 ������ �Ⱓ�� ����
            existingMod.expireDay += days;
        }
        else
        {
            // ���ο� ��� �߰�
            ModManager.Instance.AddTimedMultiplier(stat, -1, multiplier, days, sourceTag);
        }
    }

    public override void Cancel(ShopContext ctx) { }
}
