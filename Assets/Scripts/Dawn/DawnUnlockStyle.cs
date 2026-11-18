using TMPro;
using UnityEngine;

/// <summary>
/// 새벽 해금 연출(<see cref="DawnUnlockOverlay"/>)의 글꼴. 연출은 실행 중에 만들어져 씬 참조를 받을 수 없으므로
/// Resources의 이 에셋에서 글꼴을 읽는다. 없으면 씬에 있는 아무 글꼴이나 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "Dawn/Dawn Unlock Style", fileName = "DawnUnlockStyle")]
public class DawnUnlockStyle : ScriptableObject
{
    public const string ResourcePath = "Data/DawnUnlockStyle";

    [Tooltip("연출 글꼴. 메뉴·설정창과 같은 글꼴을 쓴다.")]
    public TMP_FontAsset font;

    [Tooltip("글꼴 재질(테두리). 비우면 글꼴 기본 재질.")]
    public Material material;
}
