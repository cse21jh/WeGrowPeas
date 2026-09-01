using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EconomyFeedbackController : MonoBehaviour
{
    private const int PrewarmCoinCount = 16;
    // 밭 최대 8열 × 4칸이 같은 프레임에 가치 상승할 수 있다.
    private const int PrewarmPopupCount = 32;
    private const int PrewarmGlowCount = 4;
    private const int PrewarmPlusCount = 8;
    private const int PrewarmSproutCount = 8;
    private const int FeedbackSortingOrder = 32000;
    private const float WorldGainHudDelay = 0.50f;
    private const float HudCountDuration = 0.68f;
    private const float BreedCountDuration = 0.58f;
    private static readonly Vector3 SaleWorldOffset = Vector3.up * 0.45f;
    private static readonly Vector3 DefaultPlantValueStartWorldOffset = Vector3.up * 0.9f;
    private static readonly Vector2 DefaultPlantValueEndUiOffset = Vector2.up * 64f;
    private static readonly Color HudGlowColor = new Color(1f, 0.82f, 0.22f, 0.18f);
    private static readonly Color SpendGlowColor = new Color(1f, 0.24f, 0.18f, 0.24f);
    private static readonly Color SpendCoinColor = new Color(1f, 0.70f, 0.64f, 0.94f);
    private static readonly Color SpendIconFlashColor = new Color(1f, 0.42f, 0.34f, 1f);
    private static readonly Color DefaultPositiveAmountColor = new Color(1f, 0.82f, 0.18f, 1f);
    private static readonly Color DefaultNegativeAmountColor = new Color(1f, 0.35f, 0.22f, 1f);
    private static readonly Color BreedGlowColor = new Color(0.48f, 1f, 0.58f, 0.18f);
    private static readonly Color DefaultHudPlusColor = new Color(0.48f, 1f, 0.58f, 0.42f);

    private enum HudFeedbackOwner
    {
        Gold,
        Breed
    }

    private enum CoinMotionKind
    {
        None,
        Gain,
        Spend
    }

    private static EconomyFeedbackController instance;

    [SerializeField]
    [Tooltip("비워 두면 Resources/UI/EconomyFeedbackStyle 에셋을 자동으로 불러옵니다.")]
    private EconomyFeedbackStyle feedbackStyle;

    private readonly Queue<CoinFx> coinPool = new Queue<CoinFx>();
    private readonly Queue<AmountPopupFx> popupPool = new Queue<AmountPopupFx>();
    private readonly Queue<GlowFx> glowPool = new Queue<GlowFx>();
    private readonly Queue<PlusFx> plusPool = new Queue<PlusFx>();
    private readonly Queue<SproutFx> sproutPool = new Queue<SproutFx>();
    private readonly List<CoinFx> allCoins = new List<CoinFx>();
    private readonly List<AmountPopupFx> allPopups = new List<AmountPopupFx>();
    private readonly List<GlowFx> allGlows = new List<GlowFx>();
    private readonly List<PlusFx> allPluses = new List<PlusFx>();
    private readonly List<SproutFx> allSprouts = new List<SproutFx>();
    private readonly Dictionary<int, AmountPopupFx> activePlantPopups = new Dictionary<int, AmountPopupFx>();

    private RectTransform effectsRoot;
    private TextMeshProUGUI coinText;
    private RectTransform coinTextTarget;
    private RectTransform goldTarget;
    private Image coinHudImage;
    private Vector3 coinTextBaseScale = Vector3.one;
    private Vector3 goldTargetBaseScale = Vector3.one;
    private Color coinHudBaseColor = Color.white;
    private Sprite coinSprite;
    private Texture2D glowTexture;
    private Sprite glowSprite;
    private Tween goldPunchTween;
    private Tween goldCounterTween;
    private Sequence goldGainBounceSequence;
    private int displayedGold;
    private int goldCounterTargetBalance;
    private int goldHudAnimationVersion;
    private bool hasPendingGoldSync;
    private int pendingGoldBalance;
    private TextMeshProUGUI breedCountText;
    private RectTransform breedCountTextTarget;
    private RectTransform breedIconTarget;
    private Sprite breedSprite;
    private Vector3 breedCountTextBaseScale = Vector3.one;
    private Vector3 breedIconBaseScale = Vector3.one;
    private Tween breedCounterTween;
    private Sequence breedGainBounceSequence;
    private int displayedBreedCount;
    private int breedCounterTargetCount;
    private int breedHudAnimationVersion;
    private bool prewarmed;

    public static EconomyFeedbackController EnsureExists(TextMeshProUGUI coinText)
    {
        if (instance != null)
        {
            if (!instance.gameObject.activeSelf)
                instance.gameObject.SetActive(true);
            if (!instance.enabled)
                instance.enabled = true;
            instance.BindCoinUi(coinText);
            instance.Prewarm();
            return instance;
        }

        EconomyFeedbackController existing = FindAnyObjectByType<EconomyFeedbackController>();
        if (existing != null)
        {
            instance = existing;
            existing.BindCoinUi(coinText);
            existing.Prewarm();
            return existing;
        }

        GameObject root = new GameObject(
            "Economy Feedback Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = FeedbackSortingOrder;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        EconomyFeedbackController controller = root.AddComponent<EconomyFeedbackController>();
        controller.BindCoinUi(coinText);
        controller.Prewarm();
        return controller;
    }

    public static void BindBreedCountUi(TextMeshProUGUI newBreedCountText)
    {
        if (newBreedCountText == null)
            return;

        try
        {
            EconomyFeedbackController controller = instance ?? EnsureExists(null);
            controller.BindBreedCountUiInternal(newBreedCountText);
        }
        catch (Exception exception)
        {
            // HUD 바인딩 실패가 Grid 초기화를 중단시키지 않게 한다.
            Debug.LogException(exception);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        ResolveFeedbackStyle();
        EnsureEffectsRoot();
    }

    private void OnEnable()
    {
        GameEvents.OnGoldChanged += HandleGoldChanged;
        GameEvents.OnGoldFeedbackRequested += HandleGoldFeedback;
        GameEvents.OnPlantValueFeedbackRequested += HandlePlantValueFeedback;
        GameEvents.OnBreedCountChanged += HandleBreedCountChanged;
        GameEvents.OnBreedCountFeedbackRequested += HandleBreedCountFeedback;
    }

    private void OnDisable()
    {
        GameEvents.OnGoldChanged -= HandleGoldChanged;
        GameEvents.OnGoldFeedbackRequested -= HandleGoldFeedback;
        GameEvents.OnPlantValueFeedbackRequested -= HandlePlantValueFeedback;
        GameEvents.OnBreedCountChanged -= HandleBreedCountChanged;
        GameEvents.OnBreedCountFeedbackRequested -= HandleBreedCountFeedback;
        StopAndRecycleAll();
    }

    private void OnDestroy()
    {
        StopAndRecycleAll();
        DestroyRuntimeAssets();
        if (instance == this)
            instance = null;
    }

    private void LateUpdate()
    {
        if (!hasPendingGoldSync)
            return;

        int balance = pendingGoldBalance;
        hasPendingGoldSync = false;
        StopGoldGainHudAnimation(balance, true);
    }

    private void EnsureEffectsRoot()
    {
        if (effectsRoot != null)
            return;

        GameObject root = new GameObject("Effects", typeof(RectTransform));
        effectsRoot = root.GetComponent<RectTransform>();
        effectsRoot.SetParent(transform, false);
        effectsRoot.anchorMin = Vector2.zero;
        effectsRoot.anchorMax = Vector2.one;
        effectsRoot.offsetMin = Vector2.zero;
        effectsRoot.offsetMax = Vector2.zero;
        effectsRoot.pivot = new Vector2(0.5f, 0.5f);
    }

    private void BindCoinUi(TextMeshProUGUI newCoinText)
    {
        if (newCoinText == null)
            return;

        StopGoldGainHudAnimation(goldCounterTargetBalance, false);
        coinText = newCoinText;
        coinTextTarget = newCoinText.rectTransform;
        Image sourceCoinImage = FindCoinImage(newCoinText);
        coinHudImage = sourceCoinImage;
        goldTarget = sourceCoinImage != null
            ? sourceCoinImage.rectTransform
            : newCoinText.transform.parent as RectTransform;
        if (goldTarget == null)
            goldTarget = newCoinText.rectTransform;

        coinTextBaseScale = coinTextTarget != null ? coinTextTarget.localScale : Vector3.one;
        goldTargetBaseScale = goldTarget != null ? goldTarget.localScale : Vector3.one;
        coinHudBaseColor = coinHudImage != null ? coinHudImage.color : Color.white;
        coinSprite = sourceCoinImage != null ? sourceCoinImage.sprite : null;

        for (int i = 0; i < allCoins.Count; i++)
            ApplyCoinSprite(allCoins[i].Image);

        for (int i = 0; i < allPopups.Count; i++)
        {
            ApplyCoinSprite(allPopups[i].Icon);
            ApplyAmountTextStyle(allPopups[i].Text, true);
        }

    }

    private void BindBreedCountUiInternal(TextMeshProUGUI newBreedCountText)
    {
        if (newBreedCountText == null)
            return;

        int currentCount = ParseBreedCount(newBreedCountText.text);
        StopBreedCountHudAnimation(currentCount, false);
        breedCountText = newBreedCountText;
        breedCountTextTarget = newBreedCountText.rectTransform;
        Image sourceBreedImage = FindBreedImage(newBreedCountText);
        breedSprite = sourceBreedImage != null ? sourceBreedImage.sprite : null;
        breedIconTarget = sourceBreedImage != null
            ? sourceBreedImage.rectTransform
            : newBreedCountText.transform.parent as RectTransform;
        if (breedIconTarget == null)
            breedIconTarget = newBreedCountText.rectTransform;

        breedCountTextBaseScale = breedCountTextTarget != null
            ? breedCountTextTarget.localScale
            : Vector3.one;
        breedIconBaseScale = breedIconTarget != null
            ? breedIconTarget.localScale
            : Vector3.one;
        displayedBreedCount = currentCount;
        breedCounterTargetCount = currentCount;

        for (int i = 0; i < allSprouts.Count; i++)
            ApplyBreedSprite(allSprouts[i].Image);
    }

    private Image FindCoinImage(TextMeshProUGUI sourceText)
    {
        Transform cursor = sourceText != null ? sourceText.transform.parent : null;
        Image bestImage = null;
        int bestScore = int.MinValue;

        for (int depth = 0; cursor != null && depth < 4; depth++, cursor = cursor.parent)
        {
            Image[] images = cursor.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.sprite == null)
                    continue;

                string objectName = image.gameObject.name ?? string.Empty;
                int score = 0;
                if (objectName.IndexOf("coin", StringComparison.OrdinalIgnoreCase) >= 0) score += 50;
                if (objectName.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
                if (objectName.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0) score += 30;
                if (image.transform.parent == sourceText.transform.parent) score += 15;
                if (image.transform == sourceText.transform.parent) score -= 25;
                score -= depth * 5;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestImage = image;
                }
            }

            if (bestScore >= 80)
                break;
        }

        // Coin/Gold라는 명시적 단서가 없는 배경·버튼 이미지는 동전으로 사용하지 않는다.
        return bestScore >= 40 ? bestImage : null;
    }

    private Image FindBreedImage(TextMeshProUGUI sourceText)
    {
        if (sourceText == null || sourceText.transform.parent == null)
            return null;

        Image[] images = sourceText.transform.parent.GetComponentsInChildren<Image>(true);
        Image bestImage = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
                continue;

            string objectName = image.gameObject.name ?? string.Empty;
            int score = 0;
            if (objectName.IndexOf("breed", StringComparison.OrdinalIgnoreCase) >= 0) score += 50;
            if (objectName.IndexOf("sprout", StringComparison.OrdinalIgnoreCase) >= 0) score += 45;
            if (objectName.IndexOf("seed", StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
            if (objectName.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0) score += 35;
            if (image.transform.parent == sourceText.transform.parent) score += 20;
            if (image.transform == sourceText.transform.parent) score -= 40;

            if (score > bestScore)
            {
                bestScore = score;
                bestImage = image;
            }
        }

        return bestScore >= 55 ? bestImage : null;
    }

    private void Prewarm()
    {
        EnsureEffectsRoot();
        if (prewarmed)
            return;

        prewarmed = true;
        for (int i = 0; i < PrewarmCoinCount; i++)
        {
            CoinFx fx = CreateCoinFx();
            fx.Root.gameObject.SetActive(false);
            coinPool.Enqueue(fx);
        }

        for (int i = 0; i < PrewarmPopupCount; i++)
        {
            AmountPopupFx fx = CreatePopupFx();
            fx.Root.gameObject.SetActive(false);
            popupPool.Enqueue(fx);
        }

        EnsureGlowSprite();
        for (int i = 0; i < PrewarmGlowCount; i++)
        {
            GlowFx fx = CreateGlowFx();
            fx.Root.gameObject.SetActive(false);
            glowPool.Enqueue(fx);
        }

        for (int i = 0; i < PrewarmPlusCount; i++)
        {
            PlusFx fx = CreatePlusFx();
            fx.Root.gameObject.SetActive(false);
            plusPool.Enqueue(fx);
        }

        for (int i = 0; i < PrewarmSproutCount; i++)
        {
            SproutFx fx = CreateSproutFx();
            fx.Root.gameObject.SetActive(false);
            sproutPool.Enqueue(fx);
        }
    }

    private CoinFx CreateCoinFx()
    {
        GameObject go = new GameObject("Pooled Coin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(effectsRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(28f, 28f);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        ApplyCoinSprite(image);

        CoinFx fx = new CoinFx(rect, image);
        allCoins.Add(fx);
        return fx;
    }

    private SproutFx CreateSproutFx()
    {
        GameObject go = new GameObject(
            "Pooled Breed Sprout",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(effectsRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(24f, 24f);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        ApplyBreedSprite(image);

        SproutFx fx = new SproutFx(rect, image);
        allSprouts.Add(fx);
        return fx;
    }

    private AmountPopupFx CreatePopupFx()
    {
        GameObject go = new GameObject("Pooled Gold Amount", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(effectsRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(170f, 42f);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.ignoreParentGroups = false;

        GameObject iconGo = new GameObject("Coin Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(30f, 30f);

        Image icon = iconGo.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        ApplyCoinSprite(icon);

        GameObject textGo = new GameObject("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(36f, 0f);
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        ApplyAmountTextStyle(text, true);

        AmountPopupFx fx = new AmountPopupFx(
            rect,
            group,
            icon,
            textRect,
            text);
        allPopups.Add(fx);
        return fx;
    }

    private GlowFx CreateGlowFx()
    {
        EnsureGlowSprite();
        GameObject go = new GameObject("Pooled Hud Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(effectsRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(40f, 40f);

        Image image = go.GetComponent<Image>();
        image.sprite = glowSprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.color = new Color(HudGlowColor.r, HudGlowColor.g, HudGlowColor.b, 0f);

        GlowFx fx = new GlowFx(rect, image);
        allGlows.Add(fx);
        return fx;
    }

    private PlusFx CreatePlusFx()
    {
        GameObject go = new GameObject("Pooled Hud Plus", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(effectsRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(12f, 12f);

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image horizontal = CreatePlusBar(rect, "Horizontal", new Vector2(9f, 2.2f));
        Image vertical = CreatePlusBar(rect, "Vertical", new Vector2(2.2f, 9f));

        PlusFx fx = new PlusFx(rect, group, horizontal, vertical);
        allPluses.Add(fx);
        return fx;
    }

    private Image CreatePlusBar(RectTransform parent, string objectName, Vector2 size)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = null;
        image.color = GetHudPlusColor();
        image.raycastTarget = false;
        return image;
    }

    private void ApplyCoinSprite(Image image)
    {
        if (image == null)
            return;

        image.sprite = coinSprite;
        image.enabled = coinSprite != null;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void ApplyBreedSprite(Image image)
    {
        if (image == null)
            return;

        image.sprite = breedSprite;
        image.enabled = breedSprite != null;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void ResolveFeedbackStyle()
    {
        if (feedbackStyle == null)
            feedbackStyle = Resources.Load<EconomyFeedbackStyle>(EconomyFeedbackStyle.ResourcesPath);
    }

    private void ApplyAmountTextStyle(TextMeshProUGUI text, bool positive)
    {
        if (text == null)
            return;

        ResolveFeedbackStyle();
        EconomyFeedbackAmountTextStyle amountStyle = positive
            ? feedbackStyle?.GainAmount
            : feedbackStyle?.SpendAmount;

        TMP_FontAsset configuredFont = amountStyle?.FontAsset;
        TMP_FontAsset resolvedFont = configuredFont != null
            ? configuredFont
            : coinText != null
                ? coinText.font
                : null;

        if (resolvedFont != null)
            text.font = resolvedFont;

        Material configuredMaterial = amountStyle?.MaterialPreset;
        if (configuredMaterial != null)
        {
            text.fontSharedMaterial = configuredMaterial;
        }
        else if (configuredFont != null && configuredFont.material != null)
        {
            text.fontSharedMaterial = configuredFont.material;
        }
        else if (coinText != null && coinText.fontSharedMaterial != null)
        {
            text.fontSharedMaterial = coinText.fontSharedMaterial;
        }
        else if (resolvedFont != null && resolvedFont.material != null)
        {
            text.fontSharedMaterial = resolvedFont.material;
        }

        // 합성 Bold는 작은 글자에서 숫자 내부를 메울 수 있으므로 기본값은 Normal이다.
        text.fontStyle = amountStyle?.FontStyle ?? FontStyles.Normal;
        text.fontSize = amountStyle?.FontSize ?? (positive ? 22f : 20f);
        text.color = GetAmountColor(positive);
    }

    private Color GetAmountColor(bool positive)
    {
        ResolveFeedbackStyle();
        EconomyFeedbackAmountTextStyle amountStyle = positive
            ? feedbackStyle?.GainAmount
            : feedbackStyle?.SpendAmount;
        return amountStyle?.Color
            ?? (positive ? DefaultPositiveAmountColor : DefaultNegativeAmountColor);
    }

    private Color GetHudPlusColor()
    {
        ResolveFeedbackStyle();
        return feedbackStyle != null ? feedbackStyle.HudPlusColor : DefaultHudPlusColor;
    }

    private Vector3 GetPlantValueStartWorldOffset()
    {
        ResolveFeedbackStyle();
        return feedbackStyle != null
            ? feedbackStyle.PlantValueStartWorldOffset
            : DefaultPlantValueStartWorldOffset;
    }

    private Vector2 GetPlantValueEndUiOffset()
    {
        ResolveFeedbackStyle();
        return feedbackStyle != null
            ? feedbackStyle.PlantValueEndUiOffset
            : DefaultPlantValueEndUiOffset;
    }

    private void EnsureGlowSprite()
    {
        if (glowSprite != null)
            return;

        const int size = 64;
        glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "Runtime Coin Glow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float inverseRadius = 1f / center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) * inverseRadius;
                float dy = (y - center) * inverseRadius;
                float radial = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                float alpha = radial * radial * (3f - 2f * radial);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        glowTexture.SetPixels32(pixels);
        glowTexture.Apply(false, true);
        glowSprite = Sprite.Create(
            glowTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        glowSprite.name = "Runtime Coin Glow Sprite";
        glowSprite.hideFlags = HideFlags.HideAndDontSave;
    }

    private void DestroyRuntimeAssets()
    {
        if (glowSprite != null)
            Destroy(glowSprite);
        if (glowTexture != null)
            Destroy(glowTexture);

        glowSprite = null;
        glowTexture = null;
    }

    private void HandleGoldFeedback(GoldFeedbackData data)
    {
        if (data.Delta > 0)
        {
            hasPendingGoldSync = false;
            RecycleCoinMotions(CoinMotionKind.Spend);
            PlayGoldGainHud(data.Delta, data.BalanceAfter, data.HasWorldOrigin);
            if (data.HasWorldOrigin)
                PlayGoldGain(data.WorldOrigin, data.Delta);
        }
        else if (data.Delta < 0)
        {
            hasPendingGoldSync = false;
            RecycleCoinMotions(CoinMotionKind.Gain);
            RecycleCoinMotions(CoinMotionKind.Spend);
            StopGoldGainHudAnimation(data.BalanceAfter, true);
            PlayGoldSpend(SafeMagnitude(data.Delta));
        }
    }

    private void HandleGoldChanged(int balance)
    {
        try
        {
            if (goldCounterTween != null && balance > goldCounterTargetBalance)
            {
                // 같은 프레임에 이어질 양수 피드백이 현재 표시값에서 자연스럽게 이어받도록
                // 실제 숫자 동기화를 LateUpdate까지 한 프레임 유예한다.
                hasPendingGoldSync = true;
                pendingGoldBalance = balance;
                if (coinText != null)
                    coinText.text = FormatGoldAmount(displayedGold);
                return;
            }

            hasPendingGoldSync = false;
            StopGoldGainHudAnimation(balance, true);
        }
        catch (Exception exception)
        {
            // 표시 연출 실패가 실제 골드 증감 흐름을 중단시키지 않게 한다.
            Debug.LogException(exception);
        }
    }

    private void HandleBreedCountChanged(int count)
    {
        try
        {
            StopBreedCountHudAnimation(count, true);
        }
        catch (Exception exception)
        {
            // 교배 로직은 정상 진행시키고 장식용 HUD 오류만 기록한다.
            Debug.LogException(exception);
        }
    }

    private void HandleBreedCountFeedback(BreedCountFeedbackData data)
    {
        if (data.IncrementAmount <= 0)
            return;

        PlayBreedCountGainHud(data);
    }

    private void PlayGoldGainHud(int delta, int balanceAfter, bool hasWorldOrigin)
    {
        int previousDisplayed = goldCounterTween != null
            ? displayedGold
            : SafeSubtract(balanceAfter, delta);

        StopGoldGainHudAnimation(balanceAfter, false);
        goldCounterTargetBalance = balanceAfter;
        displayedGold = previousDisplayed;
        goldHudAnimationVersion++;
        int version = goldHudAnimationVersion;
        float delay = hasWorldOrigin ? WorldGainHudDelay : 0.05f;

        if (coinText != null)
        {
            coinText.text = FormatGoldAmount(displayedGold);
            Tween counterTween = DOTween.To(
                    () => displayedGold,
                    value =>
                    {
                        if (version != goldHudAnimationVersion || coinText == null)
                            return;

                        displayedGold = value;
                        coinText.text = FormatGoldAmount(value);
                    },
                    balanceAfter,
                    HudCountDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);

            counterTween.OnComplete(() =>
            {
                if (version != goldHudAnimationVersion || coinText == null)
                    return;

                displayedGold = balanceAfter;
                coinText.text = FormatGoldAmount(balanceAfter);
            });
            counterTween.OnKill(() =>
            {
                if (goldCounterTween == counterTween)
                    goldCounterTween = null;
            });
            goldCounterTween = counterTween;
        }

        if (coinTextTarget == null && goldTarget == null)
            return;

        Sequence bounceSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .AppendInterval(delay);

        AppendGoldHudBounce(bounceSequence, 1.10f, 1.08f);
        bounceSequence.AppendInterval(0.07f);
        AppendGoldHudBounce(bounceSequence, 1.15f, 1.11f);
        bounceSequence.AppendInterval(0.08f);
        AppendGoldHudBounce(bounceSequence, 1.20f, 1.14f);
        bounceSequence.OnComplete(ResetGoldHudScales);
        bounceSequence.OnKill(() =>
        {
            if (goldGainBounceSequence == bounceSequence)
                goldGainBounceSequence = null;
        });
        goldGainBounceSequence = bounceSequence;
    }

    private void PlayBreedCountGainHud(BreedCountFeedbackData data)
    {
        int previousDisplayed;
        int targetCount;
        if (data.CounterWasActive)
        {
            previousDisplayed = breedCounterTween != null
                ? displayedBreedCount
                : data.PreviousRemainingCount;
            targetCount = data.CurrentRemainingCount;
        }
        else
        {
            previousDisplayed = breedCounterTween != null
                ? displayedBreedCount
                : ParseBreedCount(breedCountText != null ? breedCountText.text : null);
            int accumulatedTarget = breedCounterTween != null
                ? breedCounterTargetCount
                : previousDisplayed;
            targetCount = SafeAddNonNegative(accumulatedTarget, data.IncrementAmount);
        }

        StopBreedCountHudAnimation(targetCount, false);
        breedCounterTargetCount = targetCount;
        displayedBreedCount = previousDisplayed;
        breedHudAnimationVersion++;
        int version = breedHudAnimationVersion;
        const float countDelay = 0.25f;
        const float firstArrivalDelay = 0.40f;

        if (breedCountText != null)
        {
            breedCountText.text = FormatBreedCount(displayedBreedCount);
            Tween counterTween = DOTween.To(
                    () => displayedBreedCount,
                    value =>
                    {
                        if (version != breedHudAnimationVersion || breedCountText == null)
                            return;

                        displayedBreedCount = value;
                        breedCountText.text = FormatBreedCount(value);
                    },
                    targetCount,
                    BreedCountDuration)
                .SetDelay(countDelay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);

            counterTween.OnComplete(() =>
            {
                if (version != breedHudAnimationVersion || breedCountText == null)
                    return;

                displayedBreedCount = targetCount;
                breedCountText.text = FormatBreedCount(targetCount);
            });
            counterTween.OnKill(() =>
            {
                if (breedCounterTween == counterTween)
                    breedCounterTween = null;
            });
            breedCounterTween = counterTween;
        }

        if (breedCountTextTarget == null && breedIconTarget == null)
            return;

        PlayBreedSproutAbsorb();

        Sequence bounceSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .AppendInterval(firstArrivalDelay)
            .AppendCallback(PlayBreedHudAccent);

        AppendBreedHudBounce(bounceSequence, 1.10f, 1.08f);
        bounceSequence.AppendInterval(0.07f);
        AppendBreedHudBounce(bounceSequence, 1.15f, 1.11f);
        bounceSequence.AppendInterval(0.08f);
        AppendBreedHudBounce(bounceSequence, 1.20f, 1.14f);
        bounceSequence.OnComplete(ResetBreedHudScales);
        bounceSequence.OnKill(() =>
        {
            if (breedGainBounceSequence == bounceSequence)
                breedGainBounceSequence = null;
        });
        breedGainBounceSequence = bounceSequence;
    }

    private void AppendGoldHudBounce(
        Sequence sequence,
        float textScaleMultiplier,
        float iconScaleMultiplier)
    {
        sequence.AppendCallback(PlayGoldHudAccent);
        AppendHudScaleStep(
            sequence,
            coinTextTarget,
            goldTarget,
            coinTextBaseScale * textScaleMultiplier,
            goldTargetBaseScale * iconScaleMultiplier,
            0.085f,
            Ease.OutBack);
        AppendHudScaleStep(
            sequence,
            coinTextTarget,
            goldTarget,
            coinTextBaseScale,
            goldTargetBaseScale,
            0.075f,
            Ease.InOutSine);
    }

    private void AppendBreedHudBounce(
        Sequence sequence,
        float textScaleMultiplier,
        float iconScaleMultiplier)
    {
        AppendHudScaleStep(
            sequence,
            breedCountTextTarget,
            breedIconTarget,
            breedCountTextBaseScale * textScaleMultiplier,
            breedIconBaseScale * iconScaleMultiplier,
            0.085f,
            Ease.OutBack);
        AppendHudScaleStep(
            sequence,
            breedCountTextTarget,
            breedIconTarget,
            breedCountTextBaseScale,
            breedIconBaseScale,
            0.075f,
            Ease.InOutSine);
    }

    private void AppendHudScaleStep(
        Sequence sequence,
        RectTransform textTarget,
        RectTransform iconTarget,
        Vector3 textScale,
        Vector3 iconScale,
        float duration,
        Ease ease)
    {
        bool appended = false;
        bool textIsInsideIconTarget = textTarget != null
            && iconTarget != null
            && textTarget != iconTarget
            && textTarget.IsChildOf(iconTarget);
        bool iconTargetIsInsideText = textTarget != null
            && iconTarget != null
            && textTarget != iconTarget
            && iconTarget.IsChildOf(textTarget);

        // 대상이 부모/자식 관계라면 바깥쪽 Transform 하나만 움직여
        // 상속된 스케일이 두 번 곱해지지 않게 한다.
        if (textTarget != null && !textIsInsideIconTarget)
        {
            sequence.Append(textTarget.DOScale(textScale, duration).SetEase(ease));
            appended = true;
        }

        if (iconTarget != null
            && iconTarget != textTarget
            && !iconTargetIsInsideText)
        {
            Tween iconTween = iconTarget.DOScale(iconScale, duration).SetEase(ease);
            if (appended)
                sequence.Join(iconTween);
            else
            {
                sequence.Append(iconTween);
                appended = true;
            }
        }

        if (!appended)
            sequence.AppendInterval(duration);
    }

    private void HandlePlantValueFeedback(PlantValueFeedbackData data)
    {
        int delta = data.Delta;
        if (delta <= 0
            || !TryWorldToCanvas(
                data.WorldPosition + GetPlantValueStartWorldOffset(),
                out Vector2 startPosition))
        {
            return;
        }

        Vector2 endOffset = GetPlantValueEndUiOffset();

        if (activePlantPopups.TryGetValue(data.PlantInstanceId, out AmountPopupFx existing)
            && existing != null
            && existing.Active)
        {
            existing.AccumulatedAmount += delta;
            StartAmountPopup(existing, startPosition, true, true, endOffset);
            return;
        }

        AmountPopupFx popup = AcquirePopup();
        popup.TracksPlant = true;
        popup.PlantInstanceId = data.PlantInstanceId;
        popup.AccumulatedAmount = delta;
        activePlantPopups[data.PlantInstanceId] = popup;
        StartAmountPopup(popup, startPosition, true, true, endOffset);
    }

    private void PlayGoldGain(Vector3 worldOrigin, int amount)
    {
        if (!TryWorldToCanvas(worldOrigin + SaleWorldOffset, out Vector2 startPosition))
            return;

        if (!TryGetGoldTargetPosition(out Vector2 targetPosition) || coinSprite == null)
        {
            AmountPopupFx fallback = AcquirePopup();
            fallback.AccumulatedAmount = amount;
            StartAmountPopup(fallback, startPosition, true, false);
            return;
        }

        int count = GetCoinCount(amount);
        for (int i = 0; i < count; i++)
        {
            CoinFx coin = AcquireCoin(CoinMotionKind.Gain);
            coin.Root.anchoredPosition = startPosition;
            coin.Root.localScale = Vector3.one * UnityEngine.Random.Range(0.82f, 1.08f);
            coin.Root.localRotation = Quaternion.identity;
            coin.Image.color = Color.white;

            Vector2 scatter = startPosition + new Vector2(
                UnityEngine.Random.Range(-46f, 46f),
                UnityEngine.Random.Range(-24f, 46f));

            float scatterDuration = UnityEngine.Random.Range(0.13f, 0.2f);
            float stagger = i * 0.045f;
            float flightDuration = UnityEngine.Random.Range(0.48f, 0.68f);
            Vector2 controlOne = scatter + new Vector2(
                UnityEngine.Random.Range(-55f, 55f),
                UnityEngine.Random.Range(45f, 105f));
            Vector2 controlTwo = Vector2.Lerp(scatter, targetPosition, 0.68f)
                + new Vector2(UnityEngine.Random.Range(-55f, 55f), UnityEngine.Random.Range(10f, 65f));

            int version = coin.Version;
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject)
                .Append(coin.Root.DOAnchorPos(scatter, scatterDuration).SetEase(Ease.OutQuad))
                .AppendInterval(stagger)
                .Append(CreateBezierTween(coin.Root, scatter, controlOne, controlTwo, targetPosition, flightDuration)
                    .SetEase(Ease.InCubic))
                .Join(coin.Root.DOScale(0.45f, flightDuration).SetEase(Ease.InQuad));

            float fadeStart = scatterDuration + stagger + flightDuration * 0.68f;
            sequence.Insert(fadeStart, coin.Image.DOFade(0f, flightDuration * 0.32f));
            sequence.OnComplete(() => CompleteCoin(coin, version));
            coin.Sequence = sequence;
        }
    }

    private void PlayGoldSpend(int amount)
    {
        if (!TryGetGoldTargetPosition(out Vector2 targetPosition))
            return;

        if (coinSprite != null)
        {
            int count = GetSpendCoinCount(amount);
            for (int i = 0; i < count; i++)
            {
                CoinFx coin = AcquireCoin(CoinMotionKind.Spend);
                float arcProgress = count <= 1 ? 0.5f : i / (count - 1f);
                float angle = Mathf.Lerp(Mathf.PI * 1.08f, Mathf.PI * 1.92f, arcProgress)
                    + UnityEngine.Random.Range(-0.08f, 0.08f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                // 모든 소비 코인은 실제 HUD 코인 아이콘 중심에서 출발한다.
                // 화면 위쪽으로 날아가는 별도 코인은 만들지 않고, 화면 안쪽인 아래 부채꼴로
                // 서로 다른 방향과 거리만큼 멀어지며 흩어진다.
                Vector2 startPosition = targetPosition;
                Vector2 endPosition = ClampToEffectsRoot(
                    targetPosition + direction * UnityEngine.Random.Range(42f, 62f),
                    16f);

                coin.Root.anchoredPosition = startPosition;
                coin.Root.localScale = Vector3.one * UnityEngine.Random.Range(0.24f, 0.32f);
                coin.Root.localRotation = Quaternion.identity;
                coin.Image.color = SpendCoinColor;

                float duration = UnityEngine.Random.Range(0.38f, 0.50f);
                float delay = i * 0.018f;
                int version = coin.Version;

                Sequence sequence = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .AppendInterval(delay)
                    .Append(coin.Root.DOAnchorPos(endPosition, duration).SetEase(Ease.OutCubic))
                    .Join(coin.Root.DOScale(0.68f, duration * 0.26f).SetEase(Ease.OutBack))
                    .Join(coin.Root.DOLocalRotate(
                        new Vector3(0f, 0f, UnityEngine.Random.Range(-220f, 220f)),
                        duration,
                        RotateMode.FastBeyond360));

                sequence.Insert(
                    delay + duration * 0.40f,
                    coin.Root.DOScale(0.16f, duration * 0.60f).SetEase(Ease.InQuad));
                sequence.Insert(
                    delay + duration * 0.36f,
                    coin.Image.DOFade(0f, duration * 0.64f).SetEase(Ease.InQuad));
                sequence.OnComplete(() => CompleteCoin(coin, version));

                coin.Sequence = sequence;
            }
        }

        AmountPopupFx popup = AcquirePopup();
        popup.AccumulatedAmount = amount;
        // HUD가 화면 최상단에 있으므로 아래에서 시작해 화면 안쪽으로 상승시킨다.
        StartAmountPopup(popup, targetPosition + new Vector2(0f, -50f), false, false);
        PlayGoldSpendHud();
    }

    private void PlayBreedSproutAbsorb()
    {
        if (breedSprite == null
            || !TryGetHudTargetPosition(breedIconTarget, out Vector2 targetPosition))
        {
            return;
        }

        const int count = 3;
        float angleOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < count; i++)
        {
            SproutFx sprout = AcquireSprout();
            float angle = angleOffset
                + Mathf.PI * 2f * i / count
                + UnityEngine.Random.Range(-0.22f, 0.22f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 startPosition = ClampToEffectsRoot(
                targetPosition + direction * UnityEngine.Random.Range(30f, 50f),
                14f);
            Vector2 revealPosition = ClampToEffectsRoot(
                startPosition + direction * UnityEngine.Random.Range(3f, 8f),
                14f);
            Vector2 endPosition = targetPosition + UnityEngine.Random.insideUnitCircle * 2f;

            sprout.Root.anchoredPosition = startPosition;
            sprout.Root.localScale = Vector3.one * UnityEngine.Random.Range(0.52f, 0.68f);
            sprout.Root.localRotation = Quaternion.Euler(
                0f,
                0f,
                UnityEngine.Random.Range(-14f, 14f));
            sprout.Image.color = new Color(1f, 1f, 1f, 0f);

            float delay = i * 0.055f;
            float revealDuration = UnityEngine.Random.Range(0.11f, 0.14f);
            float flightDuration = UnityEngine.Random.Range(0.32f, 0.42f);
            Vector2 controlOne = revealPosition
                + Vector2.up * UnityEngine.Random.Range(8f, 18f)
                + UnityEngine.Random.insideUnitCircle * 6f;
            Vector2 controlTwo = Vector2.Lerp(revealPosition, endPosition, 0.66f)
                + UnityEngine.Random.insideUnitCircle * 10f;
            int version = sprout.Version;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject)
                .AppendInterval(delay)
                .Append(sprout.Image.DOFade(1f, revealDuration).SetEase(Ease.OutQuad))
                .Join(sprout.Root.DOAnchorPos(revealPosition, revealDuration).SetEase(Ease.OutSine))
                .Join(sprout.Root.DOScale(0.92f, revealDuration).SetEase(Ease.OutBack))
                .Append(CreateBezierTween(
                        sprout.Root,
                        revealPosition,
                        controlOne,
                        controlTwo,
                        endPosition,
                        flightDuration)
                    .SetEase(Ease.InCubic))
                .Join(sprout.Root.DOScale(0.24f, flightDuration).SetEase(Ease.InQuad))
                .Join(sprout.Image.DOFade(0f, flightDuration * 0.28f)
                    .SetDelay(flightDuration * 0.68f))
                .OnComplete(() => CompleteSprout(sprout, version));

            sprout.Sequence = sequence;
        }
    }

    private Tween CreateBezierTween(
        RectTransform target,
        Vector2 start,
        Vector2 controlOne,
        Vector2 controlTwo,
        Vector2 end,
        float duration)
    {
        float progress = 0f;
        return DOTween.To(
            () => progress,
            value =>
            {
                progress = value;
                float inverse = 1f - value;
                target.anchoredPosition =
                    inverse * inverse * inverse * start
                    + 3f * inverse * inverse * value * controlOne
                    + 3f * inverse * value * value * controlTwo
                    + value * value * value * end;
            },
            1f,
            duration);
    }

    private void StartAmountPopup(
        AmountPopupFx popup,
        Vector2 startPosition,
        bool positive,
        bool trackPlant,
        Vector2? movementOffset = null)
    {
        popup.Version++;
        int version = popup.Version;
        popup.Sequence?.Kill(false);
        popup.Sequence = null;
        popup.TracksPlant = trackPlant;
        popup.Root.gameObject.SetActive(true);
        popup.Root.SetAsLastSibling();
        popup.Root.localScale = Vector3.one * 0.82f;
        popup.Root.localRotation = Quaternion.identity;
        popup.Group.alpha = 0f;
        popup.Group.blocksRaycasts = false;
        popup.Icon.color = Color.white;
        ApplyCoinSprite(popup.Icon);
        ApplyAmountTextStyle(popup.Text, positive);

        // 차감 팝업은 위로 떠오르는 빨간 금액만 보여준다.
        // 코인 아이콘은 주변으로 흩어지는 spend 파티클과 중복되므로 양수 팝업에만 사용한다.
        popup.Icon.gameObject.SetActive(positive);
        popup.TextRect.offsetMin = new Vector2(positive ? 36f : 0f, 0f);
        popup.TextRect.offsetMax = Vector2.zero;
        popup.Text.alignment = positive
            ? TextAlignmentOptions.MidlineLeft
            : TextAlignmentOptions.Center;
        long shownAmount = Math.Abs(popup.AccumulatedAmount);
        popup.Text.text = positive ? $"+{shownAmount:N0}" : $"-{shownAmount:N0}";
        ResizePopupToText(popup, positive);

        Vector2 resolvedMovementOffset = movementOffset
            ?? Vector2.up * (positive ? 64f : 34f);
        float moveDuration = positive ? 0.72f : 0.58f;
        Vector2 safeStartPosition = ClampPopupToEffectsRoot(popup.Root, startPosition, 4f);
        Vector2 safeEndPosition = ClampPopupToEffectsRoot(
            popup.Root,
            safeStartPosition + resolvedMovementOffset,
            4f);
        popup.Root.anchoredPosition = safeStartPosition;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .Append(popup.Group.DOFade(1f, 0.12f).SetEase(Ease.OutQuad))
            .Join(popup.Root.DOScale(1f, 0.18f).SetEase(Ease.OutBack))
            .Join(popup.Root.DOAnchorPos(safeEndPosition, moveDuration).SetEase(Ease.OutCubic))
            .AppendInterval(0.12f)
            .Append(popup.Group.DOFade(0f, 0.24f).SetEase(Ease.InQuad))
            .OnComplete(() => CompletePopup(popup, version));

        popup.Sequence = sequence;
    }

    private void ResizePopupToText(AmountPopupFx popup, bool positive)
    {
        Vector2 preferredSize = popup.Text.GetPreferredValues(popup.Text.text);
        float leftContentWidth = positive ? 36f : 0f;
        float desiredWidth = Mathf.Max(170f, leftContentWidth + preferredSize.x + 12f);
        float desiredHeight = Mathf.Max(42f, preferredSize.y + 10f);

        if (effectsRoot != null)
        {
            Rect effectsRect = effectsRoot.rect;
            if (effectsRect.width > 8f)
                desiredWidth = Mathf.Min(desiredWidth, effectsRect.width - 8f);
            if (effectsRect.height > 8f)
                desiredHeight = Mathf.Min(desiredHeight, effectsRect.height - 8f);
        }

        popup.Root.sizeDelta = new Vector2(desiredWidth, desiredHeight);
    }

    private void PlayGoldHudAccent()
    {
        PlayHudAccent(goldTarget, HudGlowColor, HudFeedbackOwner.Gold);
    }

    private void PlayBreedHudAccent()
    {
        PlayHudAccent(breedIconTarget, BreedGlowColor, HudFeedbackOwner.Breed);
    }

    private void PlayHudAccent(
        RectTransform target,
        Color glowColor,
        HudFeedbackOwner owner)
    {
        if (!TryGetHudTargetPosition(target, out Vector2 targetPosition))
            return;

        PlayHudGlow(targetPosition, glowColor, owner);
        PlayHudPlus(targetPosition, owner);
    }

    private void PlayHudGlow(Vector2 targetPosition, Color color, HudFeedbackOwner owner)
    {
        GlowFx glow = AcquireGlow(owner);
        glow.Root.anchoredPosition = targetPosition;
        glow.Root.localScale = Vector3.one * 0.72f;
        glow.Root.localRotation = Quaternion.identity;
        glow.Image.color = color;

        int version = glow.Version;
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .Append(glow.Root.DOScale(1.30f, 0.28f).SetEase(Ease.OutQuad))
            .Join(glow.Image.DOFade(0f, 0.30f).SetEase(Ease.OutSine))
            .OnComplete(() => CompleteGlow(glow, version));

        glow.Sequence = sequence;
    }

    private void PlayHudPlus(Vector2 targetPosition, HudFeedbackOwner owner)
    {
        PlusFx plus = AcquirePlus(owner);
        Vector2 startPosition = targetPosition + new Vector2(
            UnityEngine.Random.Range(-9f, 9f),
            UnityEngine.Random.Range(9f, 15f));
        Vector2 endPosition = startPosition + new Vector2(
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(18f, 27f));
        startPosition = ClampToEffectsRoot(startPosition, 8f);
        endPosition = ClampToEffectsRoot(endPosition, 8f);

        plus.Root.anchoredPosition = startPosition;
        plus.Root.localScale = Vector3.one * UnityEngine.Random.Range(0.72f, 0.84f);
        plus.Root.localRotation = Quaternion.identity;
        plus.Group.alpha = 1f;
        Color plusColor = GetHudPlusColor();
        plus.Horizontal.color = plusColor;
        plus.Vertical.color = plusColor;

        int version = plus.Version;
        float duration = UnityEngine.Random.Range(0.34f, 0.42f);
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject)
            .Append(plus.Root.DOAnchorPos(endPosition, duration).SetEase(Ease.OutCubic))
            .Join(plus.Root.DOScale(0.96f, duration * 0.46f).SetEase(Ease.OutBack))
            .Join(plus.Group.DOFade(0f, duration * 0.68f).SetDelay(duration * 0.32f))
            .OnComplete(() => CompletePlus(plus, version));

        plus.Sequence = sequence;
    }

    private Vector2 ClampToEffectsRoot(Vector2 position, float padding)
    {
        if (effectsRoot == null)
            return position;

        Rect rect = effectsRoot.rect;
        return new Vector2(
            Mathf.Clamp(position.x, rect.xMin + padding, rect.xMax - padding),
            Mathf.Clamp(position.y, rect.yMin + padding, rect.yMax - padding));
    }

    private Vector2 ClampPopupToEffectsRoot(
        RectTransform popupRoot,
        Vector2 position,
        float padding)
    {
        if (effectsRoot == null || popupRoot == null)
            return position;

        Rect effectsRect = effectsRoot.rect;
        Vector2 popupSize = popupRoot.rect.size;
        float halfWidth = popupSize.x * 0.5f;
        float halfHeight = popupSize.y * 0.5f;
        return new Vector2(
            Mathf.Clamp(
                position.x,
                effectsRect.xMin + halfWidth + padding,
                effectsRect.xMax - halfWidth - padding),
            Mathf.Clamp(
                position.y,
                effectsRect.yMin + halfHeight + padding,
                effectsRect.yMax - halfHeight - padding));
    }

    private CoinFx AcquireCoin(CoinMotionKind motionKind)
    {
        CoinFx fx = coinPool.Count > 0 ? coinPool.Dequeue() : CreateCoinFx();
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Active = true;
        fx.Version++;
        fx.MotionKind = motionKind;
        fx.Root.gameObject.SetActive(true);
        fx.Root.SetAsLastSibling();
        fx.Image.color = Color.white;
        ApplyCoinSprite(fx.Image);
        return fx;
    }

    private SproutFx AcquireSprout()
    {
        SproutFx fx = sproutPool.Count > 0 ? sproutPool.Dequeue() : CreateSproutFx();
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Active = true;
        fx.Version++;
        fx.Root.gameObject.SetActive(true);
        fx.Root.SetAsLastSibling();
        ApplyBreedSprite(fx.Image);
        return fx;
    }

    private AmountPopupFx AcquirePopup()
    {
        AmountPopupFx fx = popupPool.Count > 0 ? popupPool.Dequeue() : CreatePopupFx();
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Active = true;
        fx.Version++;
        fx.TracksPlant = false;
        fx.PlantInstanceId = 0;
        fx.AccumulatedAmount = 0L;
        fx.Root.gameObject.SetActive(true);
        fx.Root.SetAsLastSibling();
        fx.Group.alpha = 1f;
        fx.Group.blocksRaycasts = false;
        return fx;
    }

    private GlowFx AcquireGlow(HudFeedbackOwner owner)
    {
        GlowFx fx = glowPool.Count > 0 ? glowPool.Dequeue() : CreateGlowFx();
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Active = true;
        fx.Version++;
        fx.Owner = owner;
        fx.Root.gameObject.SetActive(true);
        fx.Root.SetAsLastSibling();
        fx.Image.sprite = glowSprite;
        fx.Image.raycastTarget = false;
        return fx;
    }

    private PlusFx AcquirePlus(HudFeedbackOwner owner)
    {
        PlusFx fx = plusPool.Count > 0 ? plusPool.Dequeue() : CreatePlusFx();
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Active = true;
        fx.Version++;
        fx.Owner = owner;
        fx.Root.gameObject.SetActive(true);
        fx.Root.SetAsLastSibling();
        fx.Group.alpha = 1f;
        Color plusColor = GetHudPlusColor();
        fx.Horizontal.color = plusColor;
        fx.Vertical.color = plusColor;
        return fx;
    }

    private void CompleteCoin(CoinFx fx, int version)
    {
        if (fx == null || !fx.Active || fx.Version != version)
            return;

        fx.Sequence = null;
        ReleaseCoin(fx);
    }

    private void CompleteSprout(SproutFx fx, int version)
    {
        if (fx == null || !fx.Active || fx.Version != version)
            return;

        fx.Sequence = null;
        ReleaseSprout(fx);
    }

    private void CompletePopup(AmountPopupFx fx, int version)
    {
        if (fx == null || !fx.Active || fx.Version != version)
            return;

        fx.Sequence = null;
        ReleasePopup(fx);
    }

    private void CompleteGlow(GlowFx fx, int version)
    {
        if (fx == null || !fx.Active || fx.Version != version)
            return;

        fx.Sequence = null;
        ReleaseGlow(fx);
    }

    private void CompletePlus(PlusFx fx, int version)
    {
        if (fx == null || !fx.Active || fx.Version != version)
            return;

        fx.Sequence = null;
        ReleasePlus(fx);
    }

    private void ReleaseCoin(CoinFx fx)
    {
        if (fx == null || !fx.Active)
            return;

        fx.Active = false;
        fx.MotionKind = CoinMotionKind.None;
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Root.anchoredPosition = Vector2.zero;
        fx.Root.localScale = Vector3.one;
        fx.Root.localRotation = Quaternion.identity;
        fx.Image.color = Color.white;
        fx.Root.gameObject.SetActive(false);
        coinPool.Enqueue(fx);
    }

    private void ReleaseSprout(SproutFx fx)
    {
        if (fx == null || !fx.Active)
            return;

        fx.Active = false;
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Root.anchoredPosition = Vector2.zero;
        fx.Root.localScale = Vector3.one;
        fx.Root.localRotation = Quaternion.identity;
        fx.Image.color = Color.white;
        fx.Root.gameObject.SetActive(false);
        sproutPool.Enqueue(fx);
    }

    private void ReleasePopup(AmountPopupFx fx)
    {
        if (fx == null || !fx.Active)
            return;

        if (fx.TracksPlant
            && activePlantPopups.TryGetValue(fx.PlantInstanceId, out AmountPopupFx tracked)
            && tracked == fx)
        {
            activePlantPopups.Remove(fx.PlantInstanceId);
        }

        fx.Active = false;
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.TracksPlant = false;
        fx.PlantInstanceId = 0;
        fx.AccumulatedAmount = 0L;
        fx.Group.alpha = 0f;
        fx.Group.blocksRaycasts = false;
        fx.Icon.gameObject.SetActive(false);
        fx.Root.anchoredPosition = Vector2.zero;
        fx.Root.localScale = Vector3.one;
        fx.Root.localRotation = Quaternion.identity;
        fx.Root.gameObject.SetActive(false);
        popupPool.Enqueue(fx);
    }

    private void ReleaseGlow(GlowFx fx)
    {
        if (fx == null || !fx.Active)
            return;

        fx.Active = false;
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Root.anchoredPosition = Vector2.zero;
        fx.Root.localScale = Vector3.one;
        fx.Root.localRotation = Quaternion.identity;
        fx.Image.color = Color.clear;
        fx.Root.gameObject.SetActive(false);
        glowPool.Enqueue(fx);
    }

    private void ReleasePlus(PlusFx fx)
    {
        if (fx == null || !fx.Active)
            return;

        fx.Active = false;
        fx.Sequence?.Kill(false);
        fx.Sequence = null;
        fx.Root.anchoredPosition = Vector2.zero;
        fx.Root.localScale = Vector3.one;
        fx.Root.localRotation = Quaternion.identity;
        fx.Group.alpha = 0f;
        Color plusColor = GetHudPlusColor();
        fx.Horizontal.color = plusColor;
        fx.Vertical.color = plusColor;
        fx.Root.gameObject.SetActive(false);
        plusPool.Enqueue(fx);
    }

    private void StopGoldGainHudAnimation(int finalBalance, bool updateText)
    {
        hasPendingGoldSync = false;
        goldHudAnimationVersion++;
        goldCounterTargetBalance = finalBalance;

        goldCounterTween?.Kill(false);
        goldCounterTween = null;
        goldGainBounceSequence?.Kill(false);
        goldGainBounceSequence = null;
        goldPunchTween?.Kill(false);
        goldPunchTween = null;

        displayedGold = finalBalance;
        if (updateText && coinText != null)
            coinText.text = FormatGoldAmount(finalBalance);

        ResetGoldHudScales();
        RecycleHudAccents(HudFeedbackOwner.Gold);
    }

    private void StopBreedCountHudAnimation(int finalCount, bool updateText)
    {
        breedHudAnimationVersion++;
        breedCounterTargetCount = finalCount;

        breedCounterTween?.Kill(false);
        breedCounterTween = null;
        breedGainBounceSequence?.Kill(false);
        breedGainBounceSequence = null;

        displayedBreedCount = finalCount;
        if (updateText && breedCountText != null)
            breedCountText.text = FormatBreedCount(finalCount);

        ResetBreedHudScales();
        RecycleHudAccents(HudFeedbackOwner.Breed);
        RecycleBreedSprouts();
    }

    private void RecycleCoinMotions(CoinMotionKind motionKind)
    {
        for (int i = 0; i < allCoins.Count; i++)
        {
            if (allCoins[i].Active && allCoins[i].MotionKind == motionKind)
                ReleaseCoin(allCoins[i]);
        }
    }

    private void RecycleBreedSprouts()
    {
        for (int i = 0; i < allSprouts.Count; i++)
        {
            if (allSprouts[i].Active)
                ReleaseSprout(allSprouts[i]);
        }
    }

    private void RecycleHudAccents(HudFeedbackOwner owner)
    {
        for (int i = 0; i < allGlows.Count; i++)
        {
            if (allGlows[i].Active && allGlows[i].Owner == owner)
                ReleaseGlow(allGlows[i]);
        }

        for (int i = 0; i < allPluses.Count; i++)
        {
            if (allPluses[i].Active && allPluses[i].Owner == owner)
                ReleasePlus(allPluses[i]);
        }
    }

    private void ResetGoldHudScales()
    {
        if (coinTextTarget != null)
            coinTextTarget.localScale = coinTextBaseScale;
        if (goldTarget != null && goldTarget != coinTextTarget)
            goldTarget.localScale = goldTargetBaseScale;
        if (coinHudImage != null)
            coinHudImage.color = coinHudBaseColor;
    }

    private void ResetBreedHudScales()
    {
        if (breedCountTextTarget != null)
            breedCountTextTarget.localScale = breedCountTextBaseScale;
        if (breedIconTarget != null && breedIconTarget != breedCountTextTarget)
            breedIconTarget.localScale = breedIconBaseScale;
    }

    private void PlayGoldSpendHud()
    {
        if (goldTarget == null)
            return;

        goldPunchTween?.Kill(false);
        goldTarget.localScale = goldTargetBaseScale;
        if (coinHudImage != null)
            coinHudImage.color = coinHudBaseColor;

        if (TryGetGoldTargetPosition(out Vector2 targetPosition))
            PlayHudGlow(targetPosition, SpendGlowColor, HudFeedbackOwner.Gold);

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject);

        sequence.Append(goldTarget
            .DOScale(goldTargetBaseScale * 1.16f, 0.09f)
            .SetEase(Ease.OutBack));
        if (coinHudImage != null)
            sequence.Join(coinHudImage.DOColor(SpendIconFlashColor, 0.08f).SetEase(Ease.OutQuad));

        sequence.Append(goldTarget
            .DOScale(goldTargetBaseScale, 0.08f)
            .SetEase(Ease.InOutSine));
        if (coinHudImage != null)
            sequence.Join(coinHudImage.DOColor(coinHudBaseColor, 0.10f).SetEase(Ease.InOutSine));

        sequence.AppendInterval(0.035f);
        sequence.Append(goldTarget
            .DOScale(goldTargetBaseScale * 1.10f, 0.075f)
            .SetEase(Ease.OutBack));
        sequence.Append(goldTarget
            .DOScale(goldTargetBaseScale, 0.07f)
            .SetEase(Ease.InOutSine));
        sequence.OnComplete(ResetGoldHudScales);
        sequence.OnKill(() =>
        {
            if (goldPunchTween == sequence)
                goldPunchTween = null;
        });
        goldPunchTween = sequence;
    }

    private bool TryWorldToCanvas(Vector3 worldPosition, out Vector2 localPosition)
    {
        localPosition = Vector2.zero;
        if (effectsRoot == null)
            return false;

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return false;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectsRoot,
            screenPosition,
            null,
            out localPosition);
    }

    private bool TryGetGoldTargetPosition(out Vector2 localPosition)
    {
        return TryGetHudTargetPosition(goldTarget, out localPosition);
    }

    private bool TryGetHudTargetPosition(RectTransform target, out Vector2 localPosition)
    {
        localPosition = Vector2.zero;
        if (effectsRoot == null || target == null)
            return false;

        Canvas.ForceUpdateCanvases();
        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;
        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(targetCamera, targetWorldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectsRoot,
            screenPosition,
            null,
            out localPosition);
    }

    private void StopAndRecycleAll()
    {
        StopGoldGainHudAnimation(goldCounterTargetBalance, true);
        StopBreedCountHudAnimation(breedCounterTargetCount, true);

        for (int i = 0; i < allCoins.Count; i++)
        {
            if (allCoins[i].Active)
                ReleaseCoin(allCoins[i]);
        }

        for (int i = 0; i < allPopups.Count; i++)
        {
            if (allPopups[i].Active)
                ReleasePopup(allPopups[i]);
        }

        for (int i = 0; i < allGlows.Count; i++)
        {
            if (allGlows[i].Active)
                ReleaseGlow(allGlows[i]);
        }

        for (int i = 0; i < allPluses.Count; i++)
        {
            if (allPluses[i].Active)
                ReleasePlus(allPluses[i]);
        }

        for (int i = 0; i < allSprouts.Count; i++)
        {
            if (allSprouts[i].Active)
                ReleaseSprout(allSprouts[i]);
        }

        activePlantPopups.Clear();
    }

    private static int GetCoinCount(int amount)
    {
        int safeAmount = Mathf.Max(1, amount);
        return Mathf.Clamp(4 + Mathf.FloorToInt(Mathf.Log10(safeAmount)), 4, 8);
    }

    private static int GetSpendCoinCount(int amount)
    {
        int safeAmount = Mathf.Max(1, amount);
        return Mathf.Clamp(1 + Mathf.FloorToInt(Mathf.Log10(safeAmount)), 3, 4);
    }

    private static int SafeMagnitude(int value)
    {
        if (value == int.MinValue)
            return int.MaxValue;
        return Math.Abs(value);
    }

    private static int SafeSubtract(int value, int amount)
    {
        long result = (long)value - amount;
        return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, result));
    }

    private static int SafeAddNonNegative(int value, int amount)
    {
        long result = (long)value + amount;
        return (int)Math.Max(0L, Math.Min(int.MaxValue, result));
    }

    private static int ParseBreedCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        long value = 0L;
        bool foundDigit = false;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character < '0' || character > '9')
                continue;

            foundDigit = true;
            value = Math.Min(int.MaxValue, value * 10L + (character - '0'));
        }

        return foundDigit ? (int)value : 0;
    }

    private static string FormatGoldAmount(int value)
    {
        return EconomyManager.ToAbbreviatedString(value);
    }

    private static string FormatBreedCount(int value)
    {
        return $"{Mathf.Max(0, value)}회";
    }

    private sealed class CoinFx
    {
        public readonly RectTransform Root;
        public readonly Image Image;
        public Sequence Sequence;
        public bool Active;
        public int Version;
        public CoinMotionKind MotionKind;

        public CoinFx(RectTransform root, Image image)
        {
            Root = root;
            Image = image;
        }
    }

    private sealed class SproutFx
    {
        public readonly RectTransform Root;
        public readonly Image Image;
        public Sequence Sequence;
        public bool Active;
        public int Version;

        public SproutFx(RectTransform root, Image image)
        {
            Root = root;
            Image = image;
        }
    }

    private sealed class AmountPopupFx
    {
        public readonly RectTransform Root;
        public readonly CanvasGroup Group;
        public readonly Image Icon;
        public readonly RectTransform TextRect;
        public readonly TextMeshProUGUI Text;
        public Sequence Sequence;
        public bool Active;
        public bool TracksPlant;
        public int PlantInstanceId;
        public long AccumulatedAmount;
        public int Version;

        public AmountPopupFx(
            RectTransform root,
            CanvasGroup group,
            Image icon,
            RectTransform textRect,
            TextMeshProUGUI text)
        {
            Root = root;
            Group = group;
            Icon = icon;
            TextRect = textRect;
            Text = text;
        }
    }

    private sealed class GlowFx
    {
        public readonly RectTransform Root;
        public readonly Image Image;
        public Sequence Sequence;
        public bool Active;
        public int Version;
        public HudFeedbackOwner Owner;

        public GlowFx(RectTransform root, Image image)
        {
            Root = root;
            Image = image;
        }
    }

    private sealed class PlusFx
    {
        public readonly RectTransform Root;
        public readonly CanvasGroup Group;
        public readonly Image Horizontal;
        public readonly Image Vertical;
        public Sequence Sequence;
        public bool Active;
        public int Version;
        public HudFeedbackOwner Owner;

        public PlusFx(
            RectTransform root,
            CanvasGroup group,
            Image horizontal,
            Image vertical)
        {
            Root = root;
            Group = group;
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }
}
