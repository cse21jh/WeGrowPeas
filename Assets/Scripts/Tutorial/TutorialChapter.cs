using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private ShopCanvasController shopCanvas;

    public abstract int ChapterNumber { get; }
    public abstract int FinalStage { get; }
    protected abstract string[] RequiredMessages { get; }
    protected virtual string PracticeTrigger => null;
    public virtual bool IsUntimedDay => false;
    public virtual bool CanBreedDuringOpeningMessage => false;
    public virtual bool CanSkipDay => game != null;
    public virtual bool WaitingForFirstTax => false;
    public bool CanOpenPhone => phoneUnlocked;
    public bool CanUseShovel => shovelUnlocked && game != null && !game.GetGameIsStopped();
    public bool CanUseShop => shopUnlocked && game != null && !game.GetGameIsStopped();
    public bool CanUseQuests => questsUnlocked && game != null && !game.GetGameIsStopped();
    public bool CanSpawnBugs => bugsUnlocked;
    public bool CanUseSpecialItems => specialItemsUnlocked && game != null && !game.GetGameIsStopped();

    public bool CanPayTax(int dueStage) => taxUnlocked && game != null &&
        !game.GetGameIsStopped() && dueStage <= FinalStage && dueStage != 40;

    public bool CanOpenApp(int index) =>
        index == PhoneManager.MENU_INDEX_HOME ||
        (phoneUnlocked && index == PhoneManager.MENU_INDEX_MESSENGER) ||
        (taxUnlocked && index == PhoneManager.MENU_INDEX_TAX) ||
        (shopUnlocked && index == PhoneManager.MENU_INDEX_SHOP) ||
        (questsUnlocked && index == PhoneManager.MENU_INDEX_QUEST);

    protected virtual void OnDisable()
    {
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
        var messenger = game.phoneManager.messengerApp;
        if (UIManager.Instance?.PopupCanvasParent == null || TaxManager.Instance == null ||
            SaveManager.Instance == null || SceneLoader.Instance == null || messenger == null ||
            (ChapterNumber == 1 && breedRulePopup == null) ||
            (ChapterNumber == 2 && tutorialBugPrefab == null) ||
            (ChapterNumber == 3 && specialItemPrefab == null))
        {
            Debug.LogError($"Tutorial chapter {ChapterNumber}: missing scene managers or tutorial prefabs.", this);
            yield break;
        }

        var required = new List<string>(RequiredMessages);
        required.AddRange(new[] { "complete.awarded", "complete.replay", "complete.save_failed" });
        string practiceTrigger = PracticeTrigger == null ? null : Trigger(PracticeTrigger);
        if (!messenger.ConfigureTutorialChat(chat, required.ConvertAll(Trigger), practiceTrigger, out var practiceHandle))
        {
            Debug.LogError($"Tutorial chapter {ChapterNumber}: invalid mandatory messages or popup bindings.", this);
            yield break;
        }

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

            yield return RunDay(stage, messenger, practiceHandle);
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

    protected virtual IEnumerator RunDay(int stage, MessengerApp messenger, MandatoryMessageHandle practiceHandle)
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

    protected abstract WaveType WaveAt(int stage);

    protected string Trigger(string suffix) => $"tutorial{ChapterNumber}.{suffix}";

    protected IEnumerator PlayMessages(string suffix)
    {
        string trigger = Trigger(suffix);
        var messenger = game.phoneManager.messengerApp;
        messenger.ActivateTrigger(trigger);
        yield return new WaitUntil(() => messenger.IsTriggerFullySeen(trigger) && !messenger.IsMandatoryPopupOpen);
    }

    protected void SetShovelUnlocked(bool unlocked)
    {
        shovelUnlocked = unlocked;
        shovel.gameObject.SetActive(unlocked);
        shovelBackground.SetActive(unlocked);
    }

    protected void RefreshPhoneAccess() => game.phoneManager.RefreshTutorialAccess();

    protected void OpenApp(int index)
    {
        RefreshPhoneAccess();
        game.phoneManager.SetOpen(true);
        game.phoneManager.SelectBottomMenu(index);
    }

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
        yield return new WaitUntil(() => practiceBug == null);
        bugsUnlocked = true;
    }

    protected void GiveSpecialItem()
    {
        if (specialItemRoot == null)
            specialItemRoot = Instantiate(specialItemPrefab, UIManager.Instance.PopupCanvasParent, false);
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
