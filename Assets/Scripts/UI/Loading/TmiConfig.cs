using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로딩창에 띄울 TMI(팁) 목록. Resources/Data/TmiConfig 에셋으로 배치한다.
/// 기획자가 에셋에서 직접 문구를 추가·수정할 수 있다.
/// </summary>
[CreateAssetMenu(menuName = "Loading/Tmi Config", fileName = "TmiConfig")]
public class TmiConfig : ScriptableObject
{
    [Tooltip("로딩창에서 무작위로 하나 뽑아 보여줄 문구들")]
    [TextArea]
    public List<string> tips = new List<string>();
}
