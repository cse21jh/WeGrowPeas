using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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
    public WaveType lastWaveType;
    public WaveType curWaveType;
    public WaveType nextWaveType;
    public int remainWaveSkipCount;
    public int[] waveKillCount = new int[7];

    //economyManager
    public int gold;


    //ModManager
    public List<Mod> mods = new();

    //환경설정 내용
    //GameRecordHolder에 저장될 내용
}

[System.Serializable]
public class PlantData
{
    public string speciesname;
    public List<GeneticTrait> traits = new List<GeneticTrait>();
    public int gridIndex;
    public List<float> additionalResistance = new List<float>();
    public int taste;
}


public class GameManager : Singleton<GameManager>
{
    [HideInInspector] public int stage = 0;
    private bool gameOver = false;

    public Grid grid;
    public EnemyController enemyController;
    public UpgradeManager upgradeManager;
    public ShopManager shopManager;
    public EconomyManager economyManager;
    public ModManager modManager;

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
                grid.InitGrid();
                StageUpdate();
                break;

            case GameStartType.ContinueGame:
                Debug.Log("이어하기");
                LoadGame();
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
            economyManager.PushEarnedGold();
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

        yield return StartCoroutine(grid.Breeding());

        yield return StartCoroutine(enemyController.EnemyWaveCoroutine());

        gameOver = grid.CheckGameOver();

        if (gameOver)
            yield return StartCoroutine(GameOver());
        else if (!enemyController.IsLastWaveNone())
        {
            yield return new WaitForSeconds(2.0f);
            PlayerRecordForGraph.SetSP(grid.plantGrid.Count);

            if (stage == endStage) yield return StartCoroutine(ClearNormalMode());

            yield return StartCoroutine(BreedEndRoutine());
            yield return StartCoroutine(upgradeManager.UpgradePhase());
        }        
        yield return StartCoroutine(shopManager.ShopPhase(grid));

    }

    private void UpdateStageUI()
    {
        textStage.text = $"스테이지 {stage}";
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
        yield return new WaitForSeconds(2.0f);
        PassRecordToGameRecordHolder();
        SceneLoader.Instance.LoadGameOverScene();
        File.Delete(GetSavePath());
        //Time.timeScale = 0.0f;
        GameStartContext.SetStartType(GameStartType.GameOver);
        Debug.Log("GameOver");
    }

    private IEnumerator ClearNormalMode()
    {
        yield return new WaitForSeconds(2.0f);
        PassRecordToGameRecordHolder();

        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Ending);
        //File.Delete(GetSavePath());
        //Time.timeScale = 0.0f;
        SaveGame();
        Debug.Log("40일 동안 생존하였습니다. YEAH!");
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
        modManager.LoadModManager(saveData);
        Debug.Log("불러옴");
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
                additionalResistance = p.GetAdditionalResistances(),
                taste = p.GetTaste()
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
        saveData.remainWaveSkipCount = enemyController.WaveSkipCount;
        saveData.waveKillCount = enemyController.WaveKillCount;
        saveData.curWaveType = enemyController.CurrentWave.WaveType;
        saveData.nextWaveType = enemyController.NextWave.WaveType;
        saveData.lastWaveType = enemyController.LastWave.WaveType;

        //economyManager
        saveData.gold = economyManager.GetGold();

        //modManager
        saveData.mods = modManager.Mods;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(GetSavePath(), json);

        GameStartContext.SetStartType(GameStartType.ContinueGame);

        Debug.Log("저장됨");
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
