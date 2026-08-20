using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈 화면 날씨 위젯. 내일 올 웨이브를 <see cref="EnemyController"/>에서 읽어 아이콘·이름으로 보여준다.
///
/// 두 칸(Dual 프레임)을 쓰는 경우는 두 가지다.
///   - 이중 웨이브 저주: 같은 날 두 번째 웨이브까지
///   - 일기예보 특성: 모레 올 웨이브까지
///
/// 내일 웨이브에 저항이 없는 식물이 있으면 알람을 띄운다.
/// 아이콘·색 배열은 날씨 앱(<see cref="WeatherApp"/>)과 같은 순서(WaveType)로 채운다.
/// </summary>
public class WeatherWidget : MonoBehaviour
{
    [Header("한 개일 때 (WaveImageFrame_Uni)")]
    [SerializeField] private GameObject uniFrame;
    [SerializeField] private Image uniWaveImage;
    [SerializeField] private TMP_Text uniWaveNameText;

    [Header("두 개일 때 (WaveImageFrame_Dual)")]
    [SerializeField] private GameObject dualFrame;
    [SerializeField] private Image dualWaveImage1;
    [SerializeField] private TMP_Text dualWaveNameText1;
    [SerializeField] private Image dualWaveImage2;
    [SerializeField] private TMP_Text dualWaveNameText2;

    [Header("웨이브 아이콘 (WaveType 순서)")]
    [Tooltip("규격이 화면마다 달라 아이콘만 여기서 지정한다. 색은 WavePalette를 따른다.")]
    [SerializeField] private Sprite[] waveIcons;

    [Tooltip("웨이브 색을 칠할 프레임 배경. 없으면 색은 건드리지 않는다.")]
    [SerializeField] private Image uniFrameBackground;

    [Header("알람")]
    [Tooltip("내일 웨이브에 저항이 없는 식물이 있으면 켜지는 점.")]
    [SerializeField] private GameObject alarmDot;

    [Tooltip("날씨 앱 아이콘에도 같은 알람을 띄운다.\n" +
             "날씨 앱은 여는 코드가 따로 없어 알람을 꺼 줄 지점이 없다. " +
             "앱 쪽에서 해제 처리를 붙인 뒤에 켤 것.")]
    [SerializeField] private bool raiseAppAlarm = false;

    [Header("팝업")]
    [Tooltip("위젯을 눌렀을 때 열 날씨 팝업(Popup_Wave).")]
    [SerializeField] private WeatherPopup weatherPopup;

    private void OnEnable()
    {
        // 웨이브가 새로 정해지는 순간(SetNextWave)에 맞춰 갱신한다.
        // OnQuestDayPassed는 폰이 닫힌 뒤에 와서 위젯이 열려 있는 동안 반영되지 않는다.
        GameEvents.OnWaveScheduleChanged += OnWaveScheduleChanged;
        GameEvents.OnDayStarted += Refresh; // 날이 바뀌면 위험 식물 수를 다시 센다
        PhoneManager.OnAppAlarmChanged += OnAppAlarmChanged;
        WeatherPopup.OnOpened += OnPopupOpened;

        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnWaveScheduleChanged -= OnWaveScheduleChanged;
        GameEvents.OnDayStarted -= Refresh;
        PhoneManager.OnAppAlarmChanged -= OnAppAlarmChanged;
        WeatherPopup.OnOpened -= OnPopupOpened;
    }

    private void OnWaveScheduleChanged()
    {
        // 새 예보가 나왔으니 다시 알린다.
        forecastSeen = false;
        Refresh();
    }

    private void OnPopupOpened()
    {
        // 팝업으로 예보를 확인했으면 이번 예보의 알람은 끝. 다음 날 다시 켜진다.
        forecastSeen = true;
        UpdateAlarmDot();
    }

    private void OnAppAlarmChanged(AppKey key, AlarmState _)
    {
        if (key == AppKey.Weather) UpdateAlarmDot();
    }

    /// <summary>위젯 갱신. 웨이브가 바뀐 뒤 바깥에서 즉시 갱신하고 싶을 때도 부를 수 있다.</summary>
    public void Refresh()
    {
        var enemy = GameManager.Instance != null ? GameManager.Instance.enemyController : null;
        if (enemy == null) return;

        var grid = GameManager.Instance.grid;

        // 폰을 보는 시점(밤) 기준으로 CurrentWave가 "내일 올 웨이브"다.
        // EnemyController가 날씨 앱에 UpdateCurrentWave(stage + 1, currentWave)로 넘기는 것과 같은 규칙.
        Wave tomorrow = enemy.CurrentWave;

        // 같은 날 두 번째로 오는 웨이브 (이중 웨이브 저주). 없으면 null.
        Wave sameDaySecond = Valid(enemy.GetNextSecondWave()) ? enemy.GetNextSecondWave() : null;

        // 모레 웨이브는 일기예보 특성이 있어야 보인다.
        // (날씨 앱도 GetHasWeatherForecast()일 때만 UpdateNextWave(stage + 2, nextWave)를 부른다)
        bool hasForecast = grid != null && grid.GetHasWeatherForecast();
        Wave dayAfter = hasForecast && Valid(enemy.NextWave) ? enemy.NextWave : null;

        // 두 칸을 쓰는 경우 — 이중 웨이브 저주(같은 날 2번)가 우선, 그다음 일기예보(모레).
        Wave second = sameDaySecond ?? dayAfter;
        bool isDual = second != null;

        if (uniFrame != null) uniFrame.SetActive(!isDual);
        if (dualFrame != null) dualFrame.SetActive(isDual);

        if (isDual)
        {
            SetWave(dualWaveImage1, dualWaveNameText1, tomorrow);
            SetWave(dualWaveImage2, dualWaveNameText2, second);
        }
        else
        {
            SetWave(uniWaveImage, uniWaveNameText, tomorrow);

            if (uniFrameBackground != null && tomorrow != null)
                uniFrameBackground.color = WavePalette.GetColor(tomorrow.WaveType);
        }

        // 알람은 내일 실제로 맞을 웨이브만 센다. 모레 예보는 참고용이라 제외.
        UpdateAlarm(tomorrow, sameDaySecond);
    }

    private static bool Valid(Wave wave) => wave != null && wave.WaveType != WaveType.None;

    /// <summary>날씨 팝업 열기. 위젯 버튼에 연결한다.</summary>
    public void OpenWeatherPopup()
    {
        if (weatherPopup == null) return;

        PhoneManager.Instance?.PhoneTouchEffect();
        weatherPopup.Open();
    }

    private void SetWave(Image image, TMP_Text nameText, Wave wave)
    {
        if (wave == null) return;

        if (image != null && waveIcons != null && waveIcons.Length > 0)
        {
            image.sprite = waveIcons[IconIndex(wave)];
            image.enabled = image.sprite != null;
        }

        if (nameText != null)
            nameText.text = wave.WaveType == WaveType.None ? "맑음" : wave.WaveName;
    }

    /// <summary>None은 날씨 앱과 마찬가지로 0번 아이콘을 쓴다.</summary>
    private int IconIndex(Wave wave)
    {
        if (wave == null || wave.WaveType == WaveType.None) return 0;
        return Mathf.Clamp((int)wave.WaveType, 0, waveIcons.Length - 1);
    }

    // ── 알람 ──────────────────────────────────────────────────────────────────

    /// <summary>내일 웨이브를 못 버티는 식물이 있는가. 갱신할 때마다 다시 센다.</summary>
    private bool hasRisk;

    /// <summary>이번 예보를 팝업으로 확인했는가. 하루가 지나면 다시 false가 된다.</summary>
    private bool forecastSeen;

    /// <summary>알람을 켤 상황인가 — 위험한데 아직 확인하지 않았을 때.</summary>
    private bool ShouldAlarm => hasRisk && !forecastSeen;

    /// <summary>저항이 없는 식물 수를 세어 알람 여부를 정한다.</summary>
    private void UpdateAlarm(Wave next, Wave second)
    {
        var grid = GameManager.Instance != null ? GameManager.Instance.grid : null;
        if (grid == null) return;

        int atRisk = 0;
        if (next != null && next.WaveType != WaveType.None) atRisk += grid.CountNoTraitPlant(next.WaveType);
        if (second != null && second.WaveType != WaveType.None) atRisk += grid.CountNoTraitPlant(second.WaveType);

        hasRisk = atRisk > 0;

        if (raiseAppAlarm && PhoneManager.Instance != null)
        {
            // 알아두면 좋은 정보지 반드시 읽어야 하는 건 아니므로 NonMandatory.
            PhoneManager.Instance.UpdateAppAlarmState(
                AppKey.Weather,
                ShouldAlarm ? AlarmState.NonMandatory : AlarmState.None);
        }

        UpdateAlarmDot();
    }

    private void UpdateAlarmDot()
    {
        if (alarmDot == null) return;

        // 앱 알람을 함께 쓰는 경우엔 그쪽 상태를 따라간다(팝업이 끈 것도 반영되도록).
        bool on = (raiseAppAlarm && PhoneManager.Instance != null)
            ? PhoneManager.Instance.GetAppAlarmState(AppKey.Weather) != AlarmState.None
            : ShouldAlarm;

        alarmDot.SetActive(on);
    }
}
