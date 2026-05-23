
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

[System.Serializable]
public class SaveData
{
    //gameManager
    public int stage;
    public bool seenFirstGold;
    public string currentPlant;

    //grid
    public List<PlantData> plantList = new();
    public int remainBreedCount;

    public int maxCol;

    public int killBugCount;
    public int totalBreedCount;
    public int totalPeaBreedcount;
    public int totalPeanutBreedCount;

    public float bugSpawnTimeInterval;
    public float lastBugSpawnTimeInterval;

    public float bugSpeedDecreasement;
    public float bugSpawnIntervalIncreasement;
    public float ladybugSpawnProbability;
    public int maxLadybugCount;
    public int additionalLadybugGoldPerUnit;
    public float additionalLadybugResistancePerUnit;
    public int additionalBugGold;
    public int additionalNepenthesGold;
    public bool hasNepenthesPheromone;
    public float additionalNepenthesPheromoneSizeMultiplier;
    public float nepenthesSpawnProbability;

    public float weakGeneticsResistanceBonus;
    public float strongGeneticsResistanceBonus;
    public float goldenGeneticsProbabilityBonus;

    public float resistanceBonus; // 식물 일반 특성
    public int additionalPlantGold;

    public float resistanceDecayReduction; // 완두콩 특성
    public float resistanceAdaptation;

    public float additionalPeanutCopyProbability; // 땅콩 특성
    public float bonusRatioWhenDie;

    public bool hasResistanceScouter; // 일반 특성
    public bool hasGoldScouter;
    public bool hasWeatherForecast;

    public float additionalPlantGoldMultiplier;

    public float additionalPestResistance;

    public int additionalInheritance;
    public float maxBreedTimer;
    public int maxBreedCount;
    public int breedCount;
    public List<int> perBottleTiles = new();
    public int petBottleInitialStockBonus = 0;
    public int petBottlePriceReduction = 0;
    public float petBottleSpawnProbability = 0f;
    public int petBottleBlockCountBonus = 0; // 페트병 보호 횟수 보너스 (전체)

    public int chiliPepperRangeLevel = 0;
    public float chiliPepperSpawnProbability = 0f;
    public float chiliPepperHealPercent = 0f;

    public List<int> goldSoilTiles = new();

    public List<int> fertilizerColumns = new();
    public List<WaveType> fertilizerType = new();

    public int mostExpensivePlant;

    // 신규 아이템 스탯
    public int timeIsGoldLevel;
    public int badGuyMoreRiceLevel;
    public int sprinklerRangeBonus;
    public float sprinklerFertilizerSynergyBonus;
    
    // 저항력 흡수 비료 타일 인덱스 리스트
    public List<int> absorbFertilizerTiles = new List<int>();

    //public float remainBreedTime;

    //enemyController
    public Season currentSeason;
    public WaveType lastWaveType;
    public WaveType curWaveType;
    public WaveType nextWaveType;
    public int remainWaveSkipCount;
    public int[] waveKillCount = new int[8];
    public List<WaveType> stageWaveRecord = new();
    public List<int> stageKillRecord = new();
    public List<int> stageNoTraitRecord = new();

    //economyManager
    public int gold;
    public int[] sellCount = new int[2];
    public int totalGold;
    public int consumeGold;

    //shopManager
    public List<string> itemName = new();
    public List<int> itemPurchaseCount = new();
    public List<int> shopSeedDays = new(); // 날짜 리스트
    public List<int> shopSeeds = new(); // 해당 날짜의 시드 리스트 (인덱스 매칭)
    public int gameUniqueShopSeed = -1; // 게임별 고유 시드 (게임마다 다르게 생성, 저장/불러오기 시 유지)

    //ModManager
    public List<Mod> mods = new();

    //RequestManager
    public int cycleEndRound;
    public int dayPassed;
    public List<RequestInstanceSaveData> activeRequests = new();
    public int completeRequestCount;

    //PlayerRecordForGraph
    public List<int> survivedPlants = new();
    public List<int> earnedGolds = new();
    public List<int> waveEachDay = new();

    //GameStartType
    public GameStartType gst = GameStartType.None;


    //PhoneManager
    public List<string> chatPartners = new();
    public List<int> conversationSeenIndices = new();
    public List<string> activatedTriggers = new();

    public List<string> dayChatPartners = new();
    public List<ChatDayData> dayByChatPartners = new();

    //AbilityManager
    public List<PlantAbilityData> currentPlantAbility = new();
    public List<GeneralAbilityData> currentGeneralAbility = new();
    public int geneStorage;

    //CurseManager
    public string[] curseId = new string[2]; //0:temp 1:season
    public int remainSeasonCurseDay;

    //종료 시 저장
    //GameRecordHolder에 저장할 내용
}

[System.Serializable]
public class ConstantSaveData
{
    public int geneToken;
    public int customizeToken;

    //setting data
}

[System.Serializable]
public class PlantData
{
    public string speciesname;
    public List<GeneticTrait> traits = new List<GeneticTrait>();
    public int gridIndex;
    public int taste;
    public int resistWaveCount;
    public int survivedTurns; // MoneyTree 생존 턴 수
}

[System.Serializable]
public class ChatDayData
{
    public List<int> index;
    public List<int> day;
}

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
        var shopUIs = FindObjectsOfType<ShopUI>(true);
        if (shopUIs != null && shopUIs.Length > 0 && shopUIs[0] != null)
        {
            shopUIs[0].DailyReroll();
        }
    }

    IEnumerator StartStage()
    {
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

        GameEvents.RaiseDayPassedForRequest(); //NoSellPeaRequest Check


        // 여기다가 밤으로 바뀌는 이펙트 추가해야 함
        gcController.ToggleGlow(true);
        yield return waveManager.StartCoroutine(waveManager.StartNightCoroutine());

        if (gameMode == 1) curseManager.SelectCurse(stage);

        yield return StartCoroutine(phoneManager.PhonePhase());


        gcController.ToggleGlow(false);
        yield return waveManager.StartCoroutine(waveManager.StopNightCoroutine());
        /*
        if (!enemyController.IsLastWaveNone())
            yield return StartCoroutine(upgradeManager.UpgradePhase());            
        
        yield return StartCoroutine(shopManager.ShopPhase(grid));
        */
        economyManager.PushEarnedGold();
    }

    private void UpdateStageUI()
    {
        textStage.text = $"{stage}";
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

    public IEnumerator GameOver()
    {
        //Debug.Log("게임오버");
        PlayerRecordForGraph.SetSP(grid.plantGrid.Count);
        economyManager.PushEarnedGold();
        yield return new WaitForSeconds(1.0f);
        TransitionController.instance.Transition_Out();
        yield return new WaitForSeconds(1.0f);
        PassRecordToGameRecordHolder();
        SceneLoader.Instance.LoadGameOverScene();
        File.Delete(GetSavePath());
        //Time.timeScale = 0.0f;
        GameStartContext.SetStartType(GameStartType.GameOver);
        Debug.Log("GameOver");
    }
     
    private IEnumerator ClearNormalMode()
    {
        PassRecordToGameRecordHolder();

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

    private void LoadGame()
    {
        string json = File.ReadAllText(GetSavePath());
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        //grid.plantGrid.Clear(); //if needed......

        stage = saveData.stage;
        seenFirstGold = saveData.seenFirstGold;
        currentPlant = saveData.currentPlant;
        grid.LoadGrid(saveData);
        enemyController.LoadEnemyController(saveData);
        economyManager.LoadEconomyManager(saveData);
        shopManager.LoadShopManager(saveData);
        modManager.LoadModManager(saveData);
        requestManager.LoadRequestManager(saveData);
        phoneManager.LoadPhoneManager(saveData);
        curseManager.LoadCurseManager(saveData);
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.LoadCurrentAbilityManager(saveData);
        }
        PlayerRecordForGraph.SetDataFromLoad(saveData);
        Debug.Log("불러옴");
    }

    private void SaveGame()
    {
        var saveData = new SaveData();

        //gameManager
        saveData.stage = stage;
        saveData.seenFirstGold = seenFirstGold;
        saveData.currentPlant = currentPlant;

        //grid
        foreach (var p in grid.plantGrid.Values)
        {
            var plantData = new PlantData
            {
                speciesname = p.speciesname,
                traits = p.GetGeneticTrait(),
                gridIndex = p.gridIndex,
                taste = p.GetTaste(),
                resistWaveCount = p.GetResistWaveCount(),
                survivedTurns = (p is MoneyTree mt) ? mt.GetSurvivedTurns() : 0
            };

            saveData.plantList.Add(plantData);
        }
        saveData.maxCol = grid.maxCol;
        saveData.killBugCount = grid.killBugCount;
        saveData.totalBreedCount = grid.totalBreedCount;
        saveData.totalPeaBreedcount = grid.totalPeaBreedcount;
        saveData.totalPeanutBreedCount = grid.totalPeanutBreedCount;

        saveData.bugSpawnTimeInterval = grid.BugSpawnTimeInterval;
        saveData.lastBugSpawnTimeInterval = grid.LastBugSpawnTimeInterval;

        saveData.bugSpeedDecreasement = grid.BugSpeedDecreasement;
        saveData.bugSpawnIntervalIncreasement = grid.BugSpawnIntervalIncreasement;
        saveData.ladybugSpawnProbability = grid.LadybugSpawnProbability;
        saveData.maxLadybugCount = grid.MaxLadybugCount;
        saveData.additionalLadybugGoldPerUnit = grid.AdditionalLadybugGoldPerUnit;
        saveData.additionalLadybugResistancePerUnit = grid.AdditionalLadybugResistancePerUnit;
        saveData.additionalBugGold = grid.AdditionalBugGold;
        saveData.additionalNepenthesGold = grid.AdditionalNepenthesGold;
        saveData.hasNepenthesPheromone = grid.HasNepenthesPheromone;
        saveData.additionalNepenthesPheromoneSizeMultiplier = grid.AdditionalNepenthesPheromoneSizeMultiplier;
        saveData.nepenthesSpawnProbability = grid.NepenthesSpawnProbability;

        saveData.weakGeneticsResistanceBonus = grid.WeakGeneticsResistanceBonus;
        saveData.strongGeneticsResistanceBonus = grid.StrongGeneticsResistanceBonus;
        saveData.goldenGeneticsProbabilityBonus = grid.GoldenGeneticsProbabilityBonus;

        saveData.resistanceBonus = grid.ResistanceBonus; // 식물 일반 특성
        saveData.additionalPlantGold = grid.AdditionalPlantGold;

        saveData.resistanceDecayReduction = grid.ResistanceDecayReduction;// 완두콩 특성
        saveData.resistanceAdaptation = grid.ResistanceAdaptation;

        saveData.additionalPeanutCopyProbability = grid.AdditionalPeanutCopyProbability; // 땅콩 특성
        saveData.bonusRatioWhenDie = grid.BonusRatioWhenDie;

        saveData.hasResistanceScouter = grid.HasResistanceScouter;
        saveData.hasGoldScouter = grid.HasGoldScouter;
        saveData.hasWeatherForecast = grid.HasWeatherForecast;

        saveData.additionalPlantGoldMultiplier = grid.AdditionalPlantGoldMultiplier;


        saveData.additionalPestResistance = grid.AdditionalPestResistance;

        saveData.additionalInheritance = grid.AdditionalInheritance;
        saveData.maxBreedTimer = grid.MaxBreedTimer;
        saveData.maxBreedCount = grid.MaxBreedCount;

        saveData.perBottleTiles = grid.PetBottleTiles;
        saveData.petBottleInitialStockBonus = grid.PetBottleInitialStockBonus;
        saveData.petBottlePriceReduction = grid.PetBottlePriceReduction;
        saveData.petBottleSpawnProbability = grid.PetBottleSpawnProbability;
        // petBottleBlockCountBonus는 Grid에서 getter로 제공해야 함
        saveData.petBottleBlockCountBonus = grid.GetPetBottleBlockCountBonus();
        saveData.chiliPepperRangeLevel = grid.ChiliPepperRangeLevel;
        saveData.chiliPepperSpawnProbability = grid.ChiliPepperSpawnProbability;
        saveData.chiliPepperHealPercent = grid.ChiliPepperHealPercent;
        saveData.goldSoilTiles = grid.GoldSoilTiles;
        foreach (KeyValuePair<int, WaveType> fer in grid.GetFertilizerColumns())
        {
            saveData.fertilizerColumns.Add(fer.Key);
            saveData.fertilizerType.Add(fer.Value);
        }

        // 신규 아이템 스탯 저장
        saveData.timeIsGoldLevel = grid.GetTimeIsGoldLevel();
        saveData.badGuyMoreRiceLevel = grid.GetBadGuyMoreRiceLevel();
        saveData.sprinklerRangeBonus = grid.GetSprinklerRangeBonus();
        saveData.sprinklerFertilizerSynergyBonus = grid.GetSprinklerFertilizerSynergyBonus();

        saveData.mostExpensivePlant = grid.MostExpensivePlant;

        //enemyController
        saveData.currentSeason = enemyController.CurrentSeason;
        saveData.remainWaveSkipCount = enemyController.WaveSkipCount;
        saveData.waveKillCount = enemyController.WaveKillCount;
        saveData.curWaveType = enemyController.CurrentWave.WaveType;
        saveData.nextWaveType = enemyController.NextWave.WaveType;
        saveData.lastWaveType = enemyController.LastWave.WaveType;
        saveData.stageKillRecord = enemyController.StageKillRecord;
        saveData.stageWaveRecord = enemyController.StageWaveRecord;
        saveData.stageNoTraitRecord = enemyController.StageNoTraitRecord;

        //economyManager
        saveData.gold = economyManager.GetGold();
        saveData.sellCount[0] = economyManager.PeaSellCount;
        saveData.sellCount[1] = economyManager.PeanutSellCount;
        saveData.totalGold = economyManager.TotalGold;
        saveData.consumeGold = economyManager.ConsumeGold;

        //shopManager
        Dictionary<string, int> pHistory = shopManager.PurchaseHistory;
        foreach (KeyValuePair<string, int> p in pHistory)
        {
            saveData.itemName.Add(p.Key);
            saveData.itemPurchaseCount.Add(p.Value);
        }

        // 저항력 흡수 비료 타일 저장
        saveData.absorbFertilizerTiles.Clear();
        saveData.absorbFertilizerTiles.AddRange(grid.GetAbsorbFertilizerTiles());

        // 상점 시드 저장
        saveData.shopSeedDays.Clear();
        saveData.shopSeeds.Clear();
        var shopSeeds = shopManager.GetShopSeeds();
        foreach (var kvp in shopSeeds)
        {
            saveData.shopSeedDays.Add(kvp.Key);
            saveData.shopSeeds.Add(kvp.Value);
        }
        // 게임 고유 시드 저장
        saveData.gameUniqueShopSeed = shopManager.GetGameUniqueSeed();

        //modManager
        saveData.mods = modManager.Mods;

        //RequestManager
        saveData.cycleEndRound = requestManager.CycleEndRound;
        saveData.dayPassed = requestManager.DayPassed;
        saveData.activeRequests = requestManager.getSaveData();
        saveData.completeRequestCount = requestManager.CompleteRequestCount;

        //PlayerRecordForGraph
        saveData.survivedPlants = PlayerRecordForGraph.survivedPlants;
        saveData.earnedGolds = PlayerRecordForGraph.earnedGolds;
        saveData.waveEachDay = PlayerRecordForGraph.waveEachDay;

        //GameStartType
        saveData.gst = GameStartContext.StartType;

        //PhoneManager
        MessengerProgress progress = phoneManager.messengerApp.GetProgress();
        foreach (KeyValuePair<string, int> p in progress.conversationSeenIndices)
        {
            saveData.chatPartners.Add(p.Key);
            saveData.conversationSeenIndices.Add(p.Value);
        }
        foreach (KeyValuePair<string, Dictionary<int, int>> p in progress.daySeparators)
        {
            saveData.dayChatPartners.Add(p.Key);
            ChatDayData tmp = new();
            tmp.index = new();
            tmp.day = new();
            foreach (KeyValuePair<int, int> p2 in p.Value)
            {
                tmp.index.Add(p2.Key);
                tmp.day.Add(p2.Value);
            }
            saveData.dayByChatPartners.Add(tmp);
        }
        foreach (var r in progress.activatedTriggersOrdered)
        {
            saveData.activatedTriggers.Add(r);
        }

        //AbilityManager
        if (AbilityManager.Instance != null)
        {
            saveData.currentPlantAbility = AbilityManager.Instance.CurrentPlantAbility;
            saveData.currentGeneralAbility = AbilityManager.Instance.CurrentGeneralAbility;
            saveData.geneStorage = AbilityManager.Instance.Storage;
        }

        //curseManager
        saveData.curseId = curseManager.SaveCurseManager();
        saveData.remainSeasonCurseDay = curseManager.RemainingCurseDay;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(GetSavePath(), json);

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
