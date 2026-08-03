using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // ── 씬 전환 진입점 ────────────────────────────────────────────────────────
    // 화면 덮기(TransitionController) → 로딩창 → 씬 로드 → 화면 열기 순서를 여기서 소유한다.
    // 호출부는 Transition_Out()을 직접 부르지 말고 아래 메서드만 쓰면 된다.

    public void LoadStartScene()
    {
        StartCoroutine(LoadSceneRoutine("StartScene", useLoadingScreen: false, bgm: "StartScene"));
    }

    public void LoadIntroScene()
    {
        StartCoroutine(LoadSceneRoutine("IntroScene", useLoadingScreen: false, bgm: "IntroScene"));
    }

    public void LoadTutorialScene()
    {
        StartCoroutine(LoadSceneRoutine("Tutorial", useLoadingScreen: true));
    }

    public void LoadGardenScene()
    {
        StartCoroutine(LoadSceneRoutine("Garden_GrassUpdate", useLoadingScreen: true));
    }

    [Header("Loading")]
    [Tooltip("로딩창을 최소 이 시간(초)만큼은 보여준다. 너무 빨리 사라져 TMI를 못 읽는 것 방지")]
    [SerializeField] private float minLoadingTime = 4f;

    private bool isLoading;

    /// <summary>로딩창을 띄우고 씬을 비동기로 불러온다. (무거운 씬 전용)</summary>
    public void LoadWithLoadingScreen(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, useLoadingScreen: true));
    }

    /// <summary>
    /// 씬 전환 전체 순서를 담당한다.
    /// 1) 화면 덮기(Transition_Out) → 2) 로딩창 → 3) 비동기 로드 → 4) 로딩창 닫기 → 5) 화면 열기(Transition_In)
    /// </summary>
    private IEnumerator LoadSceneRoutine(string sceneName, bool useLoadingScreen, string bgm = null)
    {
        if (isLoading) yield break;
        isLoading = true;
        Time.timeScale = 1f;

        // 1) 화면 덮기 — 연출이 끝날 때까지 대기
        var transition = TransitionController.instance;
        if (transition != null)
        {
            var task = transition.Transition_Out();
            while (!task.IsCompleted) yield return null;
        }

        // 2) 로딩창 (무거운 씬만)
        var loading = useLoadingScreen ? LoadingScreen.Instance : null;
        if (loading != null) loading.Show();

        // 3) 비동기 로드
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false; // 로딩창을 충분히 보여준 뒤 전환

        float elapsed = 0f;
        float minTime = loading != null ? minLoadingTime : 0f;

        // 비동기 로드는 0.9에서 멈추고 활성화를 기다린다.
        while (op.progress < 0.9f || elapsed < minTime)
        {
            elapsed += Time.unscaledDeltaTime;

            if (loading != null)
            {
                // 실제 로드 진행도와 최소 시간 진행도 중 느린 쪽을 보여준다.
                float loadRatio = Mathf.Clamp01(op.progress / 0.9f);
                float timeRatio = minTime > 0f ? Mathf.Clamp01(elapsed / minTime) : 1f;
                loading.SetProgress(Mathf.Min(loadRatio, timeRatio));
            }
            yield return null;
        }

        if (loading != null)
        {
            loading.SetProgress(1f);
            yield return new WaitForSecondsRealtime(0.15f); // 100%를 잠깐 보여주고 전환
        }

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        if (!string.IsNullOrEmpty(bgm) && SoundManager.Instance != null)
            SoundManager.Instance.PlayBgm(bgm);

        // 새 씬이 첫 프레임을 그린 뒤에 닫아야 검은 화면이 비치지 않는다.
        yield return null;
        yield return new WaitForEndOfFrame();

        // 4) 로딩창 닫기
        if (loading != null) loading.Hide();

        // 5) 화면 열기
        if (transition != null) transition.Transition_In();

        isLoading = false;
    }

    public void LoadGameOverScene()
    {
        StartCoroutine(LoadSceneRoutine("GameOverScene", useLoadingScreen: false));
    }


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"{scene.name} 씬 로드됨 (모드: {mode})");

        // 전환 순서는 LoadSceneRoutine이 소유한다.
        // 여기서는 SceneLoader를 거치지 않은 씬 로드(직접 LoadScene 호출 등)만 보정한다.
        if (isLoading) return;

        var transition = FindAnyObjectByType<TransitionController>();
        if (transition == null) return;

        transition.transitionMat.SetFloat("_Radius", 0f);
        StartCoroutine(Transition(1.0f));
    }

    private IEnumerator Transition(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        var transition = FindAnyObjectByType<TransitionController>();
        if (transition != null) transition.Transition_In();
    }
}
