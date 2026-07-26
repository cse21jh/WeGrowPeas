using UnityEngine;

public class BreedTimerManager : MonoBehaviour
{
    Grid grid;

    [SerializeField] private GameObject[] timers;
    [SerializeField] private TimerUI[] timerUIs;

    private void Awake()
    {
        grid = FindAnyObjectByType<Grid>();
        if(grid == null)
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

        foreach(GameObject timer in timers)
        {
            timer.SetActive(false);
        }

        switch (type)
        {
            case WaveType.Aging:
                timers[0].SetActive(true);
                timerUIs[0].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[0]);
                break;
            case WaveType.Wind:
                timers[1].SetActive(true);
                timerUIs[1].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[1]);
                break;
            case WaveType.Flood:
                timers[2].SetActive(true);
                timerUIs[2].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[2]);
                break;
            case WaveType.Pest:
                timers[3].SetActive(true);
                timerUIs[3].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[3]);
                break;
            case WaveType.Cold:
                timers[4].SetActive(true);
                timerUIs[4].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[4]);
                break;
            case WaveType.HeavyRain:
                timers[5].SetActive(true);
                timerUIs[5].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[5]);
                break;
            case WaveType.Drought:
                timers[6].SetActive(true);
                timerUIs[6].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[6]);
                break;
            case WaveType.Heat:
                timers[7].SetActive(true);
                timerUIs[7].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[7]);
                break;
            case WaveType.None:
                timers[8].SetActive(true);
                timerUIs[8].UpdateMaxTimerCount();
                grid.SetBreedTimerUI(timerUIs[8]);
                break;
            default:
                Debug.LogWarning("Unhandled wave type: " + type);
                break;
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
