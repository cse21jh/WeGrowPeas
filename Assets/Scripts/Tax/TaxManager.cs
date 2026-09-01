using UnityEngine;

/// <summary>
/// 세금 납부 로직. 다음 납부 대상 세금일/금액을 알려주고, 납부(골드 차감)를 처리한다.
/// 5일마다 마감, 미납 시 진행 게이트/즉사 등은 후속 작업(여기선 "낼 수 있는 구조"만).
/// 씬에 TaxManager 오브젝트 하나 배치 필요(Singleton).
/// </summary>
public class TaxManager : Singleton<TaxManager>
{
    [SerializeField] private EconomyManager economy;

    [Tooltip("이미 납부 완료한 가장 높은 세금일. (저장 대상)")]
    [SerializeField] private int lastPaidTaxStage = 0;

    private EconomyManager Economy
    {
        get
        {
            if (economy == null) economy = FindAnyObjectByType<EconomyManager>();
            return economy;
        }
    }

    public int Interval => TaxSchedule.Interval;
    public int LastPaidTaxStage => lastPaidTaxStage;

    /// <summary>다음에 내야 할(미납) 세금일.</summary>
    public int DueTaxStage => lastPaidTaxStage + Interval;

    /// <summary>이번에 내야 할 금액.</summary>
    public int DueAmount => TaxSchedule.GetTaxAmount(DueTaxStage);

    /// <summary>해당 세금일이 이미 납부 완료됐는가.</summary>
    public bool IsPaidForStage(int taxStage) => taxStage <= lastPaidTaxStage;

    /// <summary>현재 스테이지 기준으로 이번 세금이 마감 도달(미납 상태)인가.</summary>
    public bool IsDueNow(int currentStage) => currentStage >= DueTaxStage;

    /// <summary>세금 마감(밤)이 지났는데 미납 상태인가 → 낮에 강제징수 대상.</summary>
    public bool HasOverdueTax(int currentStage)
    {
        return currentStage > DueTaxStage;
    }

    /// <summary>연체된 세금액(강제징수 금액).</summary>
    public int OverdueAmount => DueAmount;

    /// <summary>강제징수 처리(납부 완료로 간주하고 다음 세금일로 넘어감).</summary>
    public void MarkPaidForcibly() => lastPaidTaxStage = DueTaxStage;

    /// <summary>다음 회차(이번 것을 낸 뒤) 세금액. 미리보기용.</summary>
    public int NextAmount => TaxSchedule.GetTaxAmount(DueTaxStage + Interval);

    /// <summary>마감까지 남은 일수. 0이면 오늘이 마감, 음수면 연체.</summary>
    public int DaysLeft(int currentStage) => DueTaxStage - currentStage;

    /// <summary>현재 보유 골드.</summary>
    public int CurrentGold => Economy != null ? Economy.GetGold() : 0;

    /// <summary>이번 세금액 대비 보유 골드 비율(0~1). 납부 진행도 표시용.</summary>
    public float PayProgress
    {
        get
        {
            int due = DueAmount;
            if (due <= 0) return 1f;
            return Mathf.Clamp01((float)CurrentGold / due);
        }
    }

    /// <summary>지금 보유 골드로 이번 세금을 낼 수 있는가.</summary>
    public bool CanPayNow() => Economy != null && Economy.HasGold(DueAmount);

    /// <summary>이번 세금 납부 시도. 성공 시 골드 차감 + 납부 처리.</summary>
    public bool TryPay()
    {
        if (DueTaxStage == 40) return false;

        int taxStage = DueTaxStage;
        int amount = TaxSchedule.GetTaxAmount(taxStage);

        if (Economy == null || !Economy.HasGold(amount))
            return false;

        Economy.SpendGold(amount, GoldFeedbackReason.Tax);
        lastPaidTaxStage = taxStage;
        Debug.Log($"[Tax] {taxStage}일차 세금 {amount} 납부 완료. 다음 세금일 {DueTaxStage} ({DueAmount})");
        return true;
    }

    // ── 저장/로드 (SaveData 연동은 후속) ──────────────────────────────────────
    public void LoadFromSave(int paidTaxStage) => lastPaidTaxStage = paidTaxStage;
    public int GetSaveValue() => lastPaidTaxStage;
}
