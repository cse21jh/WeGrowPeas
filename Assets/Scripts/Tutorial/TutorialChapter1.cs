using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TutorialChapter1 : TutorialChapter
{
    public override int ChapterNumber => 1;
    public override int FinalStage => 10;
    private const string CameraPractice = "day01.camera";
    private const string SelectPractice = "day01.select";
    private const string BreedPractice = "day01.breed";
    private const string NightTransition = "day01.night_transition";
    private static readonly string[] Practices =
    {
        CameraPractice, SelectPractice, BreedPractice, "day01.phone", "day02.wave", "day03.plant",
        "day04.sale", "day05.tax", "day06.forecast"
    };
    protected override IEnumerable<string> PracticeTriggers => Practices;
    protected override string[] RequiredMessages => new[] { NightTransition };

    [Flags]
    private enum Goal
    {
        None = 0, Pan = 1, ZoomIn = 2, ZoomOut = 4, Select = 8, Breed = 16
    }

    private Goal completedGoals;
    private bool firstDayConfirmed;
    private bool recordingGoals;
    private VcamManager cameraManager;
    private BreedingController breeder;
    private Plant windPlant;

    public override bool IsUntimedDay => game != null && game.stage == 1;
    public override bool CanSkipDay => game != null && (game.stage > 1 || firstDayConfirmed);
    public override bool CanBreedDuringOpeningMessage => recordingGoals && IsUntimedDay &&
        game.grid.Breeder.GetIsBreeding();
    public override bool WaitingForFirstTax => game != null && game.stage == 5 && !TaxManager.Instance.IsPaidForStage(5);

    protected override void OnDisable()
    {
        StopRecordingGoals();
        base.OnDisable();
    }

    private void RecordPan() => Record(Goal.Pan);
    private void RecordZoom(bool zoomIn) => Record(zoomIn ? Goal.ZoomIn : Goal.ZoomOut);
    private void RecordSelection() => Record(Goal.Select);
    private void RecordBreed() => Record(Goal.Breed);

    private void Record(Goal goal)
    {
        if (!recordingGoals || (completedGoals & goal) != 0) return;
        completedGoals |= goal;
    }

    private void StopRecordingGoals()
    {
        if (!recordingGoals) return;
        recordingGoals = false;
        GameEvents.OnPeaBreeded -= RecordBreed;
        if (cameraManager != null)
        {
            cameraManager.OnCameraDragged -= RecordPan;
            cameraManager.OnCameraZoomed -= RecordZoom;
        }
        if (breeder != null) breeder.OnParentSelected -= RecordSelection;
    }

    protected override IEnumerator RunDay(int stage)
    {
        if (stage != 1)
        {
            yield return base.RunDay(stage);
            yield break;
        }

        cameraManager = FindAnyObjectByType<VcamManager>();
        breeder = game.grid.Breeder;
        if (cameraManager == null)
        {
            Debug.LogError("Tutorial chapter 1 needs a camera manager.", this);
            yield break;
        }

        completedGoals = Goal.None;
        firstDayConfirmed = false;
        recordingGoals = true;
        cameraManager.OnCameraDragged += RecordPan;
        cameraManager.OnCameraZoomed += RecordZoom;
        breeder.OnParentSelected += RecordSelection;
        GameEvents.OnPeaBreeded += RecordBreed;
        Coroutine breeding = StartCoroutine(game.TutorialFreeTimeRoutine());
        yield return PlayPractice(CameraPractice,
            new PracticeGoal("pan", () => HasGoal(Goal.Pan)),
            new PracticeGoal("zoom_in", () => HasGoal(Goal.ZoomIn)),
            new PracticeGoal("zoom_out", () => HasGoal(Goal.ZoomOut)));
        yield return PlayPractice(SelectPractice, new PracticeGoal("select_pea", () => HasGoal(Goal.Select)));
        yield return PlayPractice(BreedPractice, ShowBreedRulePopup,
            new PracticeGoal("breed_peas", () => HasGoal(Goal.Breed)));
        StopRecordingGoals();
        CloseBreedRulePopup();
        yield return PlayMessages(NightTransition);
        firstDayConfirmed = true;
        breeder.SkipBreed();
        yield return breeding;
        game.enemyController.RecordTutorialDayWithoutWave();
    }

    private bool HasGoal(Goal goal) => (completedGoals & goal) != 0;

    protected override IEnumerator StartDay(int stage)
    {
        switch (stage)
        {
            case 2:
                yield return PlayPractice("day02.wave", new PracticeGoal("inspect_plant",
                    () => FenceUIManager.Instance.HasVisiblePlant, () => HighlightPlants(1)));
                break;
            case 3:
                if (!game.grid.Board.HasEmptyCell())
                {
                    // A full field can still practice inspecting the wind trait.
                    foreach (var plant in game.grid.Board.Plants.Values)
                    {
                        if (plant == null || plant.isDying || !plant.IsBreedable) continue;
                        windPlant = plant;
                        if (!plant.HasTrait(WaveType.Wind))
                        {
                            var traits = new List<GeneticTrait>(plant.GetGeneticTrait())
                            {
                                new GeneticTrait(TraitType.Wind, 0.8f, 2, 0f)
                            };
                            plant.SetTrait(traits);
                        }
                        break;
                    }
                }
                else windPlant = game.grid.Spawner.AddMovablePlant(new List<GeneticTrait>
                {
                    new GeneticTrait(TraitType.NaturalDeath, 0.65f, 2, 0f),
                    new GeneticTrait(TraitType.Wind, 0.8f, 2, 0f)
                });
                yield return PlayPractice("day03.plant", new PracticeGoal("inspect_wind",
                    () => windPlant != null && FenceUIManager.Instance.CheckFenceIsShowingMe(windPlant.gridIndex),
                    () => Highlight(windPlant)));
                break;
            case 4:
                if (game.grid.Board.CountLivingPlants() == 1)
                    game.grid.Spawner.AddMovablePlant(InitialTraits(0));
                int soldBefore = game.economyManager.PeaSellCount;
                SetShovelUnlocked(true);
                yield return PlayPractice("day04.sale",
                    new PracticeGoal("pick_up_shovel", () => game.grid.isDraggingShovel ||
                        game.economyManager.PeaSellCount > soldBefore, HighlightShovel),
                    new PracticeGoal("sell_plant", () => game.economyManager.PeaSellCount > soldBefore,
                        () => { if (!game.grid.isDraggingShovel) HighlightShovel(); else HighlightPlants(1); }));
                break;
            case 6:
                var timer = game.grid.Breeder.GetBreedTimerUI();
                int previewCount = timer.WavePreviewCount;
                yield return PlayPractice("day06.forecast", new PracticeGoal("inspect_timer",
                    () => timer.WavePreviewCount > previewCount, HighlightTimer));
                break;
        }
    }

    protected override IEnumerator StartFreeTime(int stage)
    {
        if (stage == 1)
        {
            phoneUnlocked = true;
            RefreshPhoneAccess();
            yield return PlayPractice("day01.phone",
                new PracticeGoal("open_phone", () => game.phoneManager.IsOpen, HighlightPhone),
                new PracticeGoal("open_messenger", () => IsPhoneAppOpen(AppKey.Messenger),
                    () => HighlightPhoneApp(PhoneManager.MENU_INDEX_MESSENGER)));
        }
        else if (stage == 5)
        {
            taxUnlocked = true;
            int shortfall = TaxManager.Instance.DueAmount - game.economyManager.GetGold();
            if (shortfall > 0) game.economyManager.AddGold(shortfall);
            yield return PlayPractice("day05.tax",
                new PracticeGoal("open_tax", () => IsPhoneAppOpen(AppKey.Tax) || TaxManager.Instance.IsPaidForStage(5),
                    () => HighlightPhoneApp(PhoneManager.MENU_INDEX_TAX)),
                new PracticeGoal("pay_tax", () => TaxManager.Instance.IsPaidForStage(5),
                    () => { if (IsPhoneAppOpen(AppKey.Tax)) Highlight(game.phoneManager.taxCanvas?.PayButtonRect);
                        else HighlightPhoneApp(PhoneManager.MENU_INDEX_TAX); }));
        }
    }

    protected override void DrawMessageHighlights(string messageId)
    {
        var breeder = game.grid.Breeder;
        switch (messageId)
        {
            case "m002": // 완두콩 살펴보기
            case "m003": // 완두콩 이동과 선택
            case "m038" when (completedGoals & Goal.Select) == 0:
                HighlightPlants(1);
                break;
            case "m017": // 울타리에서 형질과 가격 확인
            case "m023": // 울타리에서 생존 확률 확인
                HighlightFence();
                break;
            case "m018": // 부모 선택과 교배
            case "m005": // 첫 교배 실습
                if ((completedGoals & Goal.Breed) != 0) break;
                if (breeder.BreedButtonRect.gameObject.activeInHierarchy)
                    Highlight(breeder.BreedButtonRect);
                else
                    HighlightPlants(2, skipSelected: true);
                break;
            case "m021": // 낮 건너뛰기
                Highlight(breeder.BreedSkipRect);
                break;
            case "m006":
            case "m022":
                if (game.phoneManager.IsOpen && messageId == "m022")
                    HighlightPhoneMenu(PhoneManager.MENU_INDEX_MESSENGER);
                else
                    HighlightPhone();
                break;
            case "m007":
            case "m020":
            case "m012":
            case "m026":
                HighlightTimer();
                break;
            case "m030":
                Highlight(breeder.BreedCountRect);
                break;
            case "m008":
                Highlight(windPlant);
                break;
            case "m031":
            case "m032":
                Highlight(windPlant);
                HighlightFence();
                break;
            case "m009":
                HighlightShovel();
                break;
            case "m033":
                if (game.grid.Board.Plants.Count > 1) HighlightPlants(1);
                break;
            case "m010":
                HighlightPhone();
                break;
        }
    }

    private void HighlightPlants(int count, bool skipSelected = false)
    {
        if (skipSelected)
        {
            foreach (var plant in game.grid.Board.Plants.Values)
            {
                if (plant != null && plant.IsSelected) count--;
            }
        }
        foreach (var plant in game.grid.Board.Plants.Values)
        {
            if (count <= 0) break;
            if (plant == null || plant.isDying || !plant.IsMovable ||
                (skipSelected && (plant.IsSelected || !plant.IsBreedable))) continue;
            Highlight(plant);
            count--;
        }
    }

    protected override WaveType WaveAt(int stage)
    {
        if (stage < 2 || stage > FinalStage) return WaveType.None;
        return stage >= 6 && stage % 2 == 0 ? WaveType.Wind : WaveType.Aging;
    }
}
