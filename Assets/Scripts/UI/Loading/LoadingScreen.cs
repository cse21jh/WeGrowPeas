using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 로딩 오버레이. DontDestroyOnLoad로 상주하며 진행도·스피너·TMI를 보여준다.
/// 별도 로딩 씬 없이 어떤 전환에서도 재사용한다.
///
/// 사용: SceneLoader가 LoadSceneAsync 진행도를 <see cref="SetProgress"/>로 넘겨준다.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject root;          // 로딩 UI 전체 (켜고 끄는 대상)
    [SerializeField] private CanvasGroup canvasGroup;  // 페이드용
    [SerializeField] private Slider progressBar;       // 0~1
    [SerializeField] private Image progressFill;       // Slider 대신 fillAmount를 쓸 경우
    [SerializeField] private TMP_Text percentText;     // "42%"
    [SerializeField] private TMP_Text tmiText;         // TMI 문구

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Bar")]
    [Tooltip("바가 목표치까지 따라가는 속도(초당). 값이 크면 즉시 반영")]
    [SerializeField] private float barFollowSpeed = 2.5f;

    [Header("TMI")]
    [Tooltip("로딩이 길어지면 이 간격(초)마다 다음 문구로 교체. 0이면 교체 안 함")]
    [SerializeField] private float tmiSwapInterval = 4f;

    private float targetProgress;
    private float shownProgress;
    private float tmiTimer;

    public bool IsShowing { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (root != null) root.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 안전장치: 씬이 바뀌었는데도 로딩창이 떠 있으면(코루틴 중단 등) 강제로 닫는다.
    /// 검은 배경이 그대로 남는 것을 방지.
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!IsShowing) return;
        // 씬 로드 직후 바로 닫으면 새 씬 첫 프레임이 비칠 수 있어 잠깐 뒤에 닫는다.
        Invoke(nameof(Hide), 0.1f);
    }

    private void Update()
    {
        if (!IsShowing) return;

        // 진행도를 부드럽게 따라가게 (툭툭 튀지 않도록)
        shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, barFollowSpeed * Time.unscaledDeltaTime);
        ApplyBar(shownProgress);

        // 로딩이 길면 TMI 교체
        if (tmiSwapInterval > 0f && tmiText != null)
        {
            tmiTimer += Time.unscaledDeltaTime;
            if (tmiTimer >= tmiSwapInterval)
            {
                tmiTimer = 0f;
                tmiText.text = TmiPool.GetRandom();
            }
        }
    }

    /// <summary>로딩창 표시. TMI를 새로 뽑고 진행도를 0으로 초기화한다.</summary>
    public void Show()
    {
        IsShowing = true;
        targetProgress = 0f;
        shownProgress = 0f;
        tmiTimer = 0f;

        if (root != null) root.SetActive(true);
        if (tmiText != null) tmiText.text = TmiPool.GetRandom();
        ApplyBar(0f);

        StopAllCoroutines();
        StartCoroutine(Fade(1f));
    }

    /// <summary>진행도 갱신(0~1).</summary>
    public void SetProgress(float value)
    {
        targetProgress = Mathf.Clamp01(value);
    }

    /// <summary>로딩창 숨김. 페이드 아웃이 끝나면 비활성화.</summary>
    public void Hide()
    {
        if (!IsShowing) return;
        IsShowing = false;

        // 사라지기 직전엔 100%로 맞춰 보여준다.
        shownProgress = targetProgress = 1f;
        ApplyBar(1f);

        StopAllCoroutines();
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        yield return Fade(0f);
        if (root != null) root.SetActive(false);
    }

    private IEnumerator Fade(float to)
    {
        if (canvasGroup == null) yield break;

        float from = canvasGroup.alpha;
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void ApplyBar(float value)
    {
        if (progressBar != null) progressBar.value = value;
        if (progressFill != null) progressFill.fillAmount = value;
        if (percentText != null) percentText.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
