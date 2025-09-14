using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class TransitionController : MonoBehaviour
{
    public static TransitionController instance;

    [SerializeField] private Material transitionMat;
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
            transitionMat.SetFloat("_Radius", debugRadius);
        }
    }


    /// <summary>
    /// 점점 밝아지는 트랜지션.
    /// 인트로 성격
    /// </summary>
    public void Transition_In()
    {
        Debug.Log("Transition In");
        transitionMat.SetFloat("_Radius", 0f);
        isFinished = false;
        StartCoroutine(Transition(1.5f));
    }

    /// <summary>
    /// 점점 어두워지는 트랜지션.
    /// 아웃트로 성격
    /// </summary>
    public void Transition_Out()
    {
        transitionMat.SetFloat("_Radius", 1.5f);
        isFinished = false;
        StartCoroutine(Transition(0f));
    }

    public bool IsFinished()
    {
        return isFinished;
    }

    private IEnumerator Transition(float rad)
    {
        DOTween.To(() => transitionMat.GetFloat("_Radius"), x => transitionMat.SetFloat("_Radius", x),
            rad, transitionDuration).SetEase(easeType).SetUpdate(true);

        yield return new WaitUntil(() => transitionMat.GetFloat("_Radius") == rad);

        isFinished = true;
        DOTween.KillAll();
        StopAllCoroutines();
    }
}
