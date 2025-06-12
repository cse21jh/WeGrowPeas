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
        StageUpdate();
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
        }
        Debug.Log("Game Over");
    }

    private void StageUpdate()
    {
        stage++;
        upgradeManager.UnlockUpgrade(stage);
        enemyController.UnlockWave(stage);
    }

    IEnumerator StartStage()
    {
        enemyController.ShowNextWaveText();

        yield return StartCoroutine(grid.Breeding());

        enemyController.EnemyWave();

        gameOver = grid.CheckGameOver();

        yield return new WaitForSeconds(2.0f);
        
        if(!gameOver)
            yield return StartCoroutine(upgradeManager.UpgradePhase());
    }

    private void UpdateStageUI()
    {
        textStage.text = $"<sprite=0> STAGE {stage}";
    }
}
