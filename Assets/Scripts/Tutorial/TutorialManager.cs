using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [HideInInspector] public int tStage = 1;

    [SerializeField] private TutorialGrid grid;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private UpgradeManager upgradeManager;
    //[SerializeField] private ShopManager shopManager;

    [SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private Narration n;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SoundManager.Instance.StopBgm();
        SoundManager.Instance.PlayBgm("Farm");

        Time.timeScale = 1;

        //ClickRouter.Instance.IsBlockedByUI = false;

        StartCoroutine(RunTutorial());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator RunTutorial()
    {
        yield return null;

        yield return TutorialStep0();
        yield return TutorialStep1();
        yield return TutorialStep2();
        yield return TutorialStep3();
        yield return TutorialStep4();
    }

    private IEnumerator TutorialStep0()
    {
        Debug.Log("튜토리얼 0 실행");
        grid.InitTGrid();
        UpdateStageUI();
        enemyController.UnlockWave(tStage);
        enemyController.ShowNextWaveText();
        n.AddLine(n.demoLines[n._nextIdx++]);
        //Debug.Log("AddLine이 됐어야 했는데");

        yield return null;
    }

    private IEnumerator TutorialStep1()
    {
        yield return null;
    }

    private IEnumerator TutorialStep2()
    {
        yield return null;
    }

    private IEnumerator TutorialStep3()
    {
        yield return null;
    }

    private IEnumerator TutorialStep4()
    {
        yield return null;
    }

    private void UpdateStageUI()
    {
        textStage.text = $"<sprite=0> STAGE {tStage}";
    }
}
