using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public int stage = 0;
    private bool gameOver = false;

    public Grid grid;
    public EnemyController enemyController;
    public UpgradeManager upgradeManager;


    // Start is called before the first frame update
    void Start()
    {
        stage = 1;
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
            yield return StartCoroutine(StartStage());
            stage++;
        }
        Debug.Log("Game Over");
    }

    IEnumerator StartStage()
    {
        yield return StartCoroutine(grid.Breeding());

        enemyController.EnemyWave();

        gameOver = grid.CheckGameOver();
        
        if(!gameOver)
            yield return StartCoroutine(upgradeManager.UpgradePhase());
    }
}
