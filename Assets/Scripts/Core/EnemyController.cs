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
        { WaveType.Heat, new HeatWave() },
        { WaveType.None, new NoneWave() }
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
    private Wave currentWave = new AgingWave();
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

    private void Awake()
    {
        if (waveKillCount == null || waveKillCount.Length != Wave.NumberOfWave)
            waveKillCount = new int[Wave.NumberOfWave];
    }

    public void InitEnemyController()
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

        weatherApp.InitApp(1, currentWave, nextWave, grid.CountNoTraitPlant(currentWave.WaveType));

        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
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

        // 저항력 흡수 비료 로직 실행 (상대 저항력을 흡수)
        grid.ProcessResistanceAbsorption();

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
                        // 특수(도박꾼): 저항력 40% 이하 웨이브를 스스로 버티면 가격의 20% 골드
                        if (SpecialItemSystem.Has("gambler") && !plant.IsFrozen()
                            && plant.GetResistanceValue((int)wave.WaveType) <= 0.4f)
                            GameManager.Instance.economyManager.AddGold(Mathf.RoundToInt(plant.GetSellingPrice() * 0.2f));

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

        // 저주(이중 웨이브): 서로 다른 두 번째 웨이브도 동시 판정. 저항 감소 중복 방지 위해 '죽이는 판정'만.
        if (CurseState.DoubleWave && currentWave != noneWave)
        {
            Wave second = PickSecondWave(currentWave.WaveType);
            if (second != null && second != noneWave)
            {
                for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
                {
                    if (grid.plantGrid.ContainsKey(idx))
                    {
                        Plant plant = grid.plantGrid[idx];
                        if (!plant.CanResist(second.WaveType))
                        {
                            GameEvents.RaisePeaDied();
                            plant.Die(); // TODO: 두 번째 웨이브 시각효과
                        }
                    }
                }
            }
        }

        // 저주(버섯): 버섯 타일 위 식물은 이번 웨이브에 피해(페트병은 방어)
        CurseManager.Instance?.ResolveMushroomWave();

        // 웨이브 처리 종료 후 얼어있는 식물 해동 (급속 냉각기 효과 종료)
        grid.UnfreezeAllPlants();


        SetNextSeason(); 
        SetNextWave(); // 다음 바뀐 후, 플레이어레코드 정보를 받아야 함

        grid.UpdateGoldScouterImageInGrid(); // 웨이브로 인해 단체로 죽을 때는 한 번만 갱신해서 연산량 줄이기
        grid.UpdateResistanceScouterImageInGrid(currentWave.WaveType); // 웨이브로 인해 저항력이 단체로 감소할 때는, 한 번만 갱신해서 연산량 줄이기

        int stage = GameManager.Instance.stage;
        if(weatherApp != null)
            weatherApp.LoadNextDay(stage, lastWave, currentWave,nextWave, stageNoTraitRecord[stage], grid.CountNoTraitPlant(currentWave.WaveType), stageKillRecord[stage]);
        //FlushNextWaveText();
        yield return null;
    }

    private void InitBaseWeightsByStage(int stage) // 첫 게임, 불러온 후 웨이브 해금
    {
        // 해금 기준은 WaveSchedule(단일 기준)에서 가져온다. (stage + 2 >= unlockStage 시 가중치 ON)
        foreach (WaveType type in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
        {
            if (type == WaveType.None) { baseWeights[WaveType.None] = 0f; continue; } // 리롤에서 제외
            baseWeights[type] = (stage + 2 >= WaveSchedule.GetUnlockStage(type)) ? 1f : 0f;
        }
    }

    public void UnlockWave(int stage) // 다음 스테이지에서 해금 될 웨이브 해금
    {
        // 이번 증가로 해금 스테이지에 도달한 웨이브의 가중치를 켠다. (기존 switch와 동일 동작)
        foreach (WaveType type in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
        {
            if (type == WaveType.None) continue;
            if (stage + 2 == WaveSchedule.GetUnlockStage(type))
                baseWeights[type] = 1f;
        }
    }

    public void SetNextWave()
    {
        lastWave = currentWave;
        currentWave = nextWave;
        grid.UpdateResistanceScouterImageInGrid(currentWave.WaveType);
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
        return WaveSchedule.GetSeasonByStage(stage);
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
        // 저주(기상이변): 웨이브 유형 확인 불가
        nextWaveText.text = CurseState.WaveBlind ? "???" : currentWave.WaveDescription;
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
        grid.UpdateResistanceScouterImageInGrid(currentWave.WaveType);
        setWave = currentWave.WaveType;
        lastWave = GetWaveFromWaveType(saveData.lastWaveType);
        nextWave = GetWaveFromWaveType(saveData.nextWaveType);

        waveSkipCount = saveData.remainWaveSkipCount;
        for(int i =0;i< Wave.NumberOfWave; i++)
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
                weatherApp.LoadNextDay(i, waves[stageWaveRecord[i]], currentWave, nextWave, stageNoTraitRecord[i], count, stageKillRecord[i]);
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
        // 계절 제약은 WaveSchedule(단일 기준)에서 가져온다. 해당 계절에 허용되지 않는 웨이브는 0으로 제외.
        foreach (WaveType type in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
        {
            if (type == WaveType.None) continue;
            if (map.ContainsKey(type) && !WaveSchedule.IsSeasonAllowed(type, nextSeason))
                map[type] = 0f;
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
        // 저주(집중포화): 5턴간 같은(현재 None이 아닌) 웨이브 고정
        if (CurseState.HeavyFire && currentWave != null && currentWave.WaveType != WaveType.None)
            return currentWave.WaveType;

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

    // 저주(이중 웨이브): 현재 웨이브와 다른, 해금된 웨이브 하나를 무작위 선택.
    private Wave PickSecondWave(WaveType exclude)
    {
        var candidates = new List<WaveType>();
        foreach (var kv in BuildEffectiveWeights())
            if (kv.Value > 0f && kv.Key != exclude && kv.Key != WaveType.None)
                candidates.Add(kv.Key);
        if (candidates.Count == 0) return null;
        return GetWaveFromWaveType(candidates[Random.Range(0, candidates.Count)]);
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
            weatherApp.UpdateCurrentWave(PhoneManager.Instance.GetIsPhoneTime() || !grid.GetIsBreeding() ? GameManager.Instance.stage + 1 : GameManager.Instance.stage, currentWave, grid.CountNoTraitPlant(currentWave.WaveType));
    }

    public int PickTraitFromUnlockWave()
    {
        int traitType = (int)PickNextByWeight();

        return traitType;
    }

    public void SetWeatherApp(WeatherApp app)
    {
        weatherApp = app;
    }
}
