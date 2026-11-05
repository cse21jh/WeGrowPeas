using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 로딩창에 띄울 TMI(팁) 목록. Resources/Data/TmiConfig 에셋으로 배치한다.
/// 기획자가 에셋에서 직접 문구를 추가·수정할 수 있다.
/// </summary>
[CreateAssetMenu(menuName = "Loading/Tmi Config", fileName = "TmiConfig")]
[LocGroup(LocPrefix)]
public class TmiConfig : ScriptableObject, ILocListSource
{
    private const string LocPrefix = "tmi";

    [Tooltip("로딩창에서 무작위로 하나 뽑아 보여줄 문구들")]
    [TextArea]
    public List<string> tips = new List<string>();

    /// <summary>화면에 보여줄 문구 목록.</summary>
    public List<string> GetLocalizedTips()
        => tips.Select((tip, i) => Loc.Or(LocKey.For(LocPrefix, name, TipId(i)), tip)).ToList();

    public IEnumerable<LocListText> GetLocTexts() => tips.Select((tip, i) => new LocListText(TipId(i), tip));

    // 문구만 있는 목록이라 순번을 키로 쓴다. 번역이 밀리지 않게 새 문구는 맨 뒤에 추가한다.
    public bool EnsureLocIds() => false;

    private static string TipId(int index) => $"tip{index + 1}";
}
