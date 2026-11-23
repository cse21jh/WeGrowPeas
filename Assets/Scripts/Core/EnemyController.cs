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

    [SerializeField] public WaveType setWave;

    [SerializeField] public SignPostController signPost;

    [Header("Wave Weights")]
    [SerializeField] private float defaultBaseWeight = 1f; // 기본 가중치

    // 웨이브 추첨. 표지판 같은 가중치 배수는 ModManager에서 온다.
    private readonly WaveDeck deck = new WaveDeck
    {
        WeightMultiplier = type => ModManager.Instance ? ModManager.Instance.GetMul(StatId.WaveWeightMul, (int)type) : 1f,
    };

    private bool isWaveSkipped = false;

    //int CurrentDay => GameManager.Instance.stage;


    private Season currentSeason = Season.Spring; // 세이브데이터에서 불러와야함
    [SerializeField] private SeasonManager seasonManager;





    private Wave lastWave;
    private Wave currentWave = new AgingWave();
    private Wave nextWave;
    private Wave nextSecondWave;
    private int waveSkipCount = 0;
    private int[] waveKillCount;
    private string shownWaveText;

    private List<WaveType> stageWaveRecord = new List<WaveType>(); // 세이브데이터의 웨이브, 좀 더 확인 >> 엔딩조건 체크하기 위해 필요
    private List<int> stageKillRecord = new List<int>();
    private List<int> stageNoTraitRecord = new List<int>();

    public Season CurrentSeason => currentSeason;
    public Wave CurrentWave => currentWave;
    public Wave NextWave => nextWave;
    
    public Wave GetNextWave() => nextWave;
    public Wave GetNextSecondWave() => nextSecondWave;
    public Wave LastWave => lastWave;
    public int WaveSkipCount => waveSkipCount;
    public int[] WaveKillCount => waveKillCount;
    public List<WaveType> StageWaveRecord => stageWaveRecord;
    public List<int> StageKillRecord => stageKillRecord;
    public List<int> StageNoTraitRecord => stageNoTraitRecord;

    private void OnEnable() => Loc.LanguageChanged += RefreshLocalizedTexts;

    private void OnDisable() => Loc.LanguageChanged -= RefreshLocalizedTexts;

    // 폰 알람처럼 다른 문구가 떠 있으면 웨이브 문구로 덮지 않는다. 알람은 TimerUI가 다시 그린다.
    private void RefreshLocalizedTexts()
    {
        ApplySeasonText(currentSeason);
        SetWaveSkipCountText();
        if (nextWaveText != null && nextWaveText.text == shownWaveText) ShowNextWaveText();
    }

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
        
        if (CurseState.DoubleWave)
            nextSecondWave = PickSecondWave(nextWave.WaveType);
        else
            nextSecondWave = null;


        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
    }

    public IEnumerator EnemyWaveCoroutine()
    {
        bool tutorial = GameManager.Instance.Tutorial != null;
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
            if (GameInput.GetDown(GameAction.Skip))
            {
                SkipWaveEffect();
            }
        }

        stageWaveRecord.Add(currentWave.WaveType);
        stageKillRecord.Add(0);
        stageNoTraitRecord.Add(grid.Board.CountNoTraitPlant(currentWave.WaveType));

        // 업적: 모든 칸이 찬 상태에서 웨이브를 맞는지 기록
        bool fullFieldWave = currentWave != noneWave && !tutorial && !grid.Board.HasEmptyCell();
        int plantsBeforeWave = grid.Board.Plants.Count;

        // 판정 순서는 DayRules에 있다 (흡수 비료 → 웨이브 → 두 번째 웨이브 → 버섯 → 해동)
        DayRules.ResolveWave(grid.Field, currentWave.WaveType,
            () => (nextSecondWave ?? PickSecondWave(currentWave.WaveType))?.WaveType,
            new GridFieldActions(grid));

        if (fullFieldWave) AchievementHooks.OnFullFieldWaveEnded(plantsBeforeWave, grid.Board.Plants.Count);


        if (tutorial)
            lastWave = currentWave;
        else
        {
            SetNextSeason();
            SetNextWave(); // 다음 바뀐 후, 플레이어레코드 정보를 받아야 함
        }

        grid.Scouter.UpdateGoldScouterImageInGrid(); // 웨이브로 인해 단체로 죽을 때는 한 번만 갱신해서 연산량 줄이기
        grid.Scouter.UpdateResistanceScouterImageInGrid(currentWave.WaveType); // 웨이브로 인해 저항력이 단체로 감소할 때는, 한 번만 갱신해서 연산량 줄이기

        int stage = GameManager.Instance.stage;
        //FlushNextWaveText();
        yield return null;
    }

    private void InitBaseWeightsByStage(int stage) => deck.Init(stage);

    public void UnlockWave(int stage) => deck.Unlock(stage);

    public void SetNextWave()
    {
        lastWave = currentWave;
        currentWave = nextWave;
        grid.Scouter.UpdateResistanceScouterImageInGrid(currentWave.WaveType);
        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        WaveType picked = PickNextByWeight();
        nextWave = GetWaveFromWaveType(picked);
        
        if (CurseState.DoubleWave)
            nextSecondWave = PickSecondWave(nextWave.WaveType);
        else
            nextSecondWave = null;

        GameEvents.RaiseWaveScheduleChanged(); // 예보 UI(홈 위젯 등) 즉시 갱신
    }

    public void SetTutorialWave(WaveType current, WaveType next)
    {
        currentWave = GetWaveFromWaveType(current);
        nextWave = GetWaveFromWaveType(next);
        setWave = current;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        breedTimerManager.SetTimer(current);
        ShowNextWaveText();
        GameEvents.RaiseWaveScheduleChanged();
    }

    public void RecordTutorialDayWithoutWave()
    {
        lastWave = currentWave;
        stageWaveRecord.Add(WaveType.None);
        stageKillRecord.Add(0);
        stageNoTraitRecord.Add(0);
    }

    public void AdvanceTutorialWave(WaveType following)
    {
        lastWave = currentWave;
        SetTutorialWave(nextWave.WaveType, following);
    }

    public void UpdateSecondWaveIfNull()
    {
        if (CurseState.DoubleWave && nextSecondWave == null && nextWave != null)
        {
            nextSecondWave = PickSecondWave(nextWave.WaveType);
        }
    }

    /// <summary>
    /// 디버그 전용: 일차를 강제로 점프시킬 때, 일차별 기록 리스트를 목표 일차까지 0으로 채운다.
    /// (stageKillRecord[stage] 등은 일차를 인덱스로 직접 접근하므로 크기가 맞지 않으면 IndexOutOfRange가 난다)
    /// 이미 채워진 앞쪽 기록은 건드리지 않고 부족한 뒤쪽만 더미로 메운다.
    /// </summary>
    public void DebugPadStageRecords(int targetStage)
    {
        while (stageWaveRecord.Count <= targetStage) stageWaveRecord.Add(WaveType.Aging);
        while (stageKillRecord.Count <= targetStage) stageKillRecord.Add(0);
        while (stageNoTraitRecord.Count <= targetStage) stageNoTraitRecord.Add(0);
    }

    /// <summary>디버그 전용: 다음 웨이브를 지정한 종류로 바꾼다.</summary>
    public void DebugSetNextWave(WaveType waveType)
    {
        nextWave = GetWaveFromWaveType(waveType);
        ShowNextWaveText();


        Debug.Log($"[Debug] 다음 웨이브 → {waveType}");
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

    private void ApplySeasonText(Season season)
    {
        if (tempSeasonText == null) return;

        tempSeasonText.text = season switch
        {
            Season.Summer => Loc.T("game.season.summer"),
            Season.Fall => Loc.T("game.season.fall"),
            Season.Winter => Loc.T("game.season.winter"),
            _ => Loc.T("game.season.spring"),
        };
    }

    public void SetSeason(Season season)
    {
        // 계절이 변경되었을 때, 계절에 맞는 텍스트를 해당 위치에 변경
        ApplySeasonText(season);
        if (season == Season.Winter)
            UnlockGrants.GrantEventUnlocks(UnlockManager.Ids.WinterReached); // 급속 냉각기·냉각 방패 해금
        currentSeason = season;

        seasonManager.ChangeToSeason(season);
    }

    public void WaveSkip()
    {
        if (GameManager.Instance?.Tutorial != null) return;
        if (GameManager.Instance != null && GameManager.Instance.GetGameIsStopped())
            return;

        if (grid.Breeder.GetIsBreeding() && waveSkipCount > 0 && currentWave != noneWave)
        {
            currentWave = noneWave;
            waveSkipCount--;
            SetWaveSkipCountText();
            ShowNextWaveText();
        }

        if (waveSkipCount <= 0)
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

    public string NextWaveDescription => CurseState.WaveBlind ? "???" : currentWave.WaveDescription;

    public void ShowNextWaveText()
    {
        // 저주(기상이변): 웨이브 유형 확인 불가
        shownWaveText = NextWaveDescription;
        nextWaveText.text = shownWaveText;
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

        waveSkipCountText.text = Loc.T("game.hud.extra_skips", ("count", waveSkipCount));
        return;
    }

    public void ShowWaveSkipButton()
    {
        if (waveSkipButton == null)
            return;

        if (waveSkipCount > 0)
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

    /// <summary>현재 웨이브/계절 상태를 저장 데이터에 담는다. <see cref="LoadEnemyController"/>와 짝.</summary>
    public void SaveEnemyController(WaveSave save)
    {
        save.currentSeason = CurrentSeason;
        save.curWaveType = CurrentWave.WaveType;
        save.nextWaveType = NextWave.WaveType;
        save.lastWaveType = LastWave.WaveType;
        save.remainWaveSkipCount = WaveSkipCount;
        save.waveKillCount = WaveKillCount;

        save.stageWaveRecord = StageWaveRecord;
        save.stageKillRecord = StageKillRecord;
        save.stageNoTraitRecord = StageNoTraitRecord;
    }

    /// <param name="stage">웨이브 가중치 초기화에 필요한 현재 스테이지(ProgressSave.stage).</param>
    public void LoadEnemyController(WaveSave saveData, int stage)
    {
        InitBaseWeightsByStage(stage);

        currentWave = GetWaveFromWaveType(saveData.curWaveType);
        grid.Scouter.UpdateResistanceScouterImageInGrid(currentWave.WaveType);
        setWave = currentWave.WaveType;
        lastWave = GetWaveFromWaveType(saveData.lastWaveType);
        nextWave = GetWaveFromWaveType(saveData.nextWaveType);

        waveSkipCount = saveData.remainWaveSkipCount;
        for (int i = 0; i < Wave.NumberOfWave; i++)
            waveKillCount[i] = saveData.waveKillCount[i];
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        ShowNextWaveText();
        breedTimerManager.SetTimer(currentWave.WaveType);

        SetSeason(saveData.currentSeason);

        stageWaveRecord = saveData.stageWaveRecord;
        stageKillRecord = saveData.stageKillRecord;
        stageNoTraitRecord = saveData.stageNoTraitRecord;
    }

    private void OnValidate()
    {
        currentWave = GetWaveFromWaveType(setWave);
        ShowNextWaveText();
    }

    // ---------- 가중치 기반 & 리롤 ----------
    private static int? CurrentStage => GameManager.Instance != null ? GameManager.Instance.stage : (int?)null;

    private WaveType PickNextByWeight() => deck.PickNext(currentWave?.WaveType, CurrentStage);

    // 저주(이중 웨이브): 현재 웨이브와 다른, 이미 정식 등장 중인 웨이브 하나를 무작위 선택.
    private Wave PickSecondWave(WaveType exclude)
    {
        WaveType? second = deck.PickSecond(exclude, CurrentStage);
        return second.HasValue ? GetWaveFromWaveType(second.Value) : null;
    }

    /// <summary>웨이브를 버티지 못한 식물 집계.</summary>
    public void RecordWaveHit(WaveType wave)
    {
        waveKillCount[(int)wave] += 1;
        stageKillRecord[GameManager.Instance.stage]++;
    }

    public void TutorialWave()
    {
        StartCoroutine(TEnemyWaveCoroutine());
    }

    public IEnumerator TutorialWaveCoroutine()
    {
        yield return TEnemyWaveCoroutine();
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
            for (int idx = 0; idx < grid.Board.MaxCol * 4; idx++)
            {
                if (grid.Board.Plants.ContainsKey(idx))
                {
                    Plant plant = grid.Board.Plants[idx];

                    if (idx == 0 || idx == 2) plant.Die();
                    else plant.ResistWave(wave.WaveType);
                }
            }
        }


        //SetNextWave();
        //breedTimerManager 의 null reference issue를 에디터에서 함수 분리해서 해결
        lastWave = currentWave;
        currentWave = nextWave ?? GetWaveFromWaveType(WaveType.None);
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

    public WaveType GetMostKillWaveType()
    {
        int maxKills = waveKillCount.Max();
        return (WaveType)waveKillCount.ToList().IndexOf(maxKills);
    }

    public void SkipWaveEffect()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetGameIsStopped())
            return;

        isWaveSkipped = true;
        waveManager.SkipWaveEffect();
    }

    public int PickTraitFromUnlockWave()
    {
        int traitType = (int)PickNextByWeight();

        return traitType;
    }
}
