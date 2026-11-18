using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

[ExecuteInEditMode]
public class TransitionController : MonoBehaviour
{
    public static TransitionController instance;

    [SerializeField] private GameObject blocker;

    public Material transitionMat;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private Ease easeType;

    [SerializeField] private bool isFinished = false;

    [Space(10)]
    [Header("Debug")]
    [SerializeField] private bool isDebug = false;
    [SerializeField] private float debugRadius = 0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; 
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);

                transitionMat.DOKill(true);
                transitionMat.SetFloat(ShaderProps.Radius, 1.5f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 에디터의 빠른 Play(씬 다시 불러오기 끔)에서는 이 스크립트가 [ExecuteInEditMode]라
    /// 편집 중에 이미 Awake가 끝나 Play 시작 때 다시 불리지 않는다. 그러면 DontDestroyOnLoad가 빠져
    /// 이 캔버스(전환 효과·커서·도감)가 첫 씬 전환 때 함께 사라진다.
    /// Play가 시작될 때마다 한 번 확인해 아직 씬에 묶여 있으면 상주시킨다. 빌드와 일반 Play에서는 Awake가 이미 처리했으므로 하는 일이 없다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePersistentOnPlay()
    {
        if (instance != null && instance.gameObject.scene.name == "DontDestroyOnLoad") return;

        var found = FindAnyObjectByType<TransitionController>(FindObjectsInactive.Include);
        if (found == null) return;

        instance = found;
        DontDestroyOnLoad(found.gameObject);
        if (found.transitionMat != null)
        {
            found.transitionMat.DOKill(true);
            found.transitionMat.SetFloat(ShaderProps.Radius, 1.5f);
        }
    }

    private void OnEnable()
    {
        transitionMat.DOKill(true);
        transitionMat.SetFloat(ShaderProps.Radius, 1.5f);
        isFinished = true;
    }

    private void Start()
    {
        /*
        if (SceneManager.GetActiveScene().name == "StartScene 1")
        {
            transitionMat.SetFloat(ShaderProps.Radius, 1.5f);
            isFinished = true;
        }
        */
    }

    private void Update()
    {
        if (isDebug)
        {
            Debug.Log(transitionMat.GetFloat(ShaderProps.Radius));
            //transitionMat.DOKill(true);
            //transitionMat.SetFloat(ShaderProps.Radius, debugRadius);
        }
    }


    /// <summary>
    /// 점점 밝아지는 트랜지션.
    /// 인트로 성격
    /// </summary>
    public void Transition_In()
    {
        if (!isFinished) return;

        blocker.SetActive(true);
        transitionMat.DOKill(true);
        Debug.Log("Transition In");
        transitionMat.SetFloat(ShaderProps.Radius, 0f);
        isFinished = false;
        Transition(1.5f);
    }

    /// <summary>
    /// 점점 어두워지는 트랜지션.
    /// 아웃트로 성격
    /// </summary>
    public async Task Transition_Out()
    {
        await Task.Yield();

        blocker.SetActive(true);
        transitionMat.DOKill(true);
        transitionMat.SetFloat(ShaderProps.Radius, 1.5f);
        Debug.Log(transitionMat.GetFloat(ShaderProps.Radius));
        isFinished = false;
        Transition(0f);

        while(!isFinished)
        {
            await Task.Yield();
        }
    }

    public bool IsFinished()
    {
        return isFinished;
    }

    private void Transition(float rad)
    {
        transitionMat.DOKill(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(transitionMat.DOFloat(rad, "_Radius", transitionDuration)).SetEase(easeType).SetUpdate(true).SetId("Transition");
        seq.Play();
        Debug.Log(transitionMat.GetFloat(ShaderProps.Radius));
        Debug.Log(seq.IsPlaying());

        seq.OnComplete(() =>
        {
            Debug.Log(transitionMat.GetFloat(ShaderProps.Radius));
            isFinished = true;
            blocker.SetActive(false);
            DOTween.KillAll(true);
            //DOTween.Kill("Transition", true);
            StopAllCoroutines();
        });
    }
}
