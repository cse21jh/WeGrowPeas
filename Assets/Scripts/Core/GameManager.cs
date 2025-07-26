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
    //ȯ�漳�� ����
    //GameRecordHolder�� ����� ����
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
                Debug.Log("�� ����");
                grid.InitGrid();
                StageUpdate();
                break;

            case GameStartType.ContinueGame:
                Debug.Log("�̾��ϱ�");
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

        yield return new WaitForSeconds(2.0f);

        gameOver = grid.CheckGameOver();

        if(gameOver)
            GameOver();
        else if (!enemyController.IsLastWaveNone())
            yield return StartCoroutine(upgradeManager.UpgradePhase());
    
    }

    private void UpdateStageUI()
    {
        textStage.text = $"<sprite=0> STAGE {stage}";
    }

    public void GameOver()
    {
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

        Debug.Log("�ҷ���");
    }

    private void SaveGame()
    {
        var saveData = new SaveData();

        foreach (var pair in grid.plantGrid)
        {
            var plantData = new PlantData
            {
                speciesname = pair.Value.speciesname,
                traits = pair.Value.GetGeneticTrait(),
                gridIndex = pair.Value.gridIndex
          
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

        Debug.Log("�����");
    }

    private string GetSavePath()
    {
        return Application.dataPath + "/UserData.json";
    }
}
