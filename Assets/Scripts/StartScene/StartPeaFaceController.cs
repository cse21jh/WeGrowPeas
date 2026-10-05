using UnityEngine;

/// <summary>Chooses a looping face for each spawned start-screen pea.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class StartPeaFaceController : MonoBehaviour
{
    [Tooltip("랜덤으로 재생할 peaFace Animator의 상태 이름")]
    [SerializeField] private string[] faceStates =
    {
        "Base Layer.FaceAnim_0", "Base Layer.FaceAnim_1", "Base Layer.FaceAnim_2",
        "Base Layer.FaceAnim_3", "Base Layer.FaceAnim_4", "Base Layer.FaceAnim_5",
        "Base Layer.FaceAnim_6", "Base Layer.FaceAnim_7", "Base Layer.FaceAnim_8"
    };

    [Tooltip("같은 얼굴끼리도 움직임이 겹치지 않도록 재생 시작 지점을 랜덤하게 합니다.")]
    [SerializeField] private bool randomizeStartTime = true;

    private Animator faceAnimator;

    private void Awake()
    {
        faceAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (faceAnimator.runtimeAnimatorController == null || faceStates == null || faceStates.Length == 0)
            return;

        int stateId = Animator.StringToHash(faceStates[Random.Range(0, faceStates.Length)]);
        if (!faceAnimator.HasState(0, stateId))
        {
            Debug.LogWarning("Start pea face state is missing from its Animator controller.", this);
            return;
        }

        // Enter the face directly so it is visible immediately, with no blank intro state.
        faceAnimator.Play(stateId, 0, randomizeStartTime ? Random.value : 0f);
        faceAnimator.Update(0f);
    }
}
