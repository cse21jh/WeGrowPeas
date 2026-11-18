using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 런이 끝나고 결과 화면(GameOverScene)에 들어왔을 때, 이번 런에서 열린 새벽 단계를 보여 주는 간단한 연출.
/// "새벽 단계 해금!" 아래 단계 숫자가 룰렛처럼 한 칸씩 굴러 올라가 새 단계에 멈추고, 화면을 누르면 닫힌다.
/// 새벽 모드가 처음 열렸을 때는 숫자 대신 "해금!"을 크게 띄운다.
/// 이미지 없이 글꼴만 써서 실행 중에 만든다.
/// </summary>
public class DawnUnlockOverlay : MonoBehaviour
{
    private const string ResultSceneName = "GameOverScene";
    /// <summary>화면 전환(로딩 화면이 걷히는 연출)이 끝난 뒤에 시작한다.</summary>
    public const float DefaultStartDelay = 1.2f;
    private const int MaxStage = 12;
    /// <summary>한 칸 굴러 올라가는 데 걸리는 시간. 칸이 많아도 너무 길어지지 않게 전체 시간에 상한을 둔다.</summary>
    private const float SecondsPerStep = 0.55f;
    private const float MaxSpinDuration = 4.5f;
    private const float TickInterval = 0.05f;

    private int from;
    private int to;
    private float startDelay = DefaultStartDelay;
    private CanvasGroup group;
    private TMP_Text title;
    private TMP_Text rangeText;
    private TMP_Text hint;
    /// <summary>새벽 모드가 처음 열렸을 때 숫자 대신 크게 띄우는 "해금!".</summary>
    private TMP_Text firstUnlockText;

    // 룰렛: 창(마스크) 안에서 지금 숫자와 다음 숫자 두 개를 위로 굴린다.
    private RectTransform reel;
    private TMP_Text reelCurrent;
    private TMP_Text reelNext;
    private float reelStep;
    private int reelStart;
    private int reelSteps;
    private int lastShownIndex = -1;
    private float lastTickTime;
    private Tween spinTween;

    private bool canClose;
    private bool closing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != ResultSceneName) return;
        if (!DawnUnlockAnnouncement.TryConsume(out int from, out int to)) return;
        Show(from, to);
    }

    /// <summary>지금 씬 위에 해금 연출을 띄운다. from이 0이면 새벽 모드가 처음 열린 것으로 보여 준다.</summary>
    public static DawnUnlockOverlay Show(int from, int to, float delay = DefaultStartDelay)
    {
        var root = new GameObject(nameof(DawnUnlockOverlay), typeof(RectTransform));
        var overlay = root.AddComponent<DawnUnlockOverlay>();
        overlay.from = Mathf.Clamp(from, 0, MaxStage);
        overlay.to = Mathf.Clamp(to, 1, MaxStage);
        overlay.startDelay = delay;
        overlay.Build();
        return overlay;
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        // 씬의 UI와 같은 크기 기준을 쓴다.
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.matchWidthOrHeight = 0.5f;
        var sceneScaler = FindAnyObjectByType<CanvasScaler>();
        Vector2 reference = sceneScaler != null && sceneScaler != scaler && sceneScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            ? sceneScaler.referenceResolution
            : new Vector2(1920f, 1080f);
        scaler.referenceResolution = reference;
        float unit = reference.y;

        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;

        var background = new GameObject("Background", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        background.transform.SetParent(transform, false);
        Stretch(background.rectTransform);
        background.color = new Color(0f, 0f, 0f, 0.88f);
        var button = background.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Close);

        // 씬마다 배경이 달라 글자 뒤에 어두운 판을 깐다.
        var plate = new GameObject("Plate", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        plate.transform.SetParent(transform, false);
        plate.rectTransform.sizeDelta = new Vector2(unit * 1.0f, unit * 0.68f);
        plate.rectTransform.anchoredPosition = new Vector2(0f, -unit * 0.04f);
        plate.color = new Color(0.08f, 0.06f, 0.05f, 0.95f);
        plate.raycastTarget = false;

        var style = Resources.Load<DawnUnlockStyle>(DawnUnlockStyle.ResourcePath);
        TMP_FontAsset font = style != null && style.font != null ? style.font : FindFont();
        Material material = style != null && style.font != null ? style.material : null;

        title = CreateText("Title", transform, font, material, unit * 0.075f, new Vector2(0f, unit * 0.16f), new Color(1f, 0.85f, 0.35f));
        rangeText = CreateText("Range", transform, font, material, unit * 0.045f, new Vector2(0f, -unit * 0.13f), new Color(1f, 1f, 1f, 0.85f));
        hint = CreateText("Hint", transform, font, material, unit * 0.035f, new Vector2(0f, -unit * 0.3f), new Color(1f, 1f, 1f, 0.6f));
        bool firstUnlock = from <= 0;
        title.text = Loc.T(firstUnlock ? "dawn.unlock.first" : "dawn.unlock.title");
        rangeText.text = firstUnlock ? "" : Loc.T("dawn.unlock.range", ("from", from), ("to", to));
        hint.text = Loc.T("dawn.unlock.close");

        if (firstUnlock)
        {
            firstUnlockText = CreateText("Unlocked", transform, font, material, unit * 0.14f, Vector2.zero, Color.white);
            firstUnlockText.text = Loc.T("dawn.unlock.first.big");
            firstUnlockText.transform.localScale = Vector3.zero;
        }
        else
        {
            // 지금 단계에서 출발해 한 칸씩 굴러 올라가 새 단계에 멈춘다.
            BuildReel(font, material, unit * 0.14f);
            reelStart = from;
            reelSteps = to - from;
            SetReel(0f);
        }

        title.transform.localScale = Vector3.zero;
        rangeText.alpha = 0f;
        hint.alpha = 0f;
    }

    /// <summary>
    /// "{stage}단계" / "Stage {stage}"에서 숫자 자리만 굴리고 앞뒤 글자는 고정한다.
    /// 숫자 칸은 두 자리 폭으로 잡아 숫자가 바뀌어도 앞뒤 글자가 흔들리지 않게 한다.
    /// </summary>
    private void BuildReel(TMP_FontAsset font, Material material, float size)
    {
        const string Placeholder = "{stage}";
        string template = Loc.T("dawn.unlock.stage");
        int at = template.IndexOf(Placeholder, System.StringComparison.Ordinal);
        string prefix = at >= 0 ? template.Substring(0, at) : "";
        string suffix = at >= 0 ? template.Substring(at + Placeholder.Length) : template;

        var row = new GameObject("StageRow", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(transform, false);
        row.anchoredPosition = Vector2.zero;

        var prefixText = CreateText("Prefix", row, font, material, size, Vector2.zero, Color.white);
        var suffixText = CreateText("Suffix", row, font, material, size, Vector2.zero, Color.white);
        prefixText.text = prefix;
        suffixText.text = suffix;

        float prefixWidth = string.IsNullOrEmpty(prefix) ? 0f : prefixText.GetPreferredValues(prefix).x;
        float suffixWidth = string.IsNullOrEmpty(suffix) ? 0f : suffixText.GetPreferredValues(suffix).x;
        float reelWidth = prefixText.GetPreferredValues("88").x * 1.1f;
        float left = -(prefixWidth + reelWidth + suffixWidth) * 0.5f;
        reelStep = size * 1.25f;

        Place(prefixText, left, prefixWidth, TextAlignmentOptions.Right);
        Place(suffixText, left + prefixWidth + reelWidth, suffixWidth, TextAlignmentOptions.Left);

        reel = new GameObject("Reel", typeof(RectTransform)).GetComponent<RectTransform>();
        reel.SetParent(row, false);
        reel.anchorMin = reel.anchorMax = reel.pivot = new Vector2(0.5f, 0.5f);
        reel.sizeDelta = new Vector2(reelWidth, reelStep);
        reel.anchoredPosition = new Vector2(left + prefixWidth + reelWidth * 0.5f, 0f);
        reel.gameObject.AddComponent<RectMask2D>();

        reelCurrent = CreateText("Current", reel, font, material, size, Vector2.zero, Color.white);
        reelNext = CreateText("Next", reel, font, material, size, Vector2.zero, Color.white);

        // 한 자리 숫자가 두 자리 칸 가운데 뜨면 "6 단계"처럼 벌어지므로, 붙는 글자 쪽으로 붙인다.
        var alignment = suffixWidth > 0f && prefixWidth <= 0f ? TextAlignmentOptions.Right
            : prefixWidth > 0f && suffixWidth <= 0f ? TextAlignmentOptions.Left
            : TextAlignmentOptions.Center;
        foreach (var text in new[] { reelCurrent, reelNext })
        {
            text.alignment = alignment;
            text.rectTransform.sizeDelta = new Vector2(reelWidth, text.rectTransform.sizeDelta.y);
        }
    }

    private static void Place(TMP_Text text, float left, float width, TextAlignmentOptions alignment)
    {
        text.alignment = alignment;
        var rect = text.rectTransform;
        rect.sizeDelta = new Vector2(Mathf.Max(width, 1f) + 4f, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(left + width * 0.5f, 0f);
    }

    private static int StageAt(int start, int index) => (start - 1 + index) % MaxStage + 1;

    /// <summary>룰렛 위치를 그린다. 정수부는 지금 숫자이고, 소수부만큼 위로 굴러가며 다음 숫자가 밑에서 올라온다.</summary>
    private void SetReel(float position)
    {
        int index = Mathf.Min(Mathf.FloorToInt(position), reelSteps);
        float frac = index >= reelSteps ? 0f : position - index;

        reelCurrent.text = StageAt(reelStart, index).ToString();
        reelNext.text = StageAt(reelStart, index + 1).ToString();
        reelCurrent.rectTransform.anchoredPosition = new Vector2(0f, frac * reelStep);
        reelNext.rectTransform.anchoredPosition = new Vector2(0f, (frac - 1f) * reelStep);
        reelNext.enabled = frac > 0f;

        if (index == lastShownIndex) return;
        lastShownIndex = index;
        // 빠르게 돌 때 소리가 겹치지 않게 간격을 둔다.
        if (spinTween != null && Time.unscaledTime - lastTickTime >= TickInterval)
        {
            lastTickTime = Time.unscaledTime;
            PlaySound(SoundIds.Button);
        }
    }

    private void Start()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        group.DOFade(1f, 0.3f).SetUpdate(true).SetLink(gameObject);
        yield return new WaitForSecondsRealtime(0.2f);

        title.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        PlaySound(SoundIds.Button);
        yield return new WaitForSecondsRealtime(0.5f);
        if (closing) yield break;

        if (firstUnlockText != null)
        {
            ShowFirstUnlock();
        }
        else
        {
            // 한 칸씩 천천히 굴러 올라가다가 마지막 칸에서 살짝 느려지며 멈춘다.
            float position = 0f;
            float duration = Mathf.Min(SecondsPerStep * reelSteps, MaxSpinDuration);
            spinTween = DOTween.To(() => position, p => { position = p; SetReel(p); }, reelSteps, duration)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetLink(gameObject);
            yield return spinTween.WaitForCompletion();
            if (closing) yield break;
            Land();
        }

        yield return new WaitForSecondsRealtime(0.5f);
        if (closing) yield break;
        DOTween.To(() => rangeText.alpha, a => rangeText.alpha = a, 1f, 0.3f).SetUpdate(true).SetLink(gameObject);
        yield return new WaitForSecondsRealtime(0.4f);
        if (closing) yield break;
        DOTween.To(() => hint.alpha, a => hint.alpha = a, 1f, 0.3f).SetUpdate(true).SetLink(gameObject);
        canClose = true;
    }

    /// <summary>새 단계에 멈췄을 때: 숫자를 노랗게 물들이며 크게 튀긴다.</summary>
    private void Land()
    {
        spinTween = null;
        SetReel(reelSteps);
        PlaySound(SoundIds.QuestSuccess);

        var row = reel.parent;
        row.DOKill(true);
        row.localScale = Vector3.one;
        row.DOPunchScale(Vector3.one * 0.35f, 0.45f, 6, 0.6f).SetUpdate(true).SetLink(gameObject);

        reelCurrent.color = new Color(1f, 0.85f, 0.35f);
        DOTween.To(() => reelCurrent.color, c => reelCurrent.color = c, Color.white, 0.6f)
            .SetDelay(0.25f).SetUpdate(true).SetLink(gameObject);
    }

    /// <summary>새벽 모드가 처음 열렸을 때: "해금!"을 크게 튀어나오게 한다.</summary>
    private void ShowFirstUnlock()
    {
        PlaySound(SoundIds.QuestSuccess);
        firstUnlockText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack, 2.2f).SetUpdate(true).SetLink(gameObject);
        firstUnlockText.color = new Color(1f, 0.85f, 0.35f);
        DOTween.To(() => firstUnlockText.color, c => firstUnlockText.color = c, Color.white, 0.6f)
            .SetDelay(0.35f).SetUpdate(true).SetLink(gameObject);
    }

    private void Update()
    {
        if (canClose && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Close()
    {
        // 룰렛이 멈추기 전에 누르면 끝 상태로 건너뛴다.
        if (!canClose)
        {
            closing = true;
            canClose = true;
            if (spinTween != null) spinTween.Kill();
            spinTween = null;
            if (firstUnlockText != null)
            {
                firstUnlockText.transform.DOKill(true);
                firstUnlockText.transform.localScale = Vector3.one;
                DOTween.Kill(firstUnlockText);
                firstUnlockText.color = Color.white;
            }
            else
            {
                SetReel(reelSteps);
                reelCurrent.color = Color.white;
            }
            title.transform.DOKill(true);
            title.transform.localScale = Vector3.one;
            rangeText.alpha = 1f;
            hint.alpha = 1f;
            group.DOKill(true);
            group.alpha = 1f;
            return;
        }

        if (group == null || !group.blocksRaycasts) return;
        group.blocksRaycasts = false;
        group.DOFade(0f, 0.25f).SetUpdate(true).SetLink(gameObject)
            .OnComplete(() => Destroy(gameObject));
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, Material material, float size, Vector2 position, Color color)
    {
        var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (font != null) text.font = font;
        if (material != null) text.fontSharedMaterial = material;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size * 20f, size * 1.6f);
        return text;
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.font != null) return text.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void PlaySound(string id)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect(id);
    }
}
