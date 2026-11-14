using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class TutorialChapter : MonoBehaviour
{
    public const float DaySeconds = 30f;
    public const float PhoneSeconds = 15f;

    [SerializeField] private Chat chat;
    [SerializeField] private Shovel shovel;
    [SerializeField] private GameObject shovelBackground;
    [SerializeField] private GameObject breedRulePopup;
    [SerializeField] private GameObject tutorialBugPrefab;
    [SerializeField] private GameObject specialItemPrefab;
    [SerializeField, Min(0)] private int startingGold;
    [SerializeField] private Image highlightPrefab;
    [SerializeField] private TutorialGoalPopup goalPopupPrefab;

    protected GameManager game;
    protected bool phoneUnlocked;
    protected bool shovelUnlocked;
    protected bool taxUnlocked;
    protected bool shopUnlocked;
    protected bool questsUnlocked;
    protected bool bugsUnlocked;
    protected bool specialItemsUnlocked;

    private GameObject activeBreedRulePopup;
    private TutorialBug practiceBug;
    private GameObject specialItemRoot;
    private SpecialItemUIController specialItemUI;
    private ShopCanvasController shopCanvas;
    protected ShopCanvasController ShopCanvas => shopCanvas;
    private MessengerApp messenger;
    private TutorialHighlights highlights;
    private string highlightedMessage;
    private TutorialGoalPopup goalPopup;
    private string practiceTrigger;
    private string practiceSuffix;
    private PracticeGoal[] practiceGoals;
    private bool[] completedPracticeGoals;
    private bool practiceGateVisible;
    private bool practiceAdvanceUnlocked;
    private Action practiceCompleted;

    protected readonly struct PracticeGoal
    {
        internal readonly string key;
        internal readonly Func<bool> isComplete;
        internal readonly Action highlight;

        public PracticeGoal(string key, Func<bool> isComplete, Action highlight = null)
        {
            this.key = "tutorial.goals." + key;
            this.isComplete = isComplete;
            this.highlight = highlight;
        }
    }

    public abstract int ChapterNumber { get; }
    public abstract int FinalStage { get; }
    protected abstract string[] RequiredMessages { get; }
    protected virtual IEnumerable<string> PracticeTriggers => Array.Empty<string>();
    public virtual bool IsUntimedDay => false;
    public virtual bool CanBreedDuringOpeningMessage => false;
    public virtual bool CanSkipDay => game != null;
    public virtual bool WaitingForFirstTax => false;
    public bool CanOpenPhone => phoneUnlocked;
    private bool CanUseDuringPractice(string suffix) => game != null &&
        (!game.GetGameIsStopped() || (practiceSuffix == suffix && practiceGateVisible));
    public bool CanUseShovel => shovelUnlocked && CanUseDuringPractice("day04.sale");
    public bool CanUseShop => shopUnlocked && CanUseDuringPractice("day03.shop");
    public bool CanUseQuests => questsUnlocked && CanUseDuringPractice("day16.quests");
    public bool CanSpawnBugs => bugsUnlocked;
    public bool CanUseSpecialItems => specialItemsUnlocked && CanUseDuringPractice("day10.special");
    internal bool ShowWavePreviewDuringMessage => practiceSuffix == "day06.forecast" ||
        practiceSuffix == "day21.season";
    internal bool IsDayForecastPractice => practiceSuffix == "day21.season";

    public bool CanPayTax(int dueStage) => taxUnlocked && game != null &&
        CanUseDuringPractice("day05.tax") && dueStage <= FinalStage && dueStage != 40;

    public bool CanOpenApp(int index) =>
        index == PhoneManager.MENU_INDEX_HOME ||
        (phoneUnlocked && index == PhoneManager.MENU_INDEX_MESSENGER) ||
        (taxUnlocked && index == PhoneManager.MENU_INDEX_TAX) ||
        (shopUnlocked && index == PhoneManager.MENU_INDEX_SHOP) ||
        (questsUnlocked && index == PhoneManager.MENU_INDEX_QUEST);

    protected virtual void OnDisable()
    {
        if (messenger != null)
        {
            messenger.OnMandatoryMessageShown -= OnMessageShown;
            messenger.OnMandatoryMessagesClosed -= OnMessagesClosed;
        }
        highlightedMessage = null;
        ClearPractice();
        highlights?.Dispose();
        highlights = null;
        CloseBreedRulePopup();
        if (practiceBug != null) Destroy(practiceBug.gameObject);
        if (specialItemRoot != null) Destroy(specialItemRoot);
    }

    public IEnumerator Run(GameManager manager)
    {
        game = manager;
        phoneUnlocked = ChapterNumber > 1;
        taxUnlocked = ChapterNumber > 1;
        shopUnlocked = ChapterNumber == 3;
        questsUnlocked = ChapterNumber == 3;
        bugsUnlocked = ChapterNumber == 3;
        game.phoneManager.isTutorial = true;
        SetShovelUnlocked(ChapterNumber > 1);
        RefreshPhoneAccess();

        // Scene and profile managers finish Start before any run state is initialized.
        yield return null;
#if UNITY_EDITOR
        if (breedRulePopup == null)
            breedRulePopup = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resource/Sprites/UI/MessagePopup/Popup_BreedRule 1.prefab");
#endif
        messenger = game.phoneManager.messengerApp;
        if (UIManager.Instance?.PopupCanvasParent == null || TaxManager.Instance == null ||
            SaveManager.Instance == null || SceneLoader.Instance == null || messenger == null || goalPopupPrefab == null ||
            (ChapterNumber == 1 && breedRulePopup == null) ||
            (ChapterNumber == 2 && tutorialBugPrefab == null) ||
            (ChapterNumber == 3 && specialItemPrefab == null))
        {
            Debug.LogError($"Tutorial chapter {ChapterNumber}: missing scene managers or tutorial prefabs.", this);
            yield break;
        }

        var required = new List<string>(RequiredMessages);
        required.AddRange(new[] { "complete.awarded", "complete.replay", "complete.save_failed" });
        if (!messenger.ConfigureTutorialChat(chat, required.ConvertAll(Trigger),
            new List<string>(PracticeTriggers).ConvertAll(Trigger)))
        {
            Debug.LogError($"Tutorial chapter {ChapterNumber}: invalid mandatory messages or popup bindings.", this);
            yield break;
        }

        if (highlightPrefab != null && UIManager.Instance.PopupCanvasParent is RectTransform canvas)
        {
            highlights = new TutorialHighlights(highlightPrefab, canvas);
        }
        else
        {
            Debug.LogWarning($"Tutorial chapter {ChapterNumber}: missing highlight prefab or canvas.", this);
        }
        messenger.OnMandatoryMessageShown += OnMessageShown;
        messenger.OnMandatoryMessagesClosed += OnMessagesClosed;

        game.currentPlant = PlantIds.Pea;
        DawnSystem.ClearSelection();
        CurseState.ResetAll();
        SpecialItemSystem.ResetRun();
        PlayerRecordForGraph.ClearAll();
        game.economyManager.InitEconomyManager();
        game.enemyController.InitEnemyController();
        if (ChapterNumber == 3)
            game.enemyController.SetSeason(WaveSchedule.GetSeasonByStage(1));
        if (ChapterNumber > 1)
        {
            AbilityManager.Instance?.ApplyAbilities(game);
            game.shopManager.InitializeGameSeed();
            shopCanvas = FindAnyObjectByType<ShopCanvasController>(FindObjectsInactive.Include);
        }
        if (startingGold > 0) game.economyManager.AddGold(startingGold);
        for (int i = 0; i < 4; i++)
            game.grid.Spawner.AddMovablePlant(InitialTraits(i));

        for (int stage = 1; stage <= FinalStage; stage++)
        {
            game.SetTutorialStage(stage);
            game.modManager.OnNewDay(stage);
            game.enemyController.SetTutorialWave(WaveAt(stage), WaveAt(stage + 1));
            GameEvents.RaiseDayStarted();
            if (shopUnlocked) shopCanvas?.DailyReroll();
            if (questsUnlocked && (stage - 1) % game.requestManager.cycle == 0)
                game.requestManager.StartNewCycle(stage);

            yield return RunDay(stage);
            if (ChapterNumber > 1) yield return game.BreedEndRoutine();
            if (questsUnlocked) GameEvents.RaiseDayPassedForRequest();

            // Phone forecasts use CurrentWave as tomorrow's wave, as in the main loop.
            game.enemyController.AdvanceTutorialWave(WaveAt(stage + 2));
            if (ChapterNumber == 3) game.enemyController.SetNextSeason();
            game.SetTutorialNightGlow(true);
            yield return game.waveManager.StartNightCoroutine();
            yield return StartFreeTime(stage);
            yield return game.phoneManager.PhonePhase();
            game.SetTutorialNightGlow(false);
            yield return game.waveManager.StopNightCoroutine();
            if (questsUnlocked) GameEvents.RaiseQuestDayPassed();
            game.economyManager.PushEarnedGold();
        }

        yield return BeforeComplete();
        yield return Complete();
    }

    protected virtual List<GeneticTrait> InitialTraits(int index)
    {
        return new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, 0.45f + index * 0.1f, index % 2 + 1, 0f)
        };
    }

    protected virtual IEnumerator RunDay(int stage)
    {
        yield return StartDay(stage);
        yield return game.grid.Breeder.Breeding();
        yield return game.enemyController.EnemyWaveCoroutine();
    }

    protected virtual IEnumerator StartDay(int stage)
    {
        if (stage == 1) yield return PlayMessages("day01.start");
    }

    protected virtual IEnumerator StartFreeTime(int stage)
    {
        yield break;
    }

    protected virtual IEnumerator BeforeComplete()
    {
        yield break;
    }

    private void LateUpdate()
    {
        RefreshPractice();
        if (highlights == null) return;

        highlights.BeginFrame();
        if (highlightedMessage != null && messenger.IsMandatoryPopupOpen)
        {
            DrawMessageHighlights(highlightedMessage);
            if (practiceGateVisible && practiceGoals != null)
            {
                for (int i = 0; i < practiceGoals.Length; i++)
                {
                    if (completedPracticeGoals[i] || practiceGoals[i].highlight == null) continue;
                    practiceGoals[i].highlight.Invoke();
                    break;
                }
            }
        }
        highlights.EndFrame();
    }

    private void OnMessageShown(Chat source, ChatMessage message)
    {
        highlightedMessage = source == chat ? message.locId : null;
        practiceGateVisible = source == chat && message.waitForAdvanceSignal && message.triggerId == practiceTrigger;
        RefreshPractice();
        if (practiceGateVisible && practiceSuffix == "day05.tax")
            game.phoneManager.taxCanvas?.Refresh();
    }

    private void OnMessagesClosed()
    {
        highlightedMessage = null;
        practiceGateVisible = false;
        if (practiceTrigger != null) goalPopup?.FadeOut();
    }

    protected IEnumerator PlayPractice(string suffix, params PracticeGoal[] goals) =>
        PlayPractice(suffix, null, goals);

    protected IEnumerator PlayPractice(string suffix, Action onComplete, params PracticeGoal[] goals)
    {
        practiceSuffix = suffix;
        practiceTrigger = Trigger(suffix);
        practiceGoals = goals;
        completedPracticeGoals = new bool[goals.Length];
        practiceCompleted = onComplete;
        yield return PlayMessages(suffix);
        if (goalPopup != null) yield return goalPopup.WaitForFade();
        ClearPractice();
    }

    private void RefreshPractice()
    {
        if (practiceGoals == null || practiceAdvanceUnlocked) return;
        bool complete = true;
        for (int i = 0; i < practiceGoals.Length; i++)
        {
            if (completedPracticeGoals[i]) continue;
            if (practiceGoals[i].isComplete())
            {
                completedPracticeGoals[i] = true;
                goalPopup?.SetChecked(i, true);
            }
            else
            {
                complete = false;
            }
        }
        if (practiceGateVisible && goalPopup == null && !complete)
        {
            var keys = Array.ConvertAll(practiceGoals, goal => goal.key);
            goalPopup = Instantiate(goalPopupPrefab, UIManager.Instance.PopupCanvasParent, false);
            goalPopup.Show(keys);
            for (int i = 0; i < completedPracticeGoals.Length; i++)
                if (completedPracticeGoals[i]) goalPopup.SetChecked(i, true);
        }

        if (!complete || !practiceGateVisible ||
            !messenger.TryGetAwaitingMandatoryAdvance(practiceTrigger, out var handle)) return;
        practiceAdvanceUnlocked = true;
        practiceCompleted?.Invoke();
        messenger.UnlockMandatoryAdvance(handle);
    }

    private void ClearPractice()
    {
        practiceTrigger = null;
        practiceSuffix = null;
        practiceGoals = null;
        completedPracticeGoals = null;
        practiceCompleted = null;
        practiceGateVisible = false;
        practiceAdvanceUnlocked = false;
        if (goalPopup != null) Destroy(goalPopup.gameObject);
        goalPopup = null;
    }

    protected virtual void DrawMessageHighlights(string messageId) { }

    protected void Highlight(RectTransform target) => highlights?.Show(target);
    protected void Highlight(Component target)
    {
        if (target != null && target.TryGetComponent(out Collider collider))
            highlights?.Show(collider);
    }

    protected void HighlightFence()
    {
        if (FenceUIManager.Instance != null)
            highlights?.Show(FenceUIManager.Instance.TraitDisplayPositions);
    }

    protected void HighlightPhone() => Highlight(game.phoneManager.OpenButtonRect);
    protected void HighlightTimer() => Highlight(game.grid.Breeder.GetBreedTimerUI()?.BarRect);

    protected void HighlightPhoneMenu(int index) => Highlight(game.phoneManager.GetMenuButtonRect(index));
    protected void HighlightPhoneApp(int index)
    {
        if (!game.phoneManager.IsOpen) HighlightPhone();
        else HighlightPhoneMenu(index);
    }

    protected bool IsPhoneAppOpen(AppKey app) => game.phoneManager.IsOpen && game.phoneManager.CurrentApp == app;
    protected bool IsPhoneHomeOpen => game.phoneManager.IsOpen && game.phoneManager.CurrentApp == null;
    protected void HighlightShovel() => Highlight(shovel.HandleRect);

    protected void HighlightPracticeBug()
    {
        if (practiceBug != null && !practiceBug.HasBeenCaught)
            Highlight(practiceBug);
    }

    protected void HighlightGift()
    {
        if (specialItemUI != null && SpecialItemSystem.PendingGifts > 0)
            Highlight(specialItemUI.GiftButtonRect);
    }

    protected bool IsGiftChoiceOpen => specialItemUI != null && specialItemUI.IsChoicePanelOpen;

    protected abstract WaveType WaveAt(int stage);

    protected string Trigger(string suffix) => $"tutorial{ChapterNumber}.{suffix}";

    protected IEnumerator PlayMessages(string suffix)
    {
        string trigger = Trigger(suffix);
        var messenger = game.phoneManager.messengerApp;
        messenger.ActivateTrigger(trigger);
        yield return new WaitUntil(() => !messenger.IsMandatoryPopupOpen && messenger.IsTriggerFullySeen(trigger));
    }

    protected void SetShovelUnlocked(bool unlocked)
    {
        shovelUnlocked = unlocked;
        SetShovelVisible(unlocked);
    }

    protected void SetShovelVisible(bool visible)
    {
        shovel.gameObject.SetActive(visible);
        shovelBackground.SetActive(visible);
    }

    protected void RefreshPhoneAccess() => game.phoneManager.RefreshTutorialAccess();

    protected void ShowBreedRulePopup()
    {
        activeBreedRulePopup = Instantiate(breedRulePopup, UIManager.Instance.PopupCanvasParent, false);
        foreach (PopupController panel in activeBreedRulePopup.GetComponentsInChildren<PopupController>(true))
            panel.Closed += CloseBreedRulePopup;
    }

    protected void CloseBreedRulePopup()
    {
        if (activeBreedRulePopup == null) return;
        Destroy(activeBreedRulePopup);
        activeBreedRulePopup = null;
    }

    protected IEnumerator PracticeCatchingBug()
    {
        practiceBug = Instantiate(tutorialBugPrefab).GetComponent<TutorialBug>();
        practiceBug.PrepareStationaryPractice(
            new Vector3(Camera.main.transform.position.x + 2f, Camera.main.transform.position.y, 0f));
        practiceBug.canCatchBug = true;
        yield return PlayPractice("day11.bugs",
            new PracticeGoal("catch_bug", () => practiceBug == null || practiceBug.HasBeenCaught, HighlightPracticeBug));
        bugsUnlocked = true;
    }

    protected void GiveSpecialItem()
    {
        if (specialItemUI == null)
        {
            specialItemUI = FindAnyObjectByType<SpecialItemUIController>();
            if (specialItemUI == null)
            {
                specialItemRoot = Instantiate(specialItemPrefab, UIManager.Instance.PopupCanvasParent, false);
                specialItemUI = specialItemRoot.GetComponentInChildren<SpecialItemUIController>(true);
            }
            specialItemUI?.CloseChoicePanel();
        }
        SpecialItemSystem.AddGift();
    }

    private IEnumerator Complete()
    {
        TutorialCompletion result = SaveManager.Instance.TryCompleteTutorialChapter(ChapterNumber);
        if (result == TutorialCompletion.SaveFailed)
            yield return PlayMessages("complete.save_failed");

        while (result == TutorialCompletion.SaveFailed)
        {
            bool retry = false;
            UIManager.Instance.Popup.ShowCloseablePopup(
                Loc.T("game.tutorial1.save_title"), Loc.T("game.tutorial1.retry_save"),
                onClose: () => retry = true);
            yield return new WaitUntil(() => retry);
            result = SaveManager.Instance.TryCompleteTutorialChapter(ChapterNumber);
        }

        yield return PlayMessages(result == TutorialCompletion.Awarded ? "complete.awarded" : "complete.replay");
        SceneLoader.Instance.LoadStartScene();
    }
}
