using UnityEngine;

public class LoopController : MonoBehaviour
{
    [Header("Loop Settings")]
    public float bounds = 10f;          // 이동 가능한 범위
    [Range(0f, 10f)] public float speed = 2f;            // 기본 이동 속도
    public bool randomizeSpeed = false; // 시작 시 랜덤 속도 여부

    private float moveSpeed;

    void Start()
    {
        // 시작 시 속도 지정
        moveSpeed = randomizeSpeed ? Random.Range(0, speed) : speed;
        // 랜덤 속도일 때 0이 나오면 멈출 수 있으니 보정
        if (Mathf.Approximately(moveSpeed, 0f))
            moveSpeed = speed;
    }

    void Update()
    {
        // 이동
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);

        // 범위 체크 후 루프 처리
        if (transform.position.x > bounds)
        {
            transform.position = new Vector2(-bounds, transform.position.y);
        }
        else if (transform.position.x < -bounds)
        {
            transform.position = new Vector2(bounds, transform.position.y);
        }
    }
}
