using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public enum Season
{    
    Summer,
    Fall,
    Winter,
    Spring,
}


public class EnemyController : MonoBehaviour
{
    private static readonly Dictionary<WaveType, Wave> waves = new Dictionary<WaveType, Wave>
    {
        { WaveType.Aging, new AgingWave() },
        { WaveType.Pest, new PestWave() },
        { WaveType.Wind, new WindWave() },
        { WaveType.Flood, new FloodWave() },
        { WaveType.HeavyRain, new HeavyRainWave() },
        { WaveType.Cold, new ColdWave() },
        { WaveType.Drought, new DroughtWave() },
        { WaveType.Heat, new HeatWave() }
    };

    public Grid grid;


    private Wave noneWave;

    [SerializeField] TextMeshProUGUI tempSeasonText;

    [SerializeField] TextMeshProUGUI nextWaveText;

    [SerializeField] private GameObject waveSkipButton;
    [SerializeField] TextMeshProUGUI waveSkipCountText;

    [SerializeField] private float waveDuration = 1f;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private BreedTimerManager breedTimerManager;

    [SerializeField] private WeatherApp weatherApp;

    [SerializeField] public WaveType setWave;

    [SerializeField] public SignPostController signPost;

    [Header("Wave Weights")]
    [SerializeField] private float defaultBaseWeight = 1f; // 기본 가중치

    [SerializeField] private Dictionary<WaveType, float> baseWeights = new Dictionary<WaveType, float>();

    private bool isWaveSkipped = false;

    //int CurrentDay => GameManager.Instance.stage;


    private Season currentSeason = Season.Spring; // 세이브데이터에서 불러와야함
    [SerializeField] private SeasonManager seasonManager;





    private Wave lastWave;
    private Wave currentWave;
    private Wave nextWave;
    private int waveSkipCount = 0;
    private int[] waveKillCount;

    private List<WaveType> stageWaveRecord = new List<WaveType>(); // 세이브데이터의 웨이브, 좀 더 확인 >> 엔딩조건 체크하기 위해 필요
    private List<int> stageKillRecord = new List<int>();
    private List<int> stageNoTraitRecord = new List<int>();

    public Season CurrentSeason => currentSeason;
    public Wave CurrentWave => currentWave;
    public Wave NextWave => nextWave;
    public Wave LastWave => lastWave;
    public int WaveSkipCount => waveSkipCount;
    public int[] WaveKillCount => waveKillCount;
    public List<WaveType> StageWaveRecord => stageWaveRecord;
    public List<int> StageKillRecord => stageKillRecord;
    public List<int> StageNoTraitRecord => stageNoTraitRecord;

    // Start is called before the first frame update
    void Start()
    {
        waveKillCount = new int[Wave.NumberOfWave];
        stageWaveRecord.Add(WaveType.Aging);
        stageKillRecord.Add(0);
        stageNoTraitRecord.Add(0);
        SetWaveSkipCountText();
        HideWaveSkipButton();

        InitBaseWeightsByStage(1);

        noneWave = new NoneWave();

        lastWave = GetWaveFromWaveType(WaveType.Aging);
        currentWave = lastWave;
        nextWave = GetWaveFromWaveType(PickNextByWeight());

        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator EnemyWaveCoroutine()
    {
        //Debug.Log($"웨이브 생성 할 때 사용합니다 {unlockedWave.Count}");
        Wave wave = currentWave;
        // Debug.Log("현재 웨이브 타입 : " + currentWave);
        PlayerRecordForGraph.SetWED((int)wave.WaveType);
        SoundManager.Instance.PlayEffect(wave.WaveSoundString);        

        if (waveManager != null)
        {
            waveManager.StartWave(waveDuration, wave.WaveType);
        }

        float t = 0f;
        isWaveSkipped = false;
        while (t < waveDuration && !isWaveSkipped)
        {
            t += Time.deltaTime;
            yield return null;
            if (Input.GetKeyDown(KeyCode.S))
            {
                SkipWaveEffect();                
            }
        }

        stageWaveRecord.Add(currentWave.WaveType);
        stageKillRecord.Add(0);
        stageNoTraitRecord.Add(grid.CountNoTraitPlant(currentWave.WaveType));

        if (currentWave != noneWave)
        {
            for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
            {
                if (grid.plantGrid.ContainsKey(idx))
                {
                    Plant plant = grid.plantGrid[idx];

                    if (plant.CanResist(wave.WaveType))
                    {
                        // Debug.Log(idx + "번째 식물이 웨이브를 버틸 수 있습니다");
                        plant.ResistWave(wave.WaveType);
                    }
                    else
                    {
                        waveKillCount[(int)currentWave.WaveType] += 1;
                        stageKillRecord[GameManager.Instance.stage]++;
                        GameEvents.RaisePeaDied();
                        // Debug.Log(idx + "번째 식물이 죽었습니다");
                        if (!plant.Die())
                            plant.ResistWave(wave.WaveType);
                    }

                }
            }
        }


        SetNextSeason(); 
        SetNextWave(); // 다음 바뀐 후, 플레이어레코드 정보를 받아야 함

        int stage = GameManager.Instance.stage;
        if(weatherApp != null)
            weatherApp.LoadNextDay(stage, lastWave, currentWave, stageNoTraitRecord[stage], grid.CountNoTraitPlant(currentWave.WaveType), stageKillRecord[stage]);
        //FlushNextWaveText();
        yield return null;
    }

    private void InitBaseWeightsByStage(int stage) // 첫 게임, 불러온 후 웨이브 해금
    {
        // 초기화
        baseWeights[WaveType.Aging] = 1f; // 항상 해금
        baseWeights[WaveType.Pest] = (stage + 2 >= PestWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Wind] = (stage + 2 >= WindWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Flood] = (stage + 2 >= FloodWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.HeavyRain] = (stage + 2 >= HeavyRainWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Cold] = (stage + 2 >= ColdWave.UnlockStage ) ? 1f : 0f;
        baseWeights[WaveType.Drought] = (stage + 2 >= DroughtWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Heat] = (stage + 2 >= HeatWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.None] = 0f; // 리롤 버튼에서 제외
    }

    public void UnlockWave(int stage) // 다음 스테이지에서 해금 될 웨이브 해금
    {
        switch (stage + 2)
        {
            case PestWave.UnlockStage: baseWeights[WaveType.Pest] = 1f; break;
            case WindWave.UnlockStage: baseWeights[WaveType.Wind] = 1f; break;
            case FloodWave.UnlockStage: baseWeights[WaveType.Flood] = 1f; break;
            case HeavyRainWave.UnlockStage: baseWeights[WaveType.HeavyRain] = 1f; baseWeights[WaveType.Drought] = 1f; break;
            case ColdWave.UnlockStage: baseWeights[WaveType.Cold] = 1f; baseWeights[WaveType.Heat] = 1f; break;            
        }
    }

    public void SetNextWave()
    {
        lastWave = currentWave;
        currentWave = nextWave;
        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        WaveType picked = PickNextByWeight();
        nextWave = GetWaveFromWaveType(picked);
    }

    public void SetCurrentWaveTimer()
    {
        breedTimerManager.SetTimer(currentWave.WaveType);
        ShowNextWaveText();
    }

    public void SetNextSeason()
    {
        Season nextSeason = GetSeasonByStage(GameManager.Instance.stage + 1);
        if (currentSeason != nextSeason)
            SetSeason(nextSeason);
        return;
    }

    public Season GetSeasonByStage(int stage)
    {
        return (Season)(((stage - 1) / 5) % 4);
    }

    public Season GetSeason()
    {
        return currentSeason;
    }

    public void SetSeason(Season season)
    {
        // 계절이 변경되었을 때, 계절에 맞는 텍스트를 해당 위치에 변경
        switch (season)
        {
            case Season.Spring:
                tempSeasonText.text = "봄";
                break;
            case Season.Summer:
                tempSeasonText.text = "여름";
                break;
            case Season.Fall:
                tempSeasonText.text = "가을";
                break;
            case Season.Winter:
                tempSeasonText.text = "겨울";
                break;
        }
        currentSeason = season;

        seasonManager.ChangeToSeason(season);
    }

    public void WaveSkip()
    {
        if (grid.GetIsBreeding() && waveSkipCount > 0 && currentWave!=noneWave)
        {
            currentWave = noneWave;
            waveSkipCount--;
            SetWaveSkipCountText();            
            ShowNextWaveText();
            if (weatherApp != null)
                weatherApp.UpdateCurrentWave(GameManager.Instance.stage, currentWave, 0);
        }
        if(waveSkipCount <= 0)
            HideWaveSkipButton();
        return;
    }

    public bool IsLastWaveNone()
    {
        if (lastWave == noneWave)
            return true;
        else
            return false;
    }

    public void ShowNextWaveText()
    {
        nextWaveText.text = currentWave.WaveDescription;
    }

    public void SetNextWaveText(string text)
    {
        nextWaveText.text = text;
    }

    public string GetNextWaveText()
    {
        return nextWaveText.text;   
    }

    private void FlushNextWaveText()
    {
        nextWaveText.text = "";
    }

    public void AddWaveSkipCount(int count)
    {
        ShowWaveSkipButton();
        waveSkipCount += count;
        SetWaveSkipCountText();
        return;
    }

    private void SetWaveSkipCountText()
    {
        if (waveSkipCountText == null)
            return;

        waveSkipCountText.text = "추가 스킵 "+ waveSkipCount.ToString() + "회";
        return;
    }

    public void ShowWaveSkipButton()
    {
        if (waveSkipButton == null)
            return;

        if(waveSkipCount > 0) 
            waveSkipButton.SetActive(true);
        return;
    }

    public void HideWaveSkipButton()
    {
        if (waveSkipButton == null)
            return;

        waveSkipButton.SetActive(false);
        return;
    }

    public static Wave GetWaveFromWaveType(WaveType waveType)
    {
        return waveType switch
        {
            WaveType.Aging => new AgingWave(),
            WaveType.Wind => new WindWave(),
            WaveType.Flood => new FloodWave(),
            WaveType.Pest => new PestWave(),
            WaveType.Cold => new ColdWave(),
            WaveType.HeavyRain => new HeavyRainWave(),
            WaveType.Drought => new DroughtWave(),
            WaveType.Heat => new HeatWave(),
            WaveType.None => new NoneWave(),
        };
    }

    public void LoadEnemyController(SaveData saveData)
    {
        InitBaseWeightsByStage(saveData.stage);

        currentWave = GetWaveFromWaveType(saveData.curWaveType);
        setWave = currentWave.WaveType;
        lastWave = GetWaveFromWaveType(saveData.lastWaveType);
        nextWave = GetWaveFromWaveType(saveData.nextWaveType);

        waveSkipCount = saveData.remainWaveSkipCount;
        for(int i =0;i<7;i++)
            waveKillCount[i] = saveData.waveKillCount[i];
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        ShowNextWaveText();
        breedTimerManager.SetTimer(currentWave.WaveType);

        SetSeason(saveData.currentSeason);
        
        stageWaveRecord = saveData.stageWaveRecord;
        stageKillRecord = saveData.stageKillRecord;
        stageNoTraitRecord = saveData.stageNoTraitRecord;
        if (weatherApp != null)
        {
            int count = grid.CountNoTraitPlant(currentWave.WaveType);
            for (int i = 1; i < stageWaveRecord.Count; i++)
            {
                weatherApp.LoadNextDay(i, waves[stageWaveRecord[i]], currentWave, stageNoTraitRecord[i], count, stageKillRecord[i]);
            }
        }
    }

    private void OnValidate()
    {
        currentWave = GetWaveFromWaveType(setWave);
        ShowNextWaveText();
    }

    // ---------- 가중치 기반 & 리롤 ----------
    private Dictionary<WaveType, float> BuildEffectiveWeights()
    {
        // 기본 가중치 복사
        var map = new Dictionary<WaveType, float>(baseWeights);

        Season nextSeason;
        if (GameManager.Instance == null)
            nextSeason = Season.Summer;
        else
        { 
            nextSeason = GetSeasonByStage(GameManager.Instance.stage + 2); // 다음 스테이지가 시작될 때 계절에 맞는 가중치 0으로 제외
        }
        switch (nextSeason)
        {
            case Season.Spring:
                map[WaveType.Cold] = 0;
                map[WaveType.HeavyRain] = 0;
                break;
            case Season.Summer:
                map[WaveType.Cold] = 0;
                map[WaveType.Drought] = 0;
                break;
            case Season.Fall:
                map[WaveType.Heat] = 0;
                map[WaveType.Drought] = 0;
                break;
            case Season.Winter:
                map[WaveType.Heat] = 0;
                map[WaveType.HeavyRain] = 0;
                break;
        }

        foreach (var t in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
        {
            if (t == WaveType.None) { map[t] = 0f; continue; }

            float modMul = ModManager.Instance
                ? ModManager.Instance.GetMul(StatId.WaveWeightMul, (int)t)
                : 1f;

            if (map.ContainsKey(t))
                map[t] *= modMul;
        }

        // None은 항상 제외
        map[WaveType.None] = 0f;
        return map;
    }

    private WaveType PickNextByWeight()
    {
        var map = BuildEffectiveWeights();

        // 총 합계
        float sum = 0f;
        foreach (var kv in map) sum += kv.Value;

        // 합계 0이면 자연사
        if (sum <= 0f)
            return WaveType.Aging;

        float r = Random.Range(0f, sum);
        float acc = 0f;
        foreach (var kv in map)
        {
            acc += kv.Value;
            if (r <= acc) return kv.Key;
        }
        return WaveType.Aging; // 예외처리용 기본값
    }

    public void TutorialWave()
    {
        StartCoroutine(TEnemyWaveCoroutine());
    }

    private IEnumerator TEnemyWaveCoroutine()
    {
        //Debug.Log($"웨이브 생성 할 때 사용합니다 {unlockedWave.Count}");
        Wave wave = currentWave;
        Debug.Log("currentWave : " + currentWave);
        SoundManager.Instance.PlayEffect(wave.WaveSoundString);

        if (waveManager != null)
        {
            waveManager.StartWave(waveDuration, wave.WaveType);
        }

        yield return new WaitForSeconds(waveDuration); // 웨이브 이펙트 끝 날 때까지

        if (currentWave != noneWave)
        {
            for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
            {
                if (grid.plantGrid.ContainsKey(idx))
                {
                    Plant plant = grid.plantGrid[idx];

                    if (idx == 0 || idx == 2) plant.Die();
                    else plant.ResistWave(wave.WaveType);
                }
            }
        }


        //SetNextWave();
        //breedTimerManager 의 null reference issue를 에디터에서 함수 분리해서 해결
        lastWave = currentWave;
        currentWave = nextWave;
        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        //breedTimerManager.SetTimer(currentWave.WaveType);
        ShowNextWaveText();
        WaveType picked = PickNextByWeight();
        nextWave = GetWaveFromWaveType(picked);

        currentWave = GetWaveFromWaveType(WaveType.Aging);
        //FlushNextWaveText();
        yield return null;
    }

    public string GetMostKillWaveName()
    {
        int maxKills = waveKillCount.Max();
        int waveIndex = waveKillCount.ToList().IndexOf(maxKills);

        WaveType mostKillWaveType = (WaveType)waveIndex;
        return waves[mostKillWaveType].WaveName;
    }

    public void SkipWaveEffect()
    {
        isWaveSkipped = true;
        waveManager.SkipWaveEffect();
    }

    public void UpdateCurrentWaveAlarm()
    {
        if (weatherApp != null)
            weatherApp.UpdateCurrentWave(GameManager.Instance.stage, currentWave, grid.CountNoTraitPlant(currentWave.WaveType));
    }

    public int PickTraitFromUnlockWave()
    {
        int traitType = (int)PickNextByWeight();

        return traitType;
    }
}
