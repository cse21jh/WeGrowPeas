using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 국세청 앱(Renewal) 화면 컨트롤러.
/// 마감 기한 / 납부할 금액·진행 슬라이더 / 다음 납부 예상액 / 납부 버튼을 표시하고,
/// 실제 납부는 <see cref="TaxManager"/>에 위임한다.
/// </summary>
public class TaxCanvasController : MonoBehaviour
{
    [Header("Deadline Panel")]
    [SerializeField] private TMP_Text deadlineText;   // "5일차 밤"
    [SerializeField] private TMP_Text leftDayText;    // "4일 남음"

    [Header("Amount Panel")]
    [SerializeField] private TMP_Text amountText;             // "3000 G"
    [SerializeField] private TMP_Text amountPercentageText;   // "73%"
    [SerializeField] private Slider amountSlider;             // 보유 골드 / 납부액
    [SerializeField] private TMP_Text currentPayText;         // 슬라이더 마커 위 현재 보유액

    [Header("Next Amount Panel")]
    [SerializeField] private TMP_Text nextAmountText;         // "6000 G"
    [Tooltip("납부 진행도 구간별 표정 (0~25%, 25~50%, 50~75%, 75~100%)")]
    [SerializeField] private Image gradeIcon;
    [SerializeField] private Sprite[] gradeSprites = new Sprite[4];

    [Header("Pay Panel")]
    [SerializeField] private GameObject payBtn_Payable;       // 납부 가능
    [SerializeField] private GameObject payBtn_NotPayable;    // 납부 불가
    [Tooltip("두 버튼 각각의 '현재 보유액' 텍스트")]
    [SerializeField] private TMP_Text[] currentGoldTexts;

    private void OnEnable()
    {
        // 골드가 바뀌면(식물 판매 등) 즉시 반영
        GameEvents.OnGoldChanged += OnGoldChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged -= OnGoldChanged;
    }

    private void OnGoldChanged(int _) => Refresh();

    /// <summary>화면 갱신. 앱을 열 때 / 납부 후 호출.</summary>
    public void Refresh()
    {
        var tax = TaxManager.Instance;
        if (tax == null) return;

        int stage = GameManager.Instance != null ? GameManager.Instance.stage : 0;
        int due = tax.DueAmount;
        int gold = tax.CurrentGold;

        // ── 마감 기한 ──
        if (deadlineText != null) deadlineText.text = $"{tax.DueTaxStage}일차 밤";
        if (leftDayText != null)
        {
            int left = tax.DaysLeft(stage);
            leftDayText.text = left > 0 ? $"{left}일 남음"
                             : left == 0 ? "오늘 마감"
                             : "연체";
        }

        // ── 납부할 금액 / 진행도 ──
        if (amountText != null) amountText.text = $"{due} G";

        float progress = tax.PayProgress;
        if (amountSlider != null) amountSlider.value = progress; // 0~1 (Slider Min0/Max1 기준)
        if (amountPercentageText != null) amountPercentageText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        if (currentPayText != null) currentPayText.text = $"{gold:N0} G";

        // ── 다음 납부 예상액 + 표정 ──
        if (nextAmountText != null) nextAmountText.text = $"{tax.NextAmount} G";
        UpdateGradeIcon(progress);

        // ── 납부 버튼 ──
        bool canPay = tax.CanPayNow();
        if (payBtn_Payable != null) payBtn_Payable.SetActive(canPay);
        if (payBtn_NotPayable != null) payBtn_NotPayable.SetActive(!canPay);

        if (currentGoldTexts != null)
            foreach (var t in currentGoldTexts)
                if (t != null) t.text = $"현재 보유액 {gold:N0} G";
    }

    /// <summary>납부 진행도(0~1)에 따라 표정 아이콘 교체. 0~25 / 25~50 / 50~75 / 75~100.</summary>
    private void UpdateGradeIcon(float progress)
    {
        if (gradeIcon == null || gradeSprites == null || gradeSprites.Length == 0) return;

        int idx = Mathf.Clamp(Mathf.FloorToInt(progress * 4f), 0, gradeSprites.Length - 1);
        if (gradeSprites[idx] != null)
        {
            gradeIcon.sprite = gradeSprites[idx];
            gradeIcon.enabled = true;
        }
    }

    /// <summary>납부 버튼(PayBtn_Payable) onClick에 연결.</summary>
    public void OnClickPay()
    {
        var tax = TaxManager.Instance;
        if (tax == null) return;

        PhoneManager.Instance?.PhoneTouchEffect();

        if (tax.TryPay())
        {
            SoundManager.Instance?.PlayEffect("Button");

            // 국세청 앱 알람 해제
            PhoneManager.Instance?.UpdateAppAlarmState(AppKey.Tax, AlarmState.None);

            PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
            {
                title = "국세청",
                message = "세금 납부가 완료되었습니다.",
                duration = 3f
            });

            GameEvents.RequestSaveGame();
        }
        else
        {
            PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
            {
                title = "국세청",
                message = "골드가 부족합니다. 식물을 팔아 마련하세요.",
                duration = 3.5f
            });
        }

        Refresh();
    }
}
