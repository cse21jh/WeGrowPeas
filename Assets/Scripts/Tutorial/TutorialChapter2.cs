using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TutorialChapter2 : TutorialChapter
{
    public override int ChapterNumber => 2;
    public override int FinalStage => 20;
    protected override IEnumerable<string> PracticeTriggers => new[] { "day03.shop", "day11.bugs", "day16.quests" };
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
            yield return PracticeCatchingBug();
        }
        else if (stage == 16)
        {
            questsUnlocked = true;
            RefreshPhoneAccess();
            game.requestManager.StartNewCycle(stage);
            var requestUI = FindAnyObjectByType<RequestUI>(FindObjectsInactive.Include);
            yield return PlayPractice("day16.quests",
                new PracticeGoal("open_quests", () => IsPhoneAppOpen(AppKey.Quest),
                    () => HighlightPhoneApp(PhoneManager.MENU_INDEX_QUEST)),
                new PracticeGoal("inspect_quest", () => IsPhoneAppOpen(AppKey.Quest) && requestUI.HasExpandedRequest));
        }
    }

    protected override IEnumerator StartFreeTime(int stage)
    {
        if (stage != 3) yield break;
        shopUnlocked = true;
        RefreshPhoneAccess();
        game.shopManager.AddDailyRerollCount(1);
        int rerollsBefore = ShopCanvas.SuccessfulRerolls;
        yield return PlayPractice("day03.shop",
            new PracticeGoal("open_shop", () => IsPhoneAppOpen(AppKey.Shop),
                () => HighlightPhoneApp(PhoneManager.MENU_INDEX_SHOP)),
            new PracticeGoal("inspect_shop_item", () => IsPhoneAppOpen(AppKey.Shop) && ShopCanvas.IsItemDetailOpen),
            new PracticeGoal("reroll_shop", () => ShopCanvas.SuccessfulRerolls > rerollsBefore,
                () => { if (IsPhoneAppOpen(AppKey.Shop) && !ShopCanvas.IsItemDetailOpen) Highlight(ShopCanvas.RerollRect); }));
    }

    protected override void DrawMessageHighlights(string messageId)
    {
        switch (messageId)
        {
            case "m002": // 다시 사용할 수 있는 기본 도구
                HighlightPhone();
                HighlightShovel();
                break;
            case "m005":
            case "m006":
            case "m017":
                HighlightPracticeBug();
                break;
            case "m003": // 상점 안내
            case "m007": // 퀘스트 안내
                HighlightPhone();
                break;
        }
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
