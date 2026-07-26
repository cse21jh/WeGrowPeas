using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 국세청 앱 화면 컨트롤러. (WeatherApp 등과 동일하게 씬 패널에 붙는 MonoBehaviour)
/// 이번에 내야 할 세금액·마감일을 보여주고, [납부] 버튼으로 TaxManager에 납부를 위임한다.
/// </summary>
public class TaxApp : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;  // 이번 세금액
    [SerializeField] private TMP_Text dueText;     // "N일차 마감"
    [SerializeField] private TMP_Text statusText;  // 납부 결과 안내
    [SerializeField] private Button payButton;     // 납부 버튼 (onClick → OnClickPay)

    private void OnEnable()
    {
        GameEvents.OnGoldChanged += OnGoldChanged;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged -= OnGoldChanged;
    }

    private void OnGoldChanged(int _) => Refresh();

    /// <summary>화면 표시 갱신. 앱을 열 때 / 납부 후 호출.</summary>
    public void Refresh()
    {
        var tax = TaxManager.Instance;
        if (tax == null) return;

        if (amountText != null) amountText.text = tax.DueAmount.ToString();
        if (dueText != null) dueText.text = $"{tax.DueTaxStage}일차 마감";
        if (payButton != null) payButton.interactable = tax.CanPayNow();
        if (statusText != null) statusText.text = "";
    }

    /// <summary>납부 버튼 onClick에 연결.</summary>
    public void OnClickPay()
    {
        var tax = TaxManager.Instance;
        if (tax == null) return;

        if (tax.TryPay())
        {
            if (statusText != null) statusText.text = "납부 완료!";
            if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");

            // 국세청 앱 red dot 해제
            if (PhoneManager.Instance != null)
                PhoneManager.Instance.UpdateAppAlarmState(AppKey.Tax, AlarmState.None);
        }
        else
        {
            if (statusText != null) statusText.text = "골드가 부족합니다. 식물을 팔아 마련하세요.";
        }

        Refresh();
    }
}
