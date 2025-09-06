using UnityEngine;

public class BreedTimerManager : MonoBehaviour
{
    Grid grid;

    [SerializeField] private GameObject[] timers;
    [SerializeField] private TimerUI[] timerUIs;

    private void Start()
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
        foreach(GameObject timer in timers)
        {
            timer.SetActive(false);
        }

        switch (type)
        {
            case WaveType.Aging:
                timers[0].SetActive(true);
                grid.SetBreedTimerUI(timerUIs[0]);
                break;
            case WaveType.Wind:
                timers[1].SetActive(true);
                grid.SetBreedTimerUI(timerUIs[1]);
                break;
            case WaveType.Flood:
                timers[2].SetActive(true);
                grid.SetBreedTimerUI(timerUIs[2]);
                break;
            case WaveType.Pest:
                timers[3].SetActive(true);
                grid.SetBreedTimerUI(timerUIs[3]);
                break;
            case WaveType.Cold:
                timers[4].SetActive(true);
                grid.SetBreedTimerUI(timerUIs[4]);
                break;
            case WaveType.HeavyRain:
                timers[5].SetActive(true);
                grid.SetBreedTimerUI(timerUIs[5]);
                break;
            default:
                Debug.LogWarning("Unhandled wave type: " + type);
                break;
        }
    }
}
