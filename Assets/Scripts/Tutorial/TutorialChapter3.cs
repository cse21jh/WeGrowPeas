using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TutorialChapter3 : TutorialChapter
{
    [SerializeField] private Cinemachine.CinemachineVirtualCamera[] endingCameras;

    public override int ChapterNumber => 3;
    public override int FinalStage => 40;
    protected override string[] RequiredMessages => new[]
    {
        "day01.start", "day10.special", "day21.season", "complete.ending"
    };

    private static readonly WaveType[] SeasonalWaves =
    {
        WaveType.Wind, WaveType.Pest, WaveType.Flood, WaveType.HeavyRain,
        WaveType.Cold, WaveType.Drought, WaveType.Heat
    };

    protected override List<GeneticTrait> InitialTraits(int index) => new List<GeneticTrait>
    {
        new GeneticTrait(TraitType.NaturalDeath, 0.65f + index * 0.05f, 2, 0f),
        new GeneticTrait(TraitType.Wind, 0.7f, 2, 0f),
        new GeneticTrait(TraitType.Pest, 0.7f, 2, 0f),
        new GeneticTrait(TraitType.Flood, 0.7f, 2, 0f)
    };

    protected override IEnumerator StartDay(int stage)
    {
        yield return base.StartDay(stage);
        if (stage == 21) yield return PlayMessages("day21.season");
    }

    protected override IEnumerator StartFreeTime(int stage)
    {
        if (stage == 10)
        {
            yield return PlayMessages("day10.special");
            specialItemsUnlocked = true;
            GiveSpecialItem();
            yield return new WaitUntil(() => SpecialItemSystem.PendingGifts == 0);
        }
        else if (stage == 20 || stage == 30)
        {
            GiveSpecialItem();
        }
    }

    protected override IEnumerator BeforeComplete()
    {
        game.phoneManager.SetOpen(false);
        // The renewed garden scene has no legacy Ending camera bindings.
        // These cameras belong to this scene and do not change the main game's ending route.
        FindAnyObjectByType<LetterController>()?.StartEndLetter();
        yield return new WaitForSeconds(5f);
        foreach (var camera in endingCameras)
        {
            if (camera != null) camera.Priority = 20;
        }
        FindAnyObjectByType<UIFadeController>()?.FadeOut();
        yield return new WaitForSeconds(1f);
        yield return PlayMessages("complete.ending");
    }

    protected override WaveType WaveAt(int stage)
    {
        if (stage < 1 || stage > FinalStage) return WaveType.None;
        if (stage % 3 == 1) return WaveType.Aging;
        if (stage < 5) return WaveType.Aging;
        if (stage < 10) return WaveType.Pest;
        if (stage < 15) return WaveType.Wind;
        if (stage < 21) return WaveType.Flood;

        WaveType wave = WaveType.Aging;
        Season season = WaveSchedule.GetSeasonByStage(stage);
        foreach (var candidate in SeasonalWaves)
        {
            if (stage >= WaveSchedule.GetFirstAppearStage(candidate) &&
                WaveSchedule.IsSeasonAllowed(candidate, season))
                wave = candidate;
        }
        return wave;
    }
}
