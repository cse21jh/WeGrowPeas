using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>도감 UI가 카테고리 구분 없이 렌더링할 수 있는 통합 엔트리.</summary>
public class CodexEntry
{
    public CodexProgress.Category category;
    public string id;
    public string displayName;
    public Sprite icon;
    public bool discovered;
    public string detail; // 우측 상세(리치텍스트)

    public bool locked;      // 아직 해금되지 않음(등장 조건 미충족). 발견 여부와 별개.
    public string unlockHint; // 잠김일 때 상세에 보여줄 해금 조건 설명
}

/// <summary>
/// 카테고리(아이템/식물/저주/벌레/업적)를 Resources에서 로드해 <see cref="CodexEntry"/> 목록으로 통합 제공.
/// 발견 여부(<see cref="CodexProgress"/>)와 누적 통계를 함께 채운다. 씬 의존 없음(시작화면에서도 사용 가능).
/// </summary>
public static class CodexCatalog
{
    public static List<CodexEntry> Get(CodexProgress.Category cat)
    {
        switch (cat)
        {
            case CodexProgress.Category.Item: return GetItems();
            case CodexProgress.Category.Plant: return GetPlants();
            case CodexProgress.Category.Curse: return GetCurses();
            case CodexProgress.Category.Bug: return GetBugs();
            case CodexProgress.Category.Achievement: return GetAchievements();
        }
        return new List<CodexEntry>();
    }

    public static List<CodexEntry> GetItems()
    {
        var list = new List<CodexEntry>();
        foreach (var it in Resources.LoadAll<ItemData>("Data/Item Data"))
        {
            if (it == null) continue;
            string id = it.UnlockId;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(it.GradeTagText)) sb.AppendLine($"[{it.GradeTagText}] {it.Rarity}");
            sb.AppendLine(Loc.T("game.codex.price", ("price", it.Price)));
            if (!string.IsNullOrEmpty(it.Description)) sb.AppendLine(it.LocalizedDescription);

            bool locked = !it.IsMetaUnlocked();

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Item,
                id = id,
                displayName = it.LocalizedName,
                icon = it.Icon,
                discovered = CodexProgress.IsDiscovered(CodexProgress.Category.Item, id),
                detail = sb.ToString().TrimEnd(),
                locked = locked,
                unlockHint = locked ? it.GetUnlockConditionText() : null
            });
        }
        return list;
    }

    public static List<CodexEntry> GetPlants()
    {
        var list = new List<CodexEntry>();
        foreach (var p in Resources.LoadAll<PlantCodexData>("Data/Codex/Plant"))
        {
            if (p == null) continue;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(p.description)) sb.AppendLine(p.LocalizedDescription);
            if (!string.IsNullOrEmpty(p.traitInfo)) sb.AppendLine(Loc.T("game.codex.trait", ("trait", p.LocalizedTraitInfo)));
            if (!string.IsNullOrEmpty(p.resistanceNote)) sb.AppendLine(p.LocalizedResistanceNote);
            // 판매 수를 세는 것은 완두콩·땅콩뿐이다. 나머지는 "판매한 수: 0"이 늘 붙어 헷갈리므로 뺀다.
            if (p.plantId == PlantIds.Pea)
                sb.AppendLine(Loc.T("game.codex.sold", ("count", CodexProgress.GetStat(CodexProgress.StatSoldPea))));
            else if (p.plantId == PlantIds.Peanut)
                sb.AppendLine(Loc.T("game.codex.sold", ("count", CodexProgress.GetStat(CodexProgress.StatSoldPeanut))));

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Plant,
                id = p.plantId,
                displayName = p.LocalizedName,
                icon = p.icon,
                discovered = CodexProgress.IsDiscovered(CodexProgress.Category.Plant, p.plantId),
                detail = sb.ToString().TrimEnd()
            });
        }
        return list;
    }

    public static List<CodexEntry> GetCurses()
    {
        var list = new List<CodexEntry>();
        foreach (var c in Resources.LoadAll<CurseScriptable>("Data/Codex/Curse"))
        {
            if (c == null) continue;
            var sb = new StringBuilder();
            sb.AppendLine(Loc.T(c.curseType == CurseType.Seasonal ? "game.codex.curse_seasonal" : "game.codex.curse_temporal"));
            if (!string.IsNullOrEmpty(c.description)) sb.AppendLine(c.LocalizedDescription);
            if (c.levels != null)
                for (int i = 0; i < c.levels.Count; i++)
                    if (c.levels[i] != null && !string.IsNullOrEmpty(c.levels[i].note))
                        sb.AppendLine(Loc.T("game.codex.curse_level", ("level", i + 1), ("note", c.GetLevelNote(i))));

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Curse,
                id = c.curseId,
                displayName = c.LocalizedTitle,
                icon = null,
                discovered = CodexProgress.IsDiscovered(CodexProgress.Category.Curse, c.curseId),
                detail = sb.ToString().TrimEnd()
            });
        }
        list.Sort((a, b) => string.Compare(a.id, b.id, System.StringComparison.Ordinal));
        return list;
    }

    public static List<CodexEntry> GetBugs()
    {
        var list = new List<CodexEntry>();
        foreach (var b in Resources.LoadAll<BugCodexData>("Data/Codex/Bug"))
        {
            if (b == null) continue;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(b.description)) sb.AppendLine(b.LocalizedDescription);
            sb.AppendLine(Loc.T("game.codex.bug_kills", ("count", CodexProgress.GetStat(CodexProgress.StatBugKill(b.bugId)))));

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Bug,
                id = b.bugId,
                displayName = b.LocalizedName,
                icon = b.icon,
                discovered = CodexProgress.IsDiscovered(CodexProgress.Category.Bug, b.bugId),
                detail = sb.ToString().TrimEnd()
            });
        }
        return list;
    }

    /// <summary>
    /// 업적. 달성했거나 숨김이 아니면 이름을 보여주고, 숨김 업적은 달성 전까지 ??? 로 둔다.
    /// </summary>
    public static List<CodexEntry> GetAchievements()
    {
        var list = new List<CodexEntry>();
        foreach (var a in AchievementSystem.All)
        {
            if (a == null || string.IsNullOrEmpty(a.id)) continue;
            bool done = AchievementSystem.IsUnlocked(a.id);

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(a.description)) sb.AppendLine(a.LocalizedDescription);
            if (done && AchievementSystem.TryGetUnlockedAt(a.id, out var at))
                sb.AppendLine(Loc.T("game.codex.achieved", ("year", at.Year), ("month", at.Month), ("day", at.Day)));
            else if (a.condition == AchievementCondition.LifetimeStat)
                sb.AppendLine(Loc.T("game.codex.progress", ("current", AchievementSystem.GetProgress(a)), ("target", a.target)));
            else
                sb.AppendLine(Loc.T("game.codex.not_achieved"));

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Achievement,
                id = a.id,
                displayName = done ? a.LocalizedTitle : Loc.T("game.codex.title_not_achieved", ("title", a.LocalizedTitle)),
                icon = a.icon,
                discovered = done || !a.hidden,
                detail = sb.ToString().TrimEnd()
            });
        }
        return list;
    }
}
