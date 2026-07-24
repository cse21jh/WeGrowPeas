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
/// 4카테고리(아이템/식물/저주/벌레)를 Resources에서 로드해 <see cref="CodexEntry"/> 목록으로 통합 제공.
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
            sb.AppendLine($"가격: {it.Price}골드");
            if (!string.IsNullOrEmpty(it.Description)) sb.AppendLine(it.Description);

            bool locked = !it.IsMetaUnlocked();

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Item,
                id = id,
                displayName = it.DisplayName,
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
            if (!string.IsNullOrEmpty(p.description)) sb.AppendLine(p.description);
            if (!string.IsNullOrEmpty(p.traitInfo)) sb.AppendLine($"특성: {p.traitInfo}");
            if (!string.IsNullOrEmpty(p.resistanceNote)) sb.AppendLine(p.resistanceNote);
            int sold = p.plantId == "완두콩" ? CodexProgress.GetStat(CodexProgress.StatSoldPea)
                     : p.plantId == "땅콩" ? CodexProgress.GetStat(CodexProgress.StatSoldPeanut) : 0;
            sb.AppendLine($"판매한 수: {sold}");

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Plant,
                id = p.plantId,
                displayName = p.displayName,
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
            sb.AppendLine(c.curseType == CurseType.Seasonal ? "지속형 저주" : "단발형 저주");
            if (!string.IsNullOrEmpty(c.description)) sb.AppendLine(c.description);
            if (c.levels != null)
                for (int i = 0; i < c.levels.Count; i++)
                    if (c.levels[i] != null && !string.IsNullOrEmpty(c.levels[i].note))
                        sb.AppendLine($"{i + 1}단계: {c.levels[i].note}");

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Curse,
                id = c.curseId,
                displayName = c.title,
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
            if (!string.IsNullOrEmpty(b.description)) sb.AppendLine(b.description);
            sb.AppendLine($"잡은 횟수: {CodexProgress.GetStat(CodexProgress.StatBugKill(b.bugId))}");

            list.Add(new CodexEntry
            {
                category = CodexProgress.Category.Bug,
                id = b.bugId,
                displayName = b.displayName,
                icon = b.icon,
                discovered = CodexProgress.IsDiscovered(CodexProgress.Category.Bug, b.bugId),
                detail = sb.ToString().TrimEnd()
            });
        }
        return list;
    }
}
