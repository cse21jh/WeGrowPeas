using UnityEngine;

/// <summary>
/// 로딩 스피너 회전. Time.timeScale이 0이어도 돌아가도록 unscaledDeltaTime을 쓴다.
/// </summary>
public class SpinnerRotator : MonoBehaviour
{
    [Tooltip("초당 회전 각도. 양수 = 반시계, 음수 = 시계 방향")]
    [SerializeField] private float degreesPerSecond = -180f;

    [Tooltip("체크 시 매끄럽게 돌지 않고 단계별로 끊어서 회전(트로버 느낌)")]
    [SerializeField] private bool stepped = false;

    [Tooltip("stepped일 때 한 바퀴를 몇 단계로 나눌지")]
    [SerializeField] private int steps = 12;

    private float angle;

    private void OnEnable()
    {
        angle = 0f;
    }

    private void Update()
    {
        angle += degreesPerSecond * Time.unscaledDeltaTime;
        angle %= 360f;

        float applied = stepped && steps > 0
            ? Mathf.Floor(angle / (360f / steps)) * (360f / steps)
            : angle;

        transform.localRotation = Quaternion.Euler(0f, 0f, applied);
    }
}
