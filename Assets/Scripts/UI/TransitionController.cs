using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

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
                transitionMat.SetFloat("_Radius", 1.5f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        transitionMat.DOKill(true);
        transitionMat.SetFloat("_Radius", 1.5f);
        isFinished = true;
    }

    private void Start()
    {
        /*
        if (SceneManager.GetActiveScene().name == "StartScene 1")
        {
            transitionMat.SetFloat("_Radius", 1.5f);
            isFinished = true;
        }
        */
    }

    private void Update()
    {
        if (isDebug)
        {
            Debug.Log(transitionMat.GetFloat("_Radius"));
            //transitionMat.DOKill(true);
            //transitionMat.SetFloat("_Radius", debugRadius);
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
        transitionMat.SetFloat("_Radius", 0f);
        isFinished = false;
        Transition(1.5f);
    }

    /// <summary>
    /// 점점 어두워지는 트랜지션.
    /// 아웃트로 성격
    /// </summary>
    public void Transition_Out()
    {
        blocker.SetActive(true);
        transitionMat.DOKill(true);
        transitionMat.SetFloat("_Radius", 1.5f);
        isFinished = false;
        Transition(0f);
    }

    public bool IsFinished()
    {
        return isFinished;
    }

    private void Transition(float rad)
    {
        transitionMat.DOKill(true);

        Sequence seq = DOTween.Sequence();

        seq.Join(transitionMat.DOFloat(rad, "_Radius", transitionDuration).SetEase(easeType).SetUpdate(true).SetId("Transition")).SetLink(this.gameObject);
        seq.OnComplete(() =>
        {
            blocker.SetActive(false);
            isFinished = true;
            DOTween.KillAll(true);
            //DOTween.Kill("Transition", true);
            StopAllCoroutines();
        });
    }
}
