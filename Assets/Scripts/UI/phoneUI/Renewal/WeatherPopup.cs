using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 날씨 팝업(Popup_Wave). 날씨 위젯에서 열어 지난 며칠에 무엇이 지나갔고
/// 식물이 몇 개 죽었는지 최신순으로 훑어본다.
///
/// 기록은 <see cref="EnemyController.StageWaveRecord"/> / <see cref="EnemyController.StageKillRecord"/>가
/// 일차를 인덱스로 들고 있다. 여기서는 읽기만 한다.
///
/// 행은 WaveBanner 프리팹(WaveBannerRow)이 채운다.
/// (프리팹에 붙여 인스펙터에서 연결한다)
/// </summary>
public class WeatherPopup : MonoBehaviour
{
    [Header("Popup_Wave 구조")]
    [Tooltip("Scroll View > Viewport > Content — 행이 쌓이는 곳")]
    [SerializeField] private Transform content;

    [Tooltip("WaveBanner 프리팹 — WaveBannerRow가 붙어 있어야 한다")]
    [SerializeField] private GameObject bannerPrefab;

    [Tooltip("Top > Text (TMP) — 비워두면 제목을 건드리지 않는다")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("기록이 하나도 없을 때 켤 안내. 없으면 생략")]
    [SerializeField] private GameObject emptyText;

    [Header("웨이브 아이콘 (WaveType 순서 — 날씨 앱과 같게)")]
    [SerializeField] private Sprite[] waveIcons;

    private readonly List<GameObject> spawned = new List<GameObject>();

    /// <summary>팝업이 열렸을 때. 예보를 확인한 것으로 보고 알람을 끄는 데 쓴다.</summary>
    public static event System.Action OnOpened;

    /// <summary>팝업을 열고 목록을 새로 만든다. (위젯 버튼에 연결)</summary>
    public void Open()
    {
        // 꺼져 있으면 켜는 것만으로 OnEnable이 Refresh를 부른다.
        // 이미 켜져 있을 때만 직접 갱신한다 (한 프레임에 두 번 그리지 않도록).
        if (gameObject.activeSelf) Refresh();
        else gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);

    private void OnEnable()
    {
        // 열려 있는 동안 웨이브가 넘어가면 목록도 바로 따라간다.
        // (하루 경과 알림은 폰이 닫힌 뒤에 오므로 웨이브 확정 시점을 듣는다)
        GameEvents.OnWaveScheduleChanged += Refresh;

        // "N일 전"은 현재 일차 기준이라, 날이 바뀌면 같은 기록도 라벨이 하루씩 밀린다.
        GameEvents.OnDayStarted += Refresh;

        Refresh();
        MarkForecastSeen();
    }

    private void OnDisable()
    {
        GameEvents.OnWaveScheduleChanged -= Refresh;
        GameEvents.OnDayStarted -= Refresh;
    }

    /// <summary>
    /// 한 번 띄웠으면 예보를 확인한 것이므로 알람을 끈다.
    /// 날씨 앱 아이콘의 red dot과 위젯의 점이 함께 꺼진다.
    /// </summary>
    private void MarkForecastSeen()
    {
        if (PhoneManager.Instance != null)
            PhoneManager.Instance.UpdateAppAlarmState(AppKey.Weather, AlarmState.None);

        OnOpened?.Invoke();
    }

    /// <summary>지난 기록을 최신순으로 다시 깐다.</summary>
    public void Refresh()
    {
        Clear();

        var gm = GameManager.Instance;
        var enemy = gm != null ? gm.enemyController : null;
        if (enemy == null) return;

        List<WaveType> waves = enemy.StageWaveRecord;
        List<int> kills = enemy.StageKillRecord;
        if (waves == null) return;

        if (titleText != null) titleText.text = "지난 웨이브";

        // 인덱스 0은 초기화용 더미라 1일차부터 본다. 오늘 지나간 웨이브까지 포함.
        int today = gm.stage;
        int lastDay = Mathf.Min(today, waves.Count - 1);

        int added = 0;
        for (int day = lastDay; day >= 1; day--) // 최신이 위로
        {
            int died = (kills != null && day < kills.Count) ? kills[day] : 0;
            AddRow(day, today, waves[day], died);
            added++;
        }

        if (emptyText != null) emptyText.SetActive(added == 0);
    }

    private void AddRow(int day, int today, WaveType waveType, int diedCount)
    {
        if (bannerPrefab == null || content == null) return;

        GameObject row = Instantiate(bannerPrefab, content);
        row.SetActive(true);
        spawned.Add(row);

        var banner = row.GetComponent<WaveBannerRow>();
        if (banner == null)
        {
            Debug.LogWarning("[WeatherPopup] Banner Prefab에 WaveBannerRow가 없습니다. " +
                             "프리팹에 컴포넌트를 붙이고 인스펙터에서 연결하세요.");
            return;
        }

        int ago = today - day;
        string dayLabel = ago <= 0 ? "오늘" : $"{ago}일 전";

        Sprite icon = null;
        if (waveIcons != null && waveIcons.Length > 0)
        {
            int idx = waveType == WaveType.None ? 0 : Mathf.Clamp((int)waveType, 0, waveIcons.Length - 1);
            icon = waveIcons[idx];
        }

        string plant = GameManager.Instance != null ? GameManager.Instance.currentPlant : "식물";
        string dieLabel = diedCount > 0 ? $"{plant} {diedCount}개 죽음" : "피해 없음";

        banner.Setup(dayLabel, icon, dieLabel);
    }

    /// <summary>
    /// 목록을 비운다. 내가 만든 행뿐 아니라 <b>에디터에서 미리 넣어 둔 예시 행</b>도 지운다.
    /// Content는 목록 전용이라 자식이 전부 행이다.
    /// </summary>
    private void Clear()
    {
        spawned.Clear();
        if (content == null) return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);

            // Destroy는 프레임 끝에 처리되므로, 먼저 떼어내 childCount에서 바로 빠지게 한다.
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
