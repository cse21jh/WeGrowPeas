using System;
using System.Collections.Generic;

/// <summary>
/// 이 ScriptableObject의 텍스트가 들어갈 번역 표를 지정한다. 서브클래스도 같은 표를 쓴다.
/// 예: [LocGroup("item")] → Resources/Localization/item.csv, 키는 item.{에셋 이름}.{필드}
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class LocGroupAttribute : Attribute
{
    public string Prefix { get; }

    public LocGroupAttribute(string prefix)
    {
        Prefix = prefix;
    }
}

/// <summary>
/// 번역 표로 추출할 문자열 필드. Localization 창의 'SO 텍스트 추출'이 이 필드의 값을 표에 추가한다.
/// 코드에서는 필드를 직접 읽지 말고 <see cref="Loc.Or(string, string)"/>를 거치는 속성으로 읽는다.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class LocTextAttribute : Attribute
{
    public string Suffix { get; }

    public LocTextAttribute(string suffix)
    {
        Suffix = suffix;
    }
}

/// <summary>
/// 에셋 이름을 키로 쓸 수 없을 때(한글 파일명 등) 구현한다. 비어 있으면 에셋 이름을 쓴다.
/// </summary>
public interface ILocKeyed
{
    string LocId { get; }
}

/// <summary>목록 안의 문구 하나. Id는 그 에셋 안에서 유일하고 바뀌지 않아야 한다.</summary>
public readonly struct LocListText
{
    public readonly string Id;
    public readonly string Text;

    public LocListText(string id, string text)
    {
        Id = id;
        Text = text;
    }
}

/// <summary>
/// 목록 안에 문구가 있는 ScriptableObject. 키는 {표}.{에셋}.{Id}이다.
/// 순서가 바뀌어도 키가 밀리지 않도록 Id는 순번이 아니라 항목에 붙은 값을 쓴다.
/// </summary>
public interface ILocListSource
{
    IEnumerable<LocListText> GetLocTexts();

    /// <summary>Id가 없는 항목에 Id를 준다. 값이 바뀌었으면 true.</summary>
    bool EnsureLocIds();
}

/// <summary>SO 텍스트의 번역 키 규칙. 추출 도구와 런타임이 같은 키를 쓰도록 한 곳에 둔다.</summary>
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
