using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

[System.Serializable]
public class SaveData
{
    //gameManager
    public int stage;

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
    public int additionalBugGold;

    public float additionalPeanutCopyProbability;
    public int additionalPeanutGold;
    public int additionalPeaGold;
    public float additionalPeaGoldMultiplier;

    public float additionalPestResistance;

    public int additionalInheritance;
    public float maxBreedTimer;
    public int maxBreedCount;
    public int breedCount;
    public bool hasIceBlock;
    public List<int> perBottleTiles = new();

    public List<int> fertilizerColumns = new();
    public List<WaveType> fertilizerType = new();
    //public float remainBreedTime;


    //upgradeManager
    public int remainUpgradeRerollCount;
    public List<int> remainUpgradeId = new();
    public List<int> remainUpgradeCount = new();

    //enemyController
    public Season currentSeason;
    public WaveType lastWaveType;
    public WaveType curWaveType;
    public WaveType nextWaveType;
    public int remainWaveSkipCount;
    public int[] waveKillCount = new int[7];
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

    //ModManager
    public List<Mod> mods = new();

    //RequestManager
    public int cycleEndRound;
    public List<RequestInstanceSaveData> activeRequests = new();

    //PlayerRecordForGraph
    public List<int> survivedPlants = new();
    public List<int> earnedGolds = new();
    public List<int> waveEachDay = new();

    //GameStartType
    public GameStartType gst = GameStartType.None;

    //ȯ�漳�� ����
    //GameRecordHolder�� ����� ����
}

[System.Serializable]
public class PlantData
{
    public string speciesname;
    public List<GeneticTrait> traits = new List<GeneticTrait>();
    public int gridIndex;
    public int taste;
    public int resistWaveCount;
}


public class GameManager : Singleton<GameManager>
{
    [HideInInspector] public int stage = 0;
    private bool gameOver = false;
    public int requestCycle = 2;

    public Grid grid;
    public EnemyController enemyController;
    public UpgradeManager upgradeManager;
    public ShopManager shopManager;
    public EconomyManager economyManager;
    public ModManager modManager;
    public RequestManager requestManager;
    public PhoneManager phoneManager;

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
                Debug.Log("�� ����");
                grid.InitGrid();
                economyManager.InitEconomyManager();
                PlayerRecordForGraph.ClearAll();
                StageUpdate();
                break;

            case GameStartType.ContinueGame:
                Debug.Log("�̾��ϱ�");
                LoadGame();
                break;

            case GameStartType.ContinueAfterEnding:
                Debug.Log("40�� ���� ���� �̾��ϱ�");
                LoadGame();
                stage++;
                break;
        }

        StartCoroutine(GameStart());
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

    IEnumerator GameStart()
    {
        while (!gameOver)
        {
            UpdateStageUI();
            yield return StartCoroutine(StartStage());
            StageUpdate();
            SaveGame();
        }
    }

    private void StageUpdate()
    {
        stage++;
        ModManager.Instance?.OnNewDay(stage);
        enemyController.UnlockWave(stage);
        upgradeManager.UnlockUpgrade(stage);
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

        if (stage == endStage)
        {
            economyManager.PushEarnedGold();
            yield return StartCoroutine(ClearNormalMode());
        }

        yield return StartCoroutine(BreedEndRoutine());

        yield return StartCoroutine(phoneManager.PhonePhase());
        /*
        if (!enemyController.IsLastWaveNone())
            yield return StartCoroutine(upgradeManager.UpgradePhase());            
        
        yield return StartCoroutine(shopManager.ShopPhase(grid));
        */
        economyManager.PushEarnedGold();
    }

    private void UpdateStageUI()
    {
        textStage.text = $"<sprite=0> Day {stage}";
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
        yield return null;
    }

    public IEnumerator GameOver()
    {
        //Debug.Log("���ӿ���");
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
        GameStartContext.SetStartType(GameStartType.ContinueAfterEnding);
        SaveGame();
        Debug.Log("40�� ���� �����Ͽ����ϴ�. YEAH!");
        while(true)
            yield return null;
    }

    private void LoadGame()
    {
        string json = File.ReadAllText(GetSavePath());
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        //grid.plantGrid.Clear(); //if needed......

        stage = saveData.stage;
        grid.LoadGrid(saveData);
        upgradeManager.LoadUpgradeManager(saveData);
        enemyController.LoadEnemyController(saveData);
        economyManager.LoadEconomyManager(saveData);
        shopManager.LoadShopManager(saveData);
        modManager.LoadModManager(saveData);
        requestManager.LoadRequestManager(saveData);
        PlayerRecordForGraph.SetDataFromLoad(saveData);
        Debug.Log("�ҷ���");
    }

    private void SaveGame()
    {
        var saveData = new SaveData();

        //gameManager
        saveData.stage = stage;


        //grid
        foreach (var p in grid.plantGrid.Values)
        {
            var plantData = new PlantData
            {
                speciesname = p.speciesname,
                traits = p.GetGeneticTrait(),
                gridIndex = p.gridIndex,
                taste = p.GetTaste(),
                resistWaveCount = p.GetResistWaveCount()
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
        saveData.additionalBugGold = grid.AdditionalBugGold;

        saveData.additionalPeanutGold = grid.AdditionalPeanutGold;
        saveData.additionalPeaGold = grid.AdditionalPeaGold;
        saveData.additionalPeaGoldMultiplier = grid.AdditionalPeaGoldMultiplier;
        saveData.additionalPeanutCopyProbability = grid.AdditionalPeanutCopyProbability;

        saveData.additionalPestResistance = grid.AdditionalPestResistance;

        saveData.additionalInheritance = grid.AdditionalInheritance;
        saveData.maxBreedTimer = grid.MaxBreedTimer;
        saveData.maxBreedCount = grid.MaxBreedCount;

        saveData.hasIceBlock = grid.HasIceBlock;
        saveData.perBottleTiles = grid.PetBottleTiles;
        foreach(KeyValuePair<int,WaveType> fer in grid.GetFertilizerColumns())
        {
            saveData.fertilizerColumns.Add(fer.Key);
            saveData.fertilizerType.Add(fer.Value);
        }

        //upgradeManager
        saveData.remainUpgradeRerollCount = upgradeManager.MaxRerollCount;
        Dictionary<Type, int> remainUpgrade = upgradeManager.GetRemainUpgrade();
        Dictionary<Type, Func<Upgrade>> upgradeInstance = upgradeManager.GetUpgradeInstance();
        foreach (KeyValuePair<Type, int> u in remainUpgrade)
        {
            saveData.remainUpgradeId.Add(upgradeInstance[u.Key]().UpgradeId);
            saveData.remainUpgradeCount.Add(u.Value);
        }
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
        foreach(KeyValuePair<string, int> p in pHistory)
        {
            saveData.itemName.Add(p.Key);
            saveData.itemPurchaseCount.Add(p.Value);
        }

        //modManager
        saveData.mods = modManager.Mods;

        //RequestManager
        saveData.cycleEndRound = requestManager.CycleEndRound;
        saveData.activeRequests = requestManager.getSaveData();

        //PlayerRecordForGraph
        saveData.survivedPlants = PlayerRecordForGraph.survivedPlants;
        saveData.earnedGolds = PlayerRecordForGraph.earnedGolds;
        saveData.waveEachDay = PlayerRecordForGraph.waveEachDay;

        //GameStartType
        saveData.gst = GameStartContext.StartType;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(GetSavePath(), json);

        GameStartContext.SetStartType(GameStartType.ContinueGame);

        Debug.Log("�����");
    }

    private string GetSavePath()
    {
        return Application.dataPath + "/UserData.json";
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
            itemName);
    }
}
