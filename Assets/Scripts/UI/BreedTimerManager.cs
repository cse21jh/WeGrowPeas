using UnityEngine;

public class BreedTimerManager : MonoBehaviour
{
    Grid grid;

    [SerializeField] private GameObject[] timers;
    [SerializeField] private TimerUI[] timerUIs;

    private void Awake()
    {
        grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogError("Grid not found in the scene.");
            return;
        }

        timerUIs = new TimerUI[timers.Length];

        for (int i = 0; i < timers.Length; i++)
        {
            timerUIs[i] = timers[i].GetComponentInChildren<TimerUI>();
            if (timerUIs[i] == null)
            {
                Debug.LogError($"TimerUI component not found in timer at index {i}.");
            }
        }
    }

    public void SetTimer(WaveType type)
    {
        if (CurseState.WaveBlind)
        {
            type = WaveType.Aging;
        }

        foreach (GameObject timer in timers)
        {
            timer.SetActive(false);
        }

        int index = -1;
        switch (type)
        {
            case WaveType.Aging: index = 0; break;
            case WaveType.Wind: index = 1; break;
            case WaveType.Flood: index = 2; break;
            case WaveType.Pest: index = 3; break;
            case WaveType.Cold: index = 4; break;
            case WaveType.HeavyRain: index = 5; break;
            case WaveType.Drought: index = 6; break;
            case WaveType.Heat: index = 7; break;
            case WaveType.None: index = 8; break;
            default:
                Debug.LogWarning("Unhandled wave type: " + type);
                break;
        }

        if (index != -1)
        {
            timers[index].SetActive(true);
            if (grid != null && grid.GetIsBreeding()) timerUIs[index].StartBreedingTimer();
            else timerUIs[index].UpdateMaxTimerCount();
            grid.SetBreedTimerUI(timerUIs[index]);
        }
    }

    public void SetPhoneTimer()
    {
        foreach (GameObject timer in timers)
        {
            timer.SetActive(false);
        }

        // 8번 칸에 추후 폰 타이머 UI 삽입하면 됨
        timers[8].SetActive(true);
        timerUIs[8].UpdatePhoneMaxTimerCount();
        grid.SetBreedTimerUI(timerUIs[8]);
    }

    // 세금 압류 유예 타이머(폰 타이머 슬롯 재사용)
    public void StartTaxTimer(int seconds)
    {
        foreach (GameObject timer in timers)
            timer.SetActive(false);

        timers[8].SetActive(true);
        grid.SetBreedTimerUI(timerUIs[8]);
        timerUIs[8].StartTaxTimer(seconds);
    }

    public void StopTaxTimer()
    {
        if (timerUIs != null && timerUIs.Length > 8 && timerUIs[8] != null)
            timerUIs[8].StopTimerByPhone();
    }
}
