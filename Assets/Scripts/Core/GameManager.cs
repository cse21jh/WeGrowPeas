using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        switch(GameStartContext.StartType)
        {
            case GameStartType.NewGame:
                Debug.Log("새 게임");
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
        GameRecordHolder.SaveData(stage, grid.totalBreedCount, grid.killBugCount);
        SceneLoader.Instance.LoadGameOverScene();
        //Time.timeScale = 0.0f;
        GameStartContext.SetStartType(GameStartType.NewGame);
        Debug.Log("GameOver");
    }

    private void LoadGame()
    {
        StageUpdate();
    }

    private void SaveGame()
    {

    }
}
