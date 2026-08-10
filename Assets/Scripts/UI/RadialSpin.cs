using UnityEngine;

public class RadialSpin : MonoBehaviour
{
    public float spinDuration = 2f;   // 한 바퀴 도는 데 걸리는 시간 (2초)
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    public float scaleSpeed = 2f;     // 크기 변화 속도

    void Update()
    {
        // 1. 회전 (2초에 한 번)
        float spinSpeed = 360f / spinDuration; // 2초에 360도
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

        // 2. 크기 변화 (sin으로 부드럽게 왕복)
        float t = (Mathf.Sin(Time.time * scaleSpeed) + 1f) / 2f; // 0~1 사이 값
        float scaleFactor = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = Vector3.one * scaleFactor;
    }
}
