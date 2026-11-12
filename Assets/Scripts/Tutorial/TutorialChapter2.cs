using System.Collections;
using System.Collections.Generic;

public sealed class TutorialChapter2 : TutorialChapter
{
    public override int ChapterNumber => 2;
    public override int FinalStage => 20;
    protected override string[] RequiredMessages => new[]
    {
        "day01.start", "day03.shop", "day11.bugs", "day16.quests"
    };

    protected override List<GeneticTrait> InitialTraits(int index)
    {
        var traits = new List<GeneticTrait>
        {
            new GeneticTrait(TraitType.NaturalDeath, 0.6f + index * 0.05f, 2, 0f)
        };
        if (index >= 2) traits.Add(new GeneticTrait(TraitType.Wind, 0.75f, 2, 0f));
        return traits;
    }

    protected override IEnumerator StartDay(int stage)
    {
        yield return base.StartDay(stage);
        if (stage == 11)
        {
            yield return PlayMessages("day11.bugs");
            yield return PracticeCatchingBug();
        }
        else if (stage == 16)
        {
            yield return PlayMessages("day16.quests");
            questsUnlocked = true;
            game.requestManager.StartNewCycle(stage);
            OpenApp(PhoneManager.MENU_INDEX_QUEST);
        }
    }

    protected override IEnumerator StartFreeTime(int stage)
    {
        if (stage != 3) yield break;
        yield return PlayMessages("day03.shop");
        shopUnlocked = true;
        OpenApp(PhoneManager.MENU_INDEX_SHOP);
    }

    protected override WaveType WaveAt(int stage)
    {
        if (stage < 1 || stage > FinalStage) return WaveType.None;
        if (stage < 5 || stage % 2 != 0) return WaveType.Aging;
        if (stage < 10) return WaveType.Wind;
        if (stage < 15) return WaveType.Pest;
        return WaveType.Flood;
    }
}
