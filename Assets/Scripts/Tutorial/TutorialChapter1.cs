using System.Collections;
using UnityEngine;

public sealed class TutorialChapter1 : TutorialChapter
{
    public override int ChapterNumber => 1;
    public override int FinalStage => 10;
    protected override string PracticeTrigger => "day01.start";
    protected override string[] RequiredMessages => new[]
    {
        "day01.start", "day01.phone", "day02.wave", "day03.plant",
        "day04.sale", "day05.tax", "day06.forecast"
    };

    private bool firstBreedCompleted;

    public override bool IsUntimedDay => game != null && game.stage == 1;
    public override bool CanSkipDay => game != null && (game.stage > 1 || firstBreedCompleted);
    public override bool CanBreedDuringOpeningMessage => IsUntimedDay &&
        game.grid.Breeder.GetIsBreeding() && game.phoneManager.messengerApp.IsMandatoryPopupOpen;
    public override bool WaitingForFirstTax => game != null && game.stage == 5 && !TaxManager.Instance.IsPaidForStage(5);

    private void OnEnable() => GameEvents.OnPeaBreeded += RecordBreed;

    protected override void OnDisable()
    {
        GameEvents.OnPeaBreeded -= RecordBreed;
        base.OnDisable();
    }

    private void RecordBreed()
    {
        if (CanBreedDuringOpeningMessage) firstBreedCompleted = true;
    }

    protected override IEnumerator RunDay(int stage, MessengerApp messenger, MandatoryMessageHandle practiceHandle)
    {
        if (stage != 1)
        {
            yield return base.RunDay(stage, messenger, practiceHandle);
            yield break;
        }

        messenger.ActivateTrigger(Trigger("day01.start"));
        Coroutine breeding = StartCoroutine(game.grid.Breeder.Breeding());
        yield return new WaitUntil(() => firstBreedCompleted);
        ShowBreedRulePopup();
        messenger.UnlockMandatoryAdvance(practiceHandle);
        yield return breeding;
        CloseBreedRulePopup();
        game.enemyController.RecordTutorialDayWithoutWave();
    }

    protected override IEnumerator StartDay(int stage)
    {
        switch (stage)
        {
            case 2:
                yield return PlayMessages("day02.wave");
                break;
            case 3:
                game.grid.Spawner.AddMovablePlant(new System.Collections.Generic.List<GeneticTrait>
                {
                    new GeneticTrait(TraitType.NaturalDeath, 0.65f, 2, 0f),
                    new GeneticTrait(TraitType.Wind, 0.8f, 2, 0f)
                });
                yield return PlayMessages("day03.plant");
                break;
            case 4:
                yield return PlayMessages("day04.sale");
                SetShovelUnlocked(true);
                break;
            case 6:
                yield return PlayMessages("day06.forecast");
                break;
        }
    }

    protected override IEnumerator StartFreeTime(int stage)
    {
        if (stage == 1)
        {
            phoneUnlocked = true;
            RefreshPhoneAccess();
            yield return PlayMessages("day01.phone");
        }
        else if (stage == 5)
        {
            yield return PlayMessages("day05.tax");
            taxUnlocked = true;
            int shortfall = TaxManager.Instance.DueAmount - game.economyManager.GetGold();
            if (shortfall > 0) game.economyManager.AddGold(shortfall);
            OpenApp(PhoneManager.MENU_INDEX_TAX);
        }
    }

    protected override WaveType WaveAt(int stage)
    {
        if (stage < 2 || stage > FinalStage) return WaveType.None;
        return stage >= 6 && stage % 2 == 0 ? WaveType.Wind : WaveType.Aging;
    }
}
