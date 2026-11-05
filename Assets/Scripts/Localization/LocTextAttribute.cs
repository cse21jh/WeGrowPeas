using System;

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

/// <summary>SO 텍스트의 번역 키 규칙. 추출 도구와 런타임이 같은 키를 쓰도록 한 곳에 둔다.</summary>
public static class LocKey
{
    public static string For(string prefix, string assetName, string suffix) => $"{prefix}.{assetName}.{suffix}";
}
