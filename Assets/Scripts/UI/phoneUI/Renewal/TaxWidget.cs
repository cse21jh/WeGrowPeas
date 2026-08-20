using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈 화면 국세청 위젯. 납부 기한(D-n) / 납부 가능 비율 / 진행 바를 <see cref="TaxManager"/>에 연동한다.
///
/// 값과 문구 규칙은 세금 앱(<see cref="TaxCanvasController"/>)과 맞춰 둔다.
/// 위젯이 켜질 때, 골드가 바뀔 때, 하루가 지날 때 갱신한다.
/// </summary>
public class TaxWidget : MonoBehaviour
{
    [Header("Widget_Tax 프리팹의 오브젝트를 연결")]
    [Tooltip("tax_ddayText — \"세금 납부 기한 D - 4\"")]
    [SerializeField] private TMP_Text ddayText;

    [Tooltip("tax_currentMoneyText — \"현재 납부 가능 17%\"")]
    [SerializeField] private TMP_Text payableText;

    [Tooltip("tax_Slider — 보유 골드 / 납부액 비율")]
    [SerializeField] private Slider progressSlider;

    [Tooltip("tax_Slider > Fill — 비율에 따라 색이 바뀐다")]
    [SerializeField] private Image sliderFill;

    [Header("알람")]
    [Tooltip("우상단 알림 점. 국세청 앱 아이콘의 red dot과 같은 조건으로 켜진다.")]
    [SerializeField] private GameObject alarmDot;

    [Header("슬라이더 색")]
    [Tooltip("납부 가능 비율 구간별 색. 세금 앱의 표정 아이콘과 같은 4구간(0~25/25~50/50~75/75~100%)을 쓴다.")]
    [SerializeField]
    private Color[] fillColors =
    {
        new Color(0.85f, 0.30f, 0.25f), // 빨강 — 한참 모자람
        new Color(0.90f, 0.55f, 0.20f), // 주황
        new Color(0.85f, 0.78f, 0.25f), // 노랑
        new Color(0.45f, 0.75f, 0.30f), // 초록 — 낼 수 있음
    };

    private void OnEnable()
    {
        // 골드가 바뀌면 납부 가능 비율이, 날이 바뀌면 기한(D-n)이 달라진다.
        // OnQuestDayPassed는 stage가 오르기 전에 와서 D-day가 하루 밀리므로 OnDayStarted를 쓴다.
        GameEvents.OnGoldChanged += OnGoldChanged;
        GameEvents.OnDayStarted += Refresh;
        PhoneManager.OnAppAlarmChanged += OnAppAlarmChanged;

        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged -= OnGoldChanged;
        GameEvents.OnDayStarted -= Refresh;
        PhoneManager.OnAppAlarmChanged -= OnAppAlarmChanged;
    }

    private void OnGoldChanged(int _) => Refresh();

    private void OnAppAlarmChanged(AppKey key, AlarmState _)
    {
        if (key == AppKey.Tax) Refresh();
    }

    /// <summary>위젯 갱신. 납부 후처럼 바깥에서 즉시 갱신하고 싶을 때도 부를 수 있다.</summary>
    public void Refresh()
    {
        var tax = TaxManager.Instance;
        if (tax == null) return;

        int stage = GameManager.Instance != null ? GameManager.Instance.stage : 0;

        // 40일차 세금은 걷지 않는다 (세금 앱과 같은 규칙).
        bool noTax = tax.DueTaxStage == 40;

        if (ddayText != null)
        {
            int left = tax.DaysLeft(stage);
            ddayText.text = noTax ? "세금 납부 없음"
                          : left > 0 ? $"세금 납부 기한 D - {left}"
                          : left == 0 ? "세금 납부 기한 D - DAY"
                          : "세금 연체";
        }

        float progress = noTax ? 0f : tax.PayProgress; // 0~1

        if (payableText != null)
            payableText.text = $"현재 납부 가능 {Mathf.RoundToInt(progress * 100f)}%";

        // 슬라이더 min/max가 0~1이든 0~100이든 맞게 들어가도록 환산한다.
        if (progressSlider != null)
            progressSlider.value = Mathf.Lerp(progressSlider.minValue, progressSlider.maxValue, progress);

        if (sliderFill != null) sliderFill.color = GetFillColor(progress);

        // 알람 점은 국세청 앱 아이콘과 같은 상태를 따라간다(납부하면 함께 꺼진다).
        if (alarmDot != null)
        {
            bool hasAlarm = PhoneManager.Instance != null
                            && PhoneManager.Instance.GetAppAlarmState(AppKey.Tax) != AlarmState.None;
            alarmDot.SetActive(hasAlarm);
        }
    }

    /// <summary>세금 앱의 표정 아이콘과 같은 4구간으로 색을 고른다.</summary>
    private Color GetFillColor(float progress)
    {
        if (fillColors == null || fillColors.Length == 0) return Color.white;

        int idx = Mathf.Clamp(Mathf.FloorToInt(progress * fillColors.Length), 0, fillColors.Length - 1);
        return fillColors[idx];
    }
}
