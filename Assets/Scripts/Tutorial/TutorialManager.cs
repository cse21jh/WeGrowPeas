using DG.Tweening.Core.Easing;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class TutorialManager : Singleton<TutorialManager>
{
    [HideInInspector] public int tStage = 1;

    [Header("Current Progress")]
    [SerializeField] private int currentStep = 0; // 현재 튜토리얼 단계 (0부터 시작)
    [SerializeField] private int maxStep = 7;    // 전체 튜토리얼 단계 수


    [Header("Components")]
    [SerializeField] private TutorialGrid grid;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private GameObject shopUIPanel;
    [SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private GameObject shovel;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GlowCanvasController gcController;



    [Header("Visual Guides")]
    [SerializeField] private GameObject breedGraph;
    [SerializeField] private SpawnedCircle spawnedCircle;
    [SerializeField] private GameObject rc;
    [SerializeField] private MessageController mc;


    private bool _narrationClickedThisFrame = false;
    private bool _breedSuccess = false;
    private bool _catchBug = false;
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

        yield return PlayTutorialSequence();
    }

    private IEnumerator PlayTutorialSequence()
    {
        while (currentStep <= maxStep)
        {
            // 1. 해당 단계의 메신저 트리거 발송 (0, 1, 2...)
            PhoneManager.Instance.messengerApp.ActivateTrigger(currentStep.ToString());
            

            yield return new WaitUntil(() => PhoneManager.Instance.messengerApp.IsTriggerFullySeen(currentStep.ToString()));

            // 3. 메시지 종료 후, 해당 단계에서 해야 할 "가이드/액션" 실행 (스위치문)
            ExecuteStepAction(currentStep);

            // 4. 플레이어가 목표 행동을 완료할 때까지 대기 (스위치문)
            yield return WaitForTrigger(currentStep);

            ExecuteAfterStepAction(currentStep);
           
            currentStep++;
        }

        Debug.Log("모든 튜토리얼이 완료되었습니다.");

    }

    private void ExecuteStepAction(int index)
    {
        switch (index)
        {
            case 0: // 완두콩에 마우스 가져다 두도록
                spawnedCircle.ShowCircle(new Vector3(-4.65f, 2.335f, 0f), new Vector2(75f, 75f));
                break;
            case 1: // 완두콩 클릭
                spawnedCircle.ShowCircle(new Vector3(-4.65f, 1.835f,0f), new Vector2(75f, 150f));
                grid.StartTutorialBreeding();
                grid.MakeMovable();
                break;
            case 2: // 교배                
                spawnedCircle.ShowCircle(new Vector3(-4.65f, 1.835f, 0f), new Vector2(75f, 150f));
                break;
            case 3: // 웨이브 지나감
                ActivateWave();
                break;
            case 4: // 자유시간
                gcController.ToggleGlow(true);
                waveManager.StartCoroutine(waveManager.StartNightCoroutine());
                PhoneManager.Instance.TutorialPhonePhase();
                break;
            case 5: // 삽 클릭
                shovel.SetActive(true);
                spawnedCircle.ShowCircle(new Vector3(-8f, -3.9f, 0f), new Vector2(70f, 95f));
                break;
            case 6: // 특정 완두콩 판매
                spawnedCircle.ShowCircle(new Vector3(-4.65f, 1.335f, 0f), new Vector2(75f, 75f));
                break;            
            case 7: // 이제 실전으로 끝
                // 
                break;
        }
    }
    private IEnumerator WaitForTrigger(int currentStep)
    {
        // 한 스텝 시작할 때마다 신호 초기화
        _narrationClickedThisFrame = false;
        _lastClickedObject = null;

        switch (currentStep)
        {
            case 0: // 완두콩에 마우스 가져다 두도록
                yield return new WaitUntil(() =>
                        FenceUIManager.Instance.CheckFenceIsShowingMe(0) == true
                    );
            break;

            case 1: // 완두콩 클릭
                yield return new WaitUntil(() =>
                    _lastClickedObject != null &&
                    _lastClickedObject.GetComponent<WhiteCircle>() != null
                );
                _lastClickedObject = null;
                break;
            case 2: // 교배
                yield return new WaitUntil(() => _breedSuccess);
                _breedSuccess = false;
                break;
            case 3: // 웨이브 지나감
                yield return new WaitUntil(() =>
                        tStage == 2
                );
                break;
            case 4: // 자유시간
                yield return new WaitUntil(() =>
                        PhoneManager.Instance.GetIsPhoneTime() == true
                );
                break;
            case 5: // 삽 클릭
                yield return new WaitUntil(() =>
                    grid.isDraggingShovel == true
                );
                _lastClickedObject = null;
                break;

            case 6: // 특정 완두콩 판매
                yield return new WaitUntil(() =>
                    GameObject.Find("EconomyManager").GetComponent<EconomyManager>().PeaSellCount >= 1
                );
                _lastClickedObject = null;
                break;
            case 7: // 이제 실전으로 끝
                //yield return new WaitForSeconds(1f);
                PhoneManager.Instance.isTutorialEnd = true;
                gcController.ToggleGlow(false);
                waveManager.StartCoroutine(waveManager.StopNightCoroutine());
                mc.AddMessage(MessageController.MessageSenderType.player, "저장소 0에 키우러 갈래!", "", () => FindAnyObjectByType<UIClickEvent>().OnClick_StartNewGame(0));
                mc.AddMessage(MessageController.MessageSenderType.player, "저장소 1에 키우러 갈래!", "", () => FindAnyObjectByType<UIClickEvent>().OnClick_StartNewGame(1));
                mc.AddMessage(MessageController.MessageSenderType.player, "저장소 2에 키우러 갈래!", "", () => FindAnyObjectByType<UIClickEvent>().OnClick_StartNewGame(2));
                break;
        }
    }

    private void ExecuteAfterStepAction(int index)
    {
        switch (index)
        {
            case 0: // 완두콩에 마우스 가져다 두도록
                spawnedCircle.FlushSpawnedCircleCanvas();
                grid.MakeMovable();
                break;
            case 1: // 완두콩 클릭
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;
            case 2: // 교배
                Instantiate(breedGraph, canvasTransform);
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;            
            case 3: // 웨이브 지나감
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;
            case 4: // 자유시간
                PhoneManager.Instance.TutorialPhonePhase();
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;
            case 5: // 삽 클릭
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;
            case 6: // 특정 완두콩 판매
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;
            case 7: // 이제 실전으로 끝
                spawnedCircle.FlushSpawnedCircleCanvas();
                break;
        }
    }


    /// <summary>NarrationBox의 EventTrigger/Button에서 OnClick에 연결</summary>
    public void OnNarrationBoxClicked()
    {
        _narrationClickedThisFrame = true;
    }

    /// <summary>
    /// MessageBox의 EventTrigger/Button에서 OnClick에 연결
    /// </summary>
    public void OnMessageBoxClicked()
    {
        _narrationClickedThisFrame = true;
    }

    /// <summary>특정 오브젝트(벌레/식물)의 EventTrigger/Button에서 OnClick에 연결</summary>
    public void OnObjectClicked(GameObject clicked)
    {
        _lastClickedObject = clicked;
    }

    public void OnBreedSucess()
    {
        _breedSuccess = true;
    }

    public void OnCatchBug()
    {
        _catchBug = true;
    }


    /*
    public void CloseBreedGraph()
    {
        breedGraph.SetActive(false);
    }
    */


    private void UpdateStageUI()
    {
        textStage.text = $"{tStage}";
    }

    private void ActivateWave()
    {
        enemyController.TutorialWave();
        tStage = 2;
        UpdateStageUI();
        enemyController.ShowNextWaveText();
    }

    private void OpenShop()
    {
        shopUIPanel.SetActive(true);
        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Shop);

    }

    private void FenceToggleOn(int index)
    {
        if (grid.plantGrid.ContainsKey(index))
            FenceUIManager.Instance.ToggleOn(0, grid.plantGrid[index]);
        else
            FenceUIManager.Instance.ToggleOn(0, grid.plantGrid.First().Value);

    }
}
