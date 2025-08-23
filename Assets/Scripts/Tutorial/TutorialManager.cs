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

    [Header("Sequences")]
    [SerializeField] private DialogueStep[] step0;
    [SerializeField] private DialogueStep[] step1;

    [Header("Skip Popup")]
    [SerializeField] private GameObject skipPopup;

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
        //yield return TutorialStep1();
        //yield return TutorialStep2();
        //yield return TutorialStep3();
        //yield return TutorialStep4();
    }

    private IEnumerator PlayTutorialSequence(DialogueStep[] seq)
    {
        if (seq == null || seq.Length == 0) yield break;

        for (int i = 0; i < seq.Length; i++)
        {
            var s = seq[i];
            if (s == null) continue;

            // 1) 대사 출력
            n.AddLine(s.text);

            // 2) 트리거 대기
            yield return WaitForTrigger(s);

            if (s.showSkipPopupOnComplete) skipPopup.SetActive(true);
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
                break;

            case DialogueTriggerType.KeyPress:
                //yield return new WaitUntil(() => Input.GetKeyDown(s.triggerKey));
                break;

            case DialogueTriggerType.ObjectClick:
                // 지정된 오브젝트를 클릭할 때까지 대기
                yield return new WaitUntil(() =>
                    _lastClickedObject != null && s.targetObject != null &&
                    _lastClickedObject == s.targetObject
                );
                break;
        }
    }

    /// <summary>NarrationBox의 EventTrigger/Button에서 OnClick에 연결</summary>
    public void OnNarrationBoxClicked()
    {
        _narrationClickedThisFrame = true;
        // 같은 프레임에서 여러 신호가 섞이지 않게 다음 프레임에 자동 리셋(선택)
        StartCoroutine(ResetNarrationClickNextFrame());
    }
    private IEnumerator ResetNarrationClickNextFrame()
    {
        yield return null;
        _narrationClickedThisFrame = false;
    }

    /// <summary>특정 오브젝트(벌레/식물)의 EventTrigger/Button에서 OnClick에 연결</summary>
    public void OnObjectClicked(GameObject clicked)
    {
        _lastClickedObject = clicked;
        // 필요시 다음 프레임에 리셋
        StartCoroutine(ResetObjectClickNextFrame());
    }
    private IEnumerator ResetObjectClickNextFrame()
    {
        yield return null;
        _lastClickedObject = null;
    }

    private IEnumerator TutorialStep0()
    {
        Debug.Log("튜토리얼 0 실행");
        grid.InitTGrid();
        UpdateStageUI();
        enemyController.UnlockWave(tStage);
        enemyController.ShowNextWaveText();
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
