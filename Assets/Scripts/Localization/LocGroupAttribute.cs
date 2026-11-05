using System;
using System.Collections.Generic;

/// <summary>
/// 이 ScriptableObject의 문구가 있는 번역 표를 지정한다. 서브클래스도 같은 표를 쓴다.
/// 문구는 에셋에 두지 않고 표에만 둔다. 키는 {표}.{에셋 이름}.{항목}이다.
/// 예: [LocGroup("item", "name", "description")] → Resources/Localization/item.csv의 item.Pepper.name
/// </summary>
/// <remarks>Required는 에셋마다 반드시 있어야 하는 항목이다. 에셋 문구 검사가 표에 있는지 확인한다.</remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class LocGroupAttribute : Attribute
{
    public string Prefix { get; }
    public string[] Required { get; }

    public LocGroupAttribute(string prefix, params string[] required)
    {
        Prefix = prefix;
        Required = required;
    }
}

/// <summary>
/// 에셋 이름을 키로 쓸 수 없을 때(한글 파일명 등) 구현한다. 비어 있으면 에셋 이름을 쓴다.
/// </summary>
public interface ILocKeyed
{
    string LocId { get; }
}

/// <summary>
/// 목록 안에 항목별 문구가 있는 ScriptableObject. 키는 {표}.{에셋}.{Id}이다.
/// 순서가 바뀌어도 키가 밀리지 않도록 Id는 순번이 아니라 항목에 붙은 값을 쓴다.
/// </summary>
public interface ILocListSource
{
    IEnumerable<string> GetLocIds();

    /// <summary>Id가 없는 항목에 Id를 준다. 값이 바뀌었으면 true.</summary>
    bool EnsureLocIds();
}

/// <summary>SO 문구의 번역 키 규칙. 검사 도구와 런타임이 같은 키를 쓰도록 한 곳에 둔다.</summary>
public static class LocKey
{
    public static string For(string prefix, string assetName, string suffix) => $"{prefix}.{assetName}.{suffix}";

    /// <summary>
    /// 비어 있거나 앞 항목과 겹치는 Id에 새 값을 준다. 기존 Id는 유지한다.
    /// 인스펙터에서 항목을 복제하면 Id도 복사되므로 겹침도 함께 고친다.
    /// </summary>
    /// <returns>값을 바꿨으면 true.</returns>
    public static bool EnsureUniqueIds<T>(IList<T> items, Func<T, string> getId, Action<T, string> setId) where T : class
    {
        if (items == null) return false;

        int next = 1;
        var used = new HashSet<string>();
        foreach (var item in items)
        {
            if (item == null) continue;
            string id = getId(item);
            if (string.IsNullOrEmpty(id)) continue;
            used.Add(id);
            if (id.Length > 1 && id[0] == 'm' && int.TryParse(id.Substring(1), out int number) && number >= next)
                next = number + 1;
        }

        bool changed = false;
        var seen = new HashSet<string>();
        foreach (var item in items)
        {
            if (item == null) continue;
            string id = getId(item);
            if (!string.IsNullOrEmpty(id) && seen.Add(id)) continue;

            string fresh;
            do fresh = $"m{next++:000}"; while (used.Contains(fresh));
            used.Add(fresh);
            seen.Add(fresh);
            setId(item, fresh);
            changed = true;
        }
        return changed;
    }
}
