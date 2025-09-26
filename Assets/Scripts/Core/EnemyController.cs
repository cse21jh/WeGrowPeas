using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;


public class EnemyController : MonoBehaviour
{
    private static readonly Dictionary<WaveType, Wave> waves = new Dictionary<WaveType, Wave>
    {
        { WaveType.Aging, new AgingWave() },
        { WaveType.Pest, new PestWave() },
        { WaveType.Wind, new WindWave() },
        { WaveType.Flood, new FloodWave() },
        { WaveType.HeavyRain, new HeavyRainWave() },
        { WaveType.Cold, new ColdWave() }
    };

    public Grid grid;


    private Wave noneWave;

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

    [SerializeField] private Dictionary<WaveType, float> baseWeights = new Dictionary<WaveType, float>();

    //int CurrentDay => GameManager.Instance.stage;


    // 아래 저장 필요
    
    private Wave lastWave;
    private Wave currentWave;
    private Wave nextWave;
    private int waveSkipCount = 0;
    private int[] waveKillCount;
    

    public Wave CurrentWave => currentWave;
    public Wave NextWave => nextWave;
    public Wave LastWave => lastWave;
    public int WaveSkipCount => waveSkipCount;
    public int[] WaveKillCount => waveKillCount;

    // Start is called before the first frame update
    void Start()
    {
        waveKillCount = new int[7];
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
        //Debug.Log($"웨이브 디버깅 좀 하겟습니다 {unlockedWave.Count}");
        Wave wave = currentWave;
        Debug.Log("현재 웨이브 종류 : " + currentWave);
        PlayerRecordForGraph.SetWED((int)wave.WaveType);
        SoundManager.Instance.PlayEffect(wave.WaveSoundString);

        if (waveManager != null)
        {
            waveManager.StartWave(waveDuration, wave.WaveType);
        }

        float t = 0f;
        float waveD = waveDuration;
        while (t < waveD)
        {
            t += Time.deltaTime;
            yield return null;
            if (Input.GetKeyDown(KeyCode.S))
            {
                float tmp;
                tmp = waveDuration;
                waveD= 0f;
                waveManager.SkipWaveEffect();
            }
        }        

        if (currentWave != noneWave)
        {
            for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
            {
                if (grid.plantGrid.ContainsKey(idx))
                {
                    Plant plant = grid.plantGrid[idx];

                    if (plant.CanResist(wave.WaveType))
                    {
                        Debug.Log(idx + "번째 식물이 웨이브를 버텼습니다");
                        plant.ResistWave(wave.WaveType);
                    }
                    else
                    {
                        waveKillCount[(int)currentWave.WaveType] += 1;
                        Debug.Log(idx + "번째 식물이 죽었습니다");
                        plant.Die();
                    }

                }
            }
        }

        if (grid.IsIceBlockActivated())
            grid.DeactivateIceBlock();

        SetNextWave();
        //FlushNextWaveText();
        yield return null;
    }

    private void InitBaseWeightsByStage(int stage)
    {
        // 초기화
        baseWeights[WaveType.Aging] = 1f; // 항상 가능
        baseWeights[WaveType.Pest] = (stage + 2 >= PestWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Wind] = (stage + 2 >= WindWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Flood] = (stage + 2 >= FloodWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.HeavyRain] = (stage + 2 >= HeavyRainWave.UnlockStage) ? 1f : 0f;
        baseWeights[WaveType.Cold] = (stage + 2 >= ColdWave.UnlockStage ) ? 1f : 0f;
        baseWeights[WaveType.None] = 0f; // 추첨 대상에서 제외
    }

    public void UnlockWave(int stage)
    {
        switch (stage + 2)
        {
            case PestWave.UnlockStage: baseWeights[WaveType.Pest] = 1f; break;
            case WindWave.UnlockStage: baseWeights[WaveType.Wind] = 1f; break;
            case FloodWave.UnlockStage: baseWeights[WaveType.Flood] = 1f; break;
            case HeavyRainWave.UnlockStage: baseWeights[WaveType.HeavyRain] = 1f; break;
            case ColdWave.UnlockStage: baseWeights[WaveType.Cold] = 1f; break;
        }
    }

    public void SetNextWave()
    {
        lastWave = currentWave;
        currentWave = nextWave;
        setWave = currentWave.WaveType;
        FenceUIManager.Instance.SetWaveHighlight(currentWave);
        breedTimerManager.SetTimer(currentWave.WaveType);
        ShowNextWaveText();
        WaveType picked = PickNextByWeight();
        nextWave = GetWaveFromWaveType(picked);
    }

    public void WaveSkip()
    {
        if (grid.GetIsBreeding() && waveSkipCount > 0 && currentWave!=noneWave)
        {
            currentWave = noneWave;
            waveSkipCount--;
            SetWaveSkipCountText();            
            ShowNextWaveText();
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
            WaveType.None => new NoneWave(),
        };
    }

    public void LoadEnemyController(SaveData saveData)
    {
        InitBaseWeightsByStage(GameManager.Instance.stage);

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
    }

    private void OnValidate()
    {
        currentWave = GetWaveFromWaveType(setWave);
        ShowNextWaveText();
    }

    // ---------- 가중치 계산 & 추첨 ----------
    private Dictionary<WaveType, float> BuildEffectiveWeights()
    {
        // 기본 가중치 복사
        var map = new Dictionary<WaveType, float>(baseWeights);

        foreach (var t in (WaveType[])System.Enum.GetValues(typeof(WaveType)))
        {
            if (t == WaveType.None) { map[t] = 0f; continue; }

            float modMul = ModManager.Instance
                ? ModManager.Instance.GetMul(StatId.WaveWeightMul, (int)t)
                : 1f;

            if (map.ContainsKey(t))
                map[t] *= modMul;
        }

        // None은 뽑기 제외
        map[WaveType.None] = 0f;
        return map;
    }

    private WaveType PickNextByWeight()
    {
        var map = BuildEffectiveWeights();

        // 합 계산
        float sum = 0f;
        foreach (var kv in map) sum += kv.Value;

        // 전부 0인 경우 안전값
        if (sum <= 0f)
            return WaveType.Aging;

        float r = Random.Range(0f, sum);
        float acc = 0f;
        foreach (var kv in map)
        {
            acc += kv.Value;
            if (r <= acc) return kv.Key;
        }
        return WaveType.Aging; // 부동소수점 안전장치
    }

    public void TutorialWave()
    {
        StartCoroutine(TEnemyWaveCoroutine());
    }

    private IEnumerator TEnemyWaveCoroutine()
    {
        //Debug.Log($"웨이브 디버깅 좀 하겟습니다 {unlockedWave.Count}");
        Wave wave = currentWave;
        Debug.Log("currentWave : " + currentWave);
        SoundManager.Instance.PlayEffect(wave.WaveSoundString);

        if (waveManager != null)
        {
            waveManager.StartWave(waveDuration, wave.WaveType);
        }

        yield return new WaitForSeconds(waveDuration); // 웨이브 이펙트 재생 중 대기

        if (currentWave != noneWave)
        {
            for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
            {
                if (grid.plantGrid.ContainsKey(idx))
                {
                    Plant plant = grid.plantGrid[idx];

                    if (idx == 0 || idx == 2) plant.Die();
                }
            }
        }

        if (grid.IsIceBlockActivated())
            grid.DeactivateIceBlock();

        //SetNextWave();
        //breedTimerManager 쪽 null reference issue로 부득이하게 함수 복사해서 사용
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
}
