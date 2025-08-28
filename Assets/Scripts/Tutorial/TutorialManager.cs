using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialManager : Singleton<TutorialManager>
{
    [HideInInspector] public int tStage = 1;

    [SerializeField] private TutorialGrid grid;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private ShopManager shopManager;

    [SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private Narration n;

    [Header("Sequences")]
    [SerializeField] private DialogueStep[] step0;
    [SerializeField] private DialogueStep[] step1;
    [SerializeField] private DialogueStep[] step2;
    [SerializeField] private DialogueStep[] step3;
    [SerializeField] private DialogueStep[] step4;
    [SerializeField] private DialogueStep[] step5;

    [Header("Popup")]
    [SerializeField] private GameObject skipPopup;
    [SerializeField] private GameObject tutorialEndPopup;

    [Header("White Circle Area")]
    [SerializeField] private SpawnedCircle spawnedCircle;

    private bool _narrationClickedThisFrame = false;
    private GameObject _lastClickedObject = null;

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

        grid.InitTGrid();
        UpdateStageUI();
        enemyController.UnlockWave(tStage);
        enemyController.ShowNextWaveText();

        yield return PlayTutorialSequence(step0);
    }

    private IEnumerator PlayTutorialSequence(DialogueStep[] seq)
    {
        if (seq == null || seq.Length == 0) yield break;

        n.Flush();

        for (int i = 0; i < seq.Length; i++)
        {
            var s = seq[i];
            if (s == null) continue;

            // 대사 출력
            n.AddLine(s.text);

            // 트리거 대기
            yield return WaitForTrigger(s);

            // 튜토리얼 내의 특정 액션 발생
            if (s.actions[0] != TutorialActions.None)
            {
                DoAction(s);
            }

            // 다음 step으로 넘어가는 상황
            if (s.chainTo != NextTutorialSequence.None)
            {
                var next = ResolveSequence(s.chainTo);
                if (next != null && next.Length > 0)
                    yield return PlayTutorialSequence(next);
                yield break;
            }
        }
    }

    private IEnumerator WaitForTrigger(DialogueStep s)
    {
        // 한 스텝 시작할 때마다 신호 초기화
        _narrationClickedThisFrame = false;
        _lastClickedObject = null;

        switch (s.triggerType)
        {
            case DialogueTriggerType.NarrationClick:
                // NarrationBox 클릭 신호를 기다림
                yield return new WaitUntil(() => _narrationClickedThisFrame);
                _narrationClickedThisFrame = false;
                break;

            case DialogueTriggerType.KeyPress:
                //yield return new WaitUntil(() => Input.GetKeyDown(s.triggerKey));
                break;

            case DialogueTriggerType.ObjectClick:
                // 지정된 오브젝트를 클릭할 때까지 대기
                yield return new WaitUntil(() =>
                    _lastClickedObject != null && 
                    _lastClickedObject.GetComponent<WhiteCircle>() != null
                );
                _lastClickedObject = null;
                break;
        }
    }

    private void DoAction(DialogueStep s)
    {
        for (int i = 0; i < s.actions.Length; i++)
        {
            switch (s.actions[i])
            {
                case TutorialActions.ShowSkipPopUp:
                    skipPopup.SetActive(true);
                    break;

                case TutorialActions.ShowWhiteCircle:
                    spawnedCircle.ShowCircle(s.whiteCirclePos);
                    break;

                case TutorialActions.FlushCircle:
                    spawnedCircle.FlushSpawnedCircleCanvas();
                    break;

                case TutorialActions.SpawnBug:
                    grid.SpawnTutorialBug();
                    break;

                case TutorialActions.Breed:
                    grid.StartTutorialBreeding();
                    break;

                case TutorialActions.EnemyWave:
                    ActivateWave();
                    break;

                case TutorialActions.Upgrade:
                    upgradeManager.TutorialUpgrade();
                    break;

                case TutorialActions.InitShop:
                    break;

                case TutorialActions.ShowTutorialEndPopUp:
                    tutorialEndPopup.SetActive(true);
                    break;

                case TutorialActions.ClosePanel:
                    ClosePanel();
                    break;

            }
        }
    }

    /// <summary>NarrationBox의 EventTrigger/Button에서 OnClick에 연결</summary>
    public void OnNarrationBoxClicked()
    {
        _narrationClickedThisFrame = true;
    }

    /// <summary>특정 오브젝트(벌레/식물)의 EventTrigger/Button에서 OnClick에 연결</summary>
    public void OnObjectClicked(GameObject clicked)
    {
        _lastClickedObject = clicked;
    }

    private DialogueStep[] ResolveSequence(NextTutorialSequence id)
    {
        return id switch
        {
            NextTutorialSequence.Step0 => step0,
            NextTutorialSequence.Step1 => step1,
            NextTutorialSequence.Step2 => step2,
            NextTutorialSequence.Step3 => step3,
            NextTutorialSequence.Step4 => step4,
            NextTutorialSequence.Step5 => step5,
            _ => null
        };
    }

    public void ContinueTutorial()
    {
        StartCoroutine(PlayTutorialSequence(step3));
        skipPopup.SetActive(false);
    }


    private void UpdateStageUI()
    {
        textStage.text = $"<sprite=0> STAGE {tStage}";
    }

    private void ActivateWave()
    {
        enemyController.TutorialWave();
        tStage = 2;
        UpdateStageUI();
        enemyController.ShowNextWaveText();
    }

    private void ClosePanel()
    {
        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Normal);
    }
}
