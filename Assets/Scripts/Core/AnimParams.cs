using UnityEngine;

/// <summary>Animator 파라미터와 상태 이름. Animator 창의 이름과 같아야 한다.</summary>
public static class AnimParams
{
    public static readonly int Start = Animator.StringToHash("Start");
    public static readonly int IsShow = Animator.StringToHash("isShow");
    public static readonly int FaceIndex = Animator.StringToHash("faceIndex");
    public static readonly int Play = Animator.StringToHash("Play");
    public static readonly int StartEndLetter = Animator.StringToHash("StartEndLetter");
    public static readonly int SprinklerState = Animator.StringToHash("sprinkler");
}
