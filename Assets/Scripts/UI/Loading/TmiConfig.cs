using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로딩창에 띄울 TMI(팁) 목록. Resources/Data/TmiConfig 에셋으로 배치한다.
/// 문구는 번역 표(tmi.csv)의 tip1, tip2 ... 행이고, 번호가 끊기면 거기까지 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "Loading/Tmi Config", fileName = "TmiConfig")]
[LocGroup(LocPrefix)]
public class TmiConfig : ScriptableObject
{
    private const string LocPrefix = "tmi";

    /// <summary>화면에 보여줄 문구 목록.</summary>
    public List<string> GetLocalizedTips()
    {
        var tips = new List<string>();
        for (int i = 1; Loc.Has(LocKey.For(LocPrefix, name, $"tip{i}")); i++)
            tips.Add(Loc.T(LocKey.For(LocPrefix, name, $"tip{i}")));
        return tips;
    }
}
