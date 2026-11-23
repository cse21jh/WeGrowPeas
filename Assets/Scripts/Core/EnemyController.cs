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

    /// <summary>오늘과 다음 웨이브. 추첨 가중치의 배수(표지판 등)는 ModManager에서 온다.</summary>
    public WaveState Waves { get; } = new WaveState
    {
        Deck = { WeightMultiplier = type => ModManager.Instance ? ModManager.Instance.GetMul(StatId.WaveWeightMul, (int)type) : 1f },
    };

    private bool isWaveSkipped = false;

    //int CurrentDay => GameManager.Instance.stage;


    private Season currentSeason = Season.Spring; // 세이브데이터에서 불러와야함
    [SerializeField] private SeasonManager seasonManager;





    private int[] waveKillCount;
    private string shownWaveText;

    private List<WaveType> stageWaveRecord = new List<WaveType>(); // 세이브데이터의 웨이브, 좀 더 확인 >> 엔딩조건 체크하기 위해 필요
    private List<int> stageKillRecord = new List<int>();
    private List<int> stageNoTraitRecord = new List<int>();

    public Season CurrentSeason => currentSeason;
    public Wave CurrentWave => GetWaveFromWaveType(Waves.Current);
    public Wave NextWave => ToWave(Waves.Next);

    public Wave GetNextWave() => NextWave;
    public Wave GetNextSecondWave() => ToWave(Waves.Second);
    public Wave LastWave => ToWave(Waves.Last);
    public int WaveSkipCount => Waves.Skips;

    private static Wave ToWave(WaveType? type) => type.HasValue ? GetWaveFromWaveType(type.Value) : null;

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

        Waves.Init(CurrentStage);

        setWave = Waves.Current;
        FenceUIManager.Instance.SetWaveHighlight(CurrentWave);
    }

    private bool fullFieldWave;
    private int plantsBeforeWave;

    /// <summary>웨이브 연출을 보여주고 판정 전의 기록을 남긴다. 판정은 <see cref="DayFlow"/>가 한다.</summary>
    public IEnumerator PlayWaveRoutine()
    {
        Wave wave = CurrentWave;
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

        stageWaveRecord.Add(Waves.Current);
        stageKillRecord.Add(0);
        stageNoTraitRecord.Add(grid.Board.CountNoTraitPlant(Waves.Current));

        // 업적: 모든 칸이 찬 상태에서 웨이브를 맞는지 기록
        bool tutorial = GameManager.Instance.Tutorial != null;
        fullFieldWave = Waves.Current != WaveType.None && !tutorial && !grid.Board.HasEmptyCell();
        plantsBeforeWave = grid.Board.Plants.Count;
    }

    /// <summary>웨이브 판정이 끝나고 다음 웨이브가 정해진 뒤의 화면 처리.</summary>
    public void OnWaveResolved()
    {
        SetNextSeason();
        setWave = Waves.Current;
        FenceUIManager.Instance.SetWaveHighlight(CurrentWave);
        GameEvents.RaiseWaveScheduleChanged(); // 예보 UI(홈 위젯 등) 즉시 갱신
        RefreshAfterWave();
    }

    private void RefreshAfterWave()
    {
        if (fullFieldWave) AchievementHooks.OnFullFieldWaveEnded(plantsBeforeWave, grid.Board.Plants.Count);

        grid.Scouter.UpdateGoldScouterImageInGrid(); // 웨이브로 인해 단체로 죽을 때는 한 번만 갱신해서 연산량 줄이기
        grid.Scouter.UpdateResistanceScouterImageInGrid(Waves.Current); // 웨이브로 인해 저항력이 단체로 감소할 때는, 한 번만 갱신해서 연산량 줄이기
    }

    /// <summary>튜토리얼의 웨이브. 다음 웨이브는 튜토리얼이 정한다.</summary>
    public IEnumerator TutorialChapterWaveRoutine()
    {
        yield return PlayWaveRoutine();
        GameManager.Instance.Day.ResolveWave();
        Waves.Last = Waves.Current;
        RefreshAfterWave();
        yield return null;
    }

    public void SetTutorialWave(WaveType current, WaveType next)
    {
        Waves.Current = current;
        Waves.Next = next;
        setWave = current;
        FenceUIManager.Instance.SetWaveHighlight(CurrentWave);
        breedTimerManager.SetTimer(current);
        ShowNextWaveText();
        GameEvents.RaiseWaveScheduleChanged();
    }

    public void RecordTutorialDayWithoutWave()
    {
        Waves.Last = Waves.Current;
        stageWaveRecord.Add(WaveType.None);
        stageKillRecord.Add(0);
        stageNoTraitRecord.Add(0);
    }

    public void AdvanceTutorialWave(WaveType following)
    {
        Waves.Last = Waves.Current;
        SetTutorialWave(Waves.Next.Value, following);
    }

    public void UpdateSecondWaveIfNull() => Waves.PrepareSecond(CurrentStage);

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
        Waves.Next = waveType;
        ShowNextWaveText();


        Debug.Log($"[Debug] 다음 웨이브 → {waveType}");
    }

    public void SetCurrentWaveTimer()
    {
        breedTimerManager.SetTimer(Waves.Current);
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

        if (grid.Breeder.GetIsBreeding() && Waves.SkipToday())
        {
            SetWaveSkipCountText();
            ShowNextWaveText();
        }

        if (Waves.Skips <= 0)
            HideWaveSkipButton();
        return;
    }

    public string NextWaveDescription => CurseState.WaveBlind ? "???" : CurrentWave.WaveDescription;

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
        Waves.Skips += count;
        SetWaveSkipCountText();
        return;
    }

    private void SetWaveSkipCountText()
    {
        if (waveSkipCountText == null)
            return;

        waveSkipCountText.text = Loc.T("game.hud.extra_skips", ("count", Waves.Skips));
        return;
    }

    public void ShowWaveSkipButton()
    {
        if (waveSkipButton == null)
            return;

        if (Waves.Skips > 0)
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
        Waves.Deck.Init(stage);

        Waves.Current = saveData.curWaveType;
        grid.Scouter.UpdateResistanceScouterImageInGrid(Waves.Current);
        setWave = Waves.Current;
        Waves.Last = saveData.lastWaveType;
        Waves.Next = saveData.nextWaveType;

        Waves.Skips = saveData.remainWaveSkipCount;
        for (int i = 0; i < Wave.NumberOfWave; i++)
            waveKillCount[i] = saveData.waveKillCount[i];
        FenceUIManager.Instance.SetWaveHighlight(CurrentWave);
        ShowNextWaveText();
        breedTimerManager.SetTimer(Waves.Current);

        SetSeason(saveData.currentSeason);

        stageWaveRecord = saveData.stageWaveRecord;
        stageKillRecord = saveData.stageKillRecord;
        stageNoTraitRecord = saveData.stageNoTraitRecord;
    }

    private void OnValidate()
    {
        Waves.Current = setWave;
        ShowNextWaveText();
    }

    // ---------- 가중치 기반 & 리롤 ----------
    private static int? CurrentStage => GameManager.Instance != null ? GameManager.Instance.stage : (int?)null;

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
        Wave wave = CurrentWave;
        SoundManager.Instance.PlayEffect(wave.WaveSoundString);

        if (waveManager != null)
        {
            waveManager.StartWave(waveDuration, wave.WaveType);
        }

        yield return new WaitForSeconds(waveDuration); // 웨이브 이펙트 끝 날 때까지

        if (Waves.Current != WaveType.None)
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
        Waves.Advance(CurrentStage);
        setWave = Waves.Current;
        FenceUIManager.Instance.SetWaveHighlight(CurrentWave);
        ShowNextWaveText();

        Waves.Current = WaveType.Aging;
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
        return (int)Waves.Deck.PickNext(Waves.Current, CurrentStage);
    }
}
