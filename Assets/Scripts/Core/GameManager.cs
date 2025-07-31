using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class SaveData
{
    public List<PlantData> plantList = new();
    public int remainBreedCount;
    //public float remainBreedTime;
    public int remainWaveSkipCount;
    public int remainUpgradeRerollCount;
    public WaveType curWaveType;
    //환경설정 내용
    //GameRecordHolder에 저장될 내용
}

[System.Serializable]
public class PlantData
{
    public string speciesname;
    public List<GeneticTrait> traits = new List<GeneticTrait>();
    public int gridIndex;
}


public class GameManager : Singleton<GameManager>
{
    [HideInInspector] public int stage = 0;
    private bool gameOver = false;

    public Grid grid;
    public EnemyController enemyController;
    public UpgradeManager upgradeManager;

    [SerializeField] private TextMeshProUGUI textStage;

    // Start is called before the first frame update
    void Start()
    {
        SoundManager.Instance.StopBgm();
        SoundManager.Instance.PlayBgm("Farm");

        Time.timeScale = 1;

        ClickRouter.Instance.IsBlockedByUI = false;

        switch(GameStartContext.StartType)
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
        while(!gameOver)
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
        enemyController.UnlockWave(stage);
        upgradeManager.UnlockUpgrade(stage);   
    }

    IEnumerator StartStage()
    {
        enemyController.ShowNextWaveText();

        yield return StartCoroutine(grid.Breeding());

        enemyController.EnemyWave();

        gameOver = grid.CheckGameOver();

        if(gameOver)
            yield return StartCoroutine(GameOver());
        else if (!enemyController.IsLastWaveNone())
        {
            yield return new WaitForSeconds(2.0f);
            yield return StartCoroutine(upgradeManager.UpgradePhase());
        }

    }

    private void UpdateStageUI()
    {
        textStage.text = $"<sprite=0> STAGE {stage}";
    }

    public IEnumerator GameOver()
    {
        yield return new WaitForSeconds(2.0f);
        GameRecordHolder.SaveRecord(stage, grid.totalBreedCount, grid.killBugCount);
        SceneLoader.Instance.LoadGameOverScene();
        //Time.timeScale = 0.0f;
        GameStartContext.SetStartType(GameStartType.GameOver);
        Debug.Log("GameOver");
    }

    private void LoadGame()
    {
        StageUpdate();
        //LoadGrid;

        string json = File.ReadAllText(GetSavePath());
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        //grid.plantGrid.Clear(); //if needed......

        grid.LoadGrid(saveData.plantList);

        Debug.Log("불러옴");
    }

    private void SaveGame()
    {
        var saveData = new SaveData();

        foreach (var p in grid.plantGrid.Values)
        {
            var plantData = new PlantData
            {
                speciesname = p.speciesname,
                traits = p.GetGeneticTrait(),
                gridIndex = p.gridIndex
            };
            
            saveData.plantList.Add(plantData);
        }

        saveData.remainBreedCount = grid.BreedCount;
        //saveData.remainBreedTime = 30.0f;
        saveData.remainWaveSkipCount = enemyController.WaveSkipCount;
        saveData.remainUpgradeRerollCount = upgradeManager.MaxRerollCount;
        saveData.curWaveType = enemyController.CurrentWave.WaveType;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(GetSavePath(), json);

        GameStartContext.SetStartType(GameStartType.ContinueGame); 

        Debug.Log("저장됨");
    }

    private string GetSavePath()
    {
        return Application.dataPath + "/UserData.json";
    }
}
