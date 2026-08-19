
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;


public class GameManager : Singleton<GameManager>
{
    public int stage = 0;
    [HideInInspector] public bool seenFirstGold = false;
    public string currentPlant = "땅콩";

    //위는 저장 필요

    private bool gameOver = false;
    public int requestCycle = 2;

    private bool isStopped = false;

    private int gameMode = 0; //0: normal 1: endless

    /// <summary>저주가 이번 런에서 도는가. 무한모드(1) 또는 새벽 모드(저주 레벨 &gt; 0)일 때.</summary>
    private bool CurseActive => gameMode == 1 || DawnSystem.Current.curseLevel > 0;

    public Grid grid;
    public EnemyController enemyController;
    public WaveManager waveManager;
    public ShopManager shopManager;
    public EconomyManager economyManager;
    public ModManager modManager;
    public RequestManager requestManager;
    public PhoneManager phoneManager;
    public CurseManager curseManager;

    [SerializeField] private GlowCanvasController gcController;

    [SerializeField] private TextMeshProUGUI textStage;

    [SerializeField] private int endStage = 40;

    // Start is called before the first frame update
    void Start()
    {
        SoundManager.Instance.StopBgm();
        SoundManager.Instance.PlayBgm("Farm");

        Time.timeScale = 1;

        ClickRouter.Instance.IsBlockedByUI = false;

        // 결과창에서 "이번 판에 새로 해금된 아이템"을 뽑기 위한 시작 시점 스냅샷
        UnlockRunTracker.CaptureRunStart();

        switch (GameStartContext.StartType)
        {
            case GameStartType.NewGame:
                Debug.Log("새 게임");
                if (AbilityManager.Instance != null)
                    AbilityManager.Instance.ApplyAbilities(this);
                enemyController.InitEnemyController();
                grid.InitGrid();                
                economyManager.InitEconomyManager();
                shopManager.InitializeGameSeed(); // 새 게임 시작 시 게임 고유 시드 초기화                
                PlayerRecordForGraph.ClearAll();
                RecallRecorder.ResetRun(); // 회상: 일자별 스냅샷 초기화
                CurseState.ResetAll(); // 저주 상태 초기화
                SpecialItemSystem.ResetRun(); // 특수 아이템 초기화
                gameMode = 0;
                StageUpdate();
                break;

            case GameStartType.ContinueGame:
                Debug.Log("불러오기");
                enemyController.InitEnemyController();
                LoadGame();
                gameMode = (stage > endStage) ? 1 : 0;
                break;

            case GameStartType.ContinueAfterEnding:
                Debug.Log("40일 이후 계속 불러오기");
                LoadGame();
                gameMode = 1;
                break;
        }

        if (gameMode == 1) StartCoroutine(StartEndlessGameMode()); 
        else StartCoroutine(StartNormalMode());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        GameEvents.OnSaveGameRequested += SaveGame;
    }

    private void OnDisable()
    {
        GameEvents.OnSaveGameRequested -= SaveGame;
    }

    IEnumerator StartNormalMode()
    {
        while (!gameOver)
        {
            UpdateStageUI();

            if (CurseActive) curseManager.ApplyCurse(); // 새벽: 전날 밤 선택된 저주 발동

            yield return StartCoroutine(StartStage());

            if (stage == endStage)
            {
                economyManager.PushEarnedGold();
                yield return StartCoroutine(ClearNormalMode());
                break;
            }

            StageUpdate();
            SaveGame();
        }
    }

    IEnumerator StartEndlessGameMode()
    {
        Debug.Log("무한 모드입니다.");

        while(!gameOver)
        {
            UpdateStageUI();

            curseManager.ApplyCurse();

            yield return StartCoroutine(StartStage());

            //curseManager.SelectCurse(stage);
            StageUpdate();
            SaveGame();
        }
    }

    private void StageUpdate()
    {
        stage++;
        ModManager.Instance?.OnNewDay(stage);
        PhoneManager.Instance.messengerApp.RefreshchatPartnerList();
        enemyController.UnlockWave(stage);
        
        // 매일 상점 자동 리롤 (비활성화된 오브젝트도 포함해서 찾기)
        // 구 UI(ShopUI)와 신규 UI(ShopCanvasController)를 모두 갱신 — 한쪽만 씬에 있어도 동작.
        var shopUIs = FindObjectsOfType<ShopUI>(true);
        if (shopUIs != null && shopUIs.Length > 0 && shopUIs[0] != null)
        {
            shopUIs[0].DailyReroll();
        }

        var shopCanvases = FindObjectsOfType<ShopCanvasController>(true);
        if (shopCanvases != null && shopCanvases.Length > 0 && shopCanvases[0] != null)
        {
            shopCanvases[0].DailyReroll();
        }
    }

    IEnumerator StartStage()
    {
        // 세금 연체 시 낮 시작 직후(웨이브 전) 강제징수/압류
        if (TaxManager.Instance != null && TaxManager.Instance.HasOverdueTax(stage))
            yield return StartCoroutine(TaxCollectionRoutine());

        enemyController.ShowNextWaveText();

        if (stage % 5 == requestCycle) requestManager.StartNewCycle(stage);

        yield return StartCoroutine(grid.Breeding());

        yield return StartCoroutine(enemyController.EnemyWaveCoroutine());

        gameOver = grid.CheckGameOver();

        if (gameOver)
            yield return null;

        yield return new WaitForSeconds(2.0f);
        PlayerRecordForGraph.SetSP(grid.plantGrid.Count);

        /*if (stage == endStage)
        {
            economyManager.PushEarnedGold();
            yield return StartCoroutine(ClearNormalMode());
        }*/

        yield return StartCoroutine(BreedEndRoutine());

        GameEvents.RaiseDayPassedForRequest(); //NoSellPeaRequest Check + 저주 만료 처리


        // 여기다가 밤으로 바뀌는 이펙트 추가해야 함
        gcController.ToggleGlow(true);
        yield return waveManager.StartCoroutine(waveManager.StartNightCoroutine());

        if (CurseActive) curseManager.SelectCurse(stage);

        // 특수 아이템: 엔딩 전 10·20·30일 밤에 선물 도착 (수령 전까지 유지).
        // 폰에서 수령하므로 폰 페이즈 직전에 지급한다.
        if (stage % 10 == 0 && stage < endStage)
        {
            SpecialItemSystem.AddGift();
            PhoneNotificationBus.OnShow?.Invoke(new PhoneNotificationData
            {
                title = "완두콩의 선물이 도착했습니다!",
                message = "선물 버튼을 눌러 특수 아이템을 수령하세요.",
                duration = 5f
            });
        }

        yield return StartCoroutine(phoneManager.PhonePhase());


        gcController.ToggleGlow(false);
        yield return waveManager.StartCoroutine(waveManager.StopNightCoroutine());
        /*
        if (!enemyController.IsLastWaveNone())
            yield return StartCoroutine(upgradeManager.UpgradePhase());            
        
        yield return StartCoroutine(shopManager.ShopPhase(grid));
        */
        GameEvents.RaiseQuestDayPassed();

        // 회상: 자유시간이 끝난 지금이 하루의 마지막 상태.
        // PushEarnedGold보다 앞이어야 오늘 번 골드(EarnedGoldToday)가 아직 살아 있다.
        RecallRecorder.CaptureDay();

        economyManager.PushEarnedGold();
    }

    private void UpdateStageUI()
    {
        textStage.text = $"{stage}";
    }

    /// <summary>엔딩(클리어) 일차.</summary>
    public int EndStage => endStage;

    /// <summary>
    /// 디버그 전용: 현재 일차를 강제로 변경한다.
    /// 진행 중인 하루는 그대로 끝나고, 그 뒤 일차 판정부터 새 값이 적용된다.
    /// (endStage로 맞추면 이번 날이 끝나는 즉시 엔딩으로 진입)
    /// </summary>
    public void DebugSetStage(int newStage)
    {
        stage = Mathf.Clamp(newStage, 1, endStage);
        // 일차별 기록 리스트(kill/wave/noTrait)를 새 일차 크기까지 0으로 채워 인덱스 접근 오류를 막는다.
        enemyController?.DebugPadStageRecords(stage);
        UpdateStageUI();
        Debug.Log($"[Debug] 일차를 {stage}일차로 변경했습니다.");
    }

    public IEnumerator BreedEndRoutine()
    {
        Plant plant;
        List<Peanut> peanutList = new List<Peanut>();
        for (int idx = 0; idx < grid.maxCol * 4; idx++)
        {
            plant = null;
            grid.plantGrid.TryGetValue(idx, out plant);
            if (plant == null)
                continue;
            if (plant.GetType() == typeof(Peanut))
                peanutList.Add(plant.gameObject.GetComponent<Peanut>());
        }
        for (int i = 0; i < peanutList.Count; i++)
            peanutList[i].TrySpawnCopy();

        // 땅과 콩: 웨이브가 지나간 후 식물이 확률적으로 뿌리를 내림
        grid?.ProcessLandAndBeanRooting();

        // 무당벌레당 골드 지급 (웨이브 종료 시)
        if (grid != null && grid.AdditionalLadybugGoldPerUnit > 0 && grid.ladybugs != null)
        {
            int ladybugGold = grid.ladybugs.Count * grid.AdditionalLadybugGoldPerUnit;
            if (ladybugGold > 0)
            {
                economyManager.AddGold(ladybugGold);
            }
        }

        // 치료형 캡사이신: 고추 주변 식물의 저항력 회복
        if (grid != null && grid.ChiliPepperHealPercent > 0f)
        {
            foreach (var currentPlant in grid.plantGrid.Values)
            {
                if (currentPlant is ChiliPepper chiliPepper)
                {
                    var nearbyPlants = grid.GetPlantsNearChiliPepper(chiliPepper);
                    foreach (var nearbyPlant in nearbyPlants)
                    {
                        // 모든 저항력을 회복 (구매 횟수 * 3%)
                        var traits = nearbyPlant.GetGeneticTrait();
                        for (int i = 0; i < traits.Count; i++)
                        {
                            float healAmount = grid.ChiliPepperHealPercent;
                            float currentResistance = traits[i].resistance;
                            float newResistance = Mathf.Clamp(currentResistance + healAmount, 0.1f, 1.0f);
                            traits[i] = new GeneticTrait(traits[i].traitType, newResistance, traits[i].genetics, traits[i].additionalResistance);
                        }
                        nearbyPlant.SetTrait(traits);
                    }
                }
            }
        }
        
        yield return null;
    }

    // 세금 연체 강제징수(낮 시작): 있으면 강제 차감, 부족하면 음수 + 10초 압류 페이즈 → 강제매각.
    public IEnumerator TaxCollectionRoutine()
    {
        int owed = TaxManager.Instance != null ? TaxManager.Instance.OverdueAmount : 0;
        TaxManager.Instance?.MarkPaidForcibly(); // 세금 자체는 징수 처리(다음 세금일로)

        // 강제징수 처리됐으니 국세청 앱 red dot(알람) 끔
        if (phoneManager != null)
            phoneManager.UpdateAppAlarmState(AppKey.Tax, AlarmState.None);

        if (owed <= 0) yield break;

        economyManager.SpendGold(owed); // 강제 차감(부족하면 음수)

        if (economyManager.GetGold() >= 0)
        {
            Debug.Log($"[Tax] 강제징수 {owed} 완료(잔액 충분).");
            yield break;
        }

        // 부족 → 빚(음수). 압류 대상은 삽으로 팔 수 있는 Pea/Peanut만.
        var seized = new System.Collections.Generic.HashSet<Plant>();
        RefreshSeizure(seized);
        Debug.Log($"[Tax] 세금 부족! 빚 {-economyManager.GetGold()}, 식물 {seized.Count}개 압류. 10초 내 상환 필요.");

        // 10초 유예: 기존 타이머 UI 재사용. 삽으로 '안 압류' 식물을 팔면 골드↑ → 압류 스티커 실시간 갱신.
        // (웨이브 전이라 StopGame 불필요 — 안 멈춰야 타이머가 흐르고 삽질 가능)
        phoneManager.StartTaxTimer(10);
        float t = 10f;
        int lastGold = economyManager.GetGold();
        while (t > 0f && economyManager.GetGold() < 0)
        {
            t -= Time.deltaTime;
            int g = economyManager.GetGold();
            if (g != lastGold) { lastGold = g; RefreshSeizure(seized); } // 골드 변할 때마다 스티커 갱신
            yield return null;
        }
        phoneManager.StopTaxTimer();

        if (economyManager.GetGold() >= 0)
        {
            foreach (var p in seized) if (p != null) p.SetSeized(false); // 상환 완료 → 압류 해제
            Debug.Log("[Tax] 상환 완료 — 압류 해제.");
        }
        else
        {
            // 압류분 강제매각(비싼 것부터, 골드 0 이상 될 때까지)
            var toSell = new System.Collections.Generic.List<Plant>(seized);
            toSell.Sort((a, b) => b.GetSellingPrice().CompareTo(a.GetSellingPrice()));
            foreach (var p in toSell)
            {
                if (p == null) continue;
                if (economyManager.GetGold() >= 0) { p.SetSeized(false); continue; }
                economyManager.AddGold(p.GetSellingPrice());
                p.Die(DeathCause.Other);
                yield return new WaitForSeconds(0.12f); // 순차 매각 연출
            }
            Debug.Log("[Tax] 압류분 강제매각 완료.");
        }

        // 다 팔려 Pea/Peanut이 없으면 게임오버
        if (grid.CheckGameOver())
            yield return StartCoroutine(GameOver());
    }

    // 현재 빚(-gold)만큼 Pea/Peanut을 비싼 순으로 압류. seized 집합과 스티커를 갱신.
    private void RefreshSeizure(System.Collections.Generic.HashSet<Plant> seized)
    {
        int debt = Mathf.Max(0, -economyManager.GetGold());

        var seizable = new System.Collections.Generic.List<Plant>();
        foreach (var p in grid.plantGrid.Values)
            if (p != null && (p is Pea || p is Peanut)) seizable.Add(p);
        seizable.Sort((a, b) => b.GetSellingPrice().CompareTo(a.GetSellingPrice()));

        var needed = new System.Collections.Generic.HashSet<Plant>();
        int sum = 0;
        foreach (var p in seizable)
        {
            if (sum >= debt) break;
            needed.Add(p);
            sum += p.GetSellingPrice();
        }

        // 더 이상 불필요한 압류 해제
        foreach (var p in new System.Collections.Generic.List<Plant>(seized))
            if (!needed.Contains(p)) { if (p != null) p.SetSeized(false); seized.Remove(p); }
        // 새로 필요한 압류
        foreach (var p in needed)
            if (seized.Add(p)) p.SetSeized(true);
    }

    public IEnumerator GameOver()
    {
        if (gameOver) yield break; // 중복 호출 방지(자동 트리거 + 명시 호출)
        gameOver = true;

        //Debug.Log("게임오버");
        PlayerRecordForGraph.SetSP(grid.plantGrid.Count);

        // 회상: 게임오버 당일은 자유시간을 채우지 못하고 끝나므로 여기서 마지막 스냅샷을 찍는다.
        // PushEarnedGold보다 앞이어야 오늘 번 골드가 아직 남아 있다.
        RecallRecorder.CaptureDay(isFinalPartial: true);

        economyManager.PushEarnedGold();
        yield return new WaitForSeconds(1.0f);

        // 요약의 일수 보정(stage - 1)이 걸리도록 결과 종류를 먼저 확정한다.
        GameStartContext.SetStartType(GameStartType.GameOver);

        // 런 종료 공통 처리 — 세이브 파일을 지우기 전에 끝나야 타임라인이 살아 있다.
        yield return StartCoroutine(FinishRunRoutine());

        // 화면 덮기/열기 연출은 SceneLoader가 담당한다.
        SceneLoader.Instance.LoadGameOverScene();
        File.Delete(GetSavePath());
        //Time.timeScale = 0.0f;
        Debug.Log("GameOver");
    }
     
    private IEnumerator ClearNormalMode()
    {
        if (TaxManager.Instance != null && TaxManager.Instance.DueTaxStage == 40)
        {
            TaxManager.Instance.MarkPaidForcibly();
        }

        DawnSystem.RecordRunCleared(); // 엔딩 도달: 다음 새벽 단계 해금(= 이번 단계 클리어 기록)

        // 런 종료 공통 처리. 엔딩 연출(카메라 전환 + 편지)이 시작되기 전이라
        // 게임오버 때와 같은 농장 시점으로 사진이 남는다.
        yield return StartCoroutine(FinishRunRoutine());

        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Ending);
        //File.Delete(GetSavePath());
        //Time.timeScale = 0.0f;
        stage++;
        GameStartContext.SetStartType(GameStartType.ContinueAfterEnding);
        SaveGame();
        Debug.Log("40일 클리어했습니다. YEAH!");
        while(true)
            yield return null;
    }

    /// <summary>
    /// 런 종료 공통 처리. 끝난 방식(엔딩/게임오버)을 가리지 않는다.
    /// 요약 확정 → 유전자 지급 → 농장 사진 + 회상 기록 순서.
    ///
    /// 유전자 지급이 결과 화면이 아니라 여기 있는 이유는, 결과 화면을 회상으로 다시 열어도
    /// 다시 지급되지 않게 하기 위해서다. 지급 결과는 회상 기록에도 그대로 남는다.
    /// </summary>
    private IEnumerator FinishRunRoutine()
    {
        PassRecordToGameRecordHolder();
        RunRecordFormatter.AwardGenetics(GameRecordHolder.Current);

        byte[] png = null;
        yield return StartCoroutine(RecallScreenshot.CaptureRoutine(bytes => png = bytes));
        RecallStore.Commit(png);
    }

    private void LoadGame()
    {
        string json = File.ReadAllText(GetSavePath());
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        // 진행 상황
        stage = saveData.progress.stage;
        seenFirstGold = saveData.progress.seenFirstGold;
        currentPlant = saveData.progress.currentPlant;
        if (TaxManager.Instance != null) TaxManager.Instance.LoadFromSave(saveData.progress.lastPaidTaxStage);
        DawnSystem.SetSelectedStage(saveData.progress.selectedDawnStage); // 새벽 제약이 이어하기에도 적용되도록

        // 시스템별 복원. 각 필드가 무엇인지는 해당 시스템의 LoadXxx가 안다.
        // 순서를 바꾸면 로드 중 초기화 순서가 달라지므로 주의.
        grid.LoadGrid(saveData.grid);
        enemyController.LoadEnemyController(saveData.wave, saveData.progress.stage);
        economyManager.LoadEconomyManager(saveData.economy);
        shopManager.LoadShopManager(saveData.shop);
        modManager.LoadModManager(saveData.mod);
        requestManager.LoadRequestManager(saveData.request);
        phoneManager.LoadPhoneManager(saveData.phone);
        curseManager.LoadCurseManager(saveData.curse);
        SpecialItemSystem.LoadFromSave(saveData.specialItem);
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.LoadCurrentAbilityManager(saveData.ability, saveData.progress.currentPlant);
        }
        PlayerRecordForGraph.SetDataFromLoad(saveData.graph);
        RecallRecorder.LoadFromSave(saveData.recall);

        Debug.Log("불러옴");
    }

    private void SaveGame()
    {
        var saveData = new SaveData();

        // 진행 상황
        saveData.progress.stage = stage;
        saveData.progress.seenFirstGold = seenFirstGold;
        saveData.progress.currentPlant = currentPlant;
        saveData.progress.lastPaidTaxStage = TaxManager.Instance != null ? TaxManager.Instance.GetSaveValue() : 0;
        saveData.progress.selectedDawnStage = DawnSystem.SelectedDawnStage;
        // 불러올 때 무조건 이어하기 상태로 시작하도록 정규화해서 저장
        saveData.progress.gst = stage > endStage ? GameStartType.ContinueAfterEnding : GameStartType.ContinueGame;

        // 시스템별 저장. LoadGame과 같은 순서로 두어 짝을 눈으로 확인할 수 있게 한다.
        grid.SaveGrid(saveData.grid);
        enemyController.SaveEnemyController(saveData.wave);
        economyManager.SaveEconomyManager(saveData.economy);
        shopManager.SaveShopManager(saveData.shop);
        modManager.SaveModManager(saveData.mod);
        requestManager.SaveRequestManager(saveData.request);
        phoneManager.SavePhoneManager(saveData.phone);
        curseManager.SaveCurseManager(saveData.curse);
        SpecialItemSystem.SaveTo(saveData.specialItem);
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.SaveCurrentAbilityManager(saveData.ability);
        }
        PlayerRecordForGraph.SaveTo(saveData.graph);
        RecallRecorder.SaveTo(saveData.recall);

        File.WriteAllText(GetSavePath(), JsonUtility.ToJson(saveData, true));
        GameStartContext.SetStartType(GameStartType.ContinueGame);

        Debug.Log("저장됨");
    }

    private string GetSavePath()
    {
        string defaultPath = Application.dataPath + "/UserData_2.json";

        if (SaveContext.Instance == null) return defaultPath;

        return SaveContext.Instance.CurrentSaveFilePath;
    }

    private void PassRecordToGameRecordHolder()
    {
        string mostKillWaveName = enemyController.GetMostKillWaveName();
        string itemName = shopManager.ReturnMostPurchasedItem();

        GameRecordHolder.SaveRecord(stage,
            grid.totalPeaBreedcount,
            grid.totalPeanutBreedCount,
            economyManager.PeaSellCount,
            economyManager.PeanutSellCount,
            grid.killBugCount,
            economyManager.TotalGold,
            economyManager.ConsumeGold,
            mostKillWaveName,
            itemName,
            grid.MostExpensivePlant,
            requestManager.CompleteRequestCount
            );
            

    }

    public void StopGame()
    {
        isStopped = true;
    }

    public void ResumeGame()
    {
        isStopped = false;
    }

    public bool GetGameIsStopped()
    {
        return isStopped;
    }
}
