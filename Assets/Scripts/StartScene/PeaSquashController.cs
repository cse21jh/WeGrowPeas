using UnityEngine;

/// <summary>Deforms the pea's visuals on impact without changing its physics shape.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PeaSquashController : MonoBehaviour
{
    [Header("Visual hierarchy")]
    [SerializeField] private Transform squashAxis;
    [SerializeField] private Transform visual;

    [Header("Softness")]
    [Tooltip("충돌 속도에 따른 눌림 강도")]
    [SerializeField, Min(0f)] private float impactStrength = 0.09f;
    [Tooltip("최대로 납작해지는 비율. 0.28이면 충돌 방향으로 최대 28% 줄어듭니다.")]
    [SerializeField, Range(0f, 0.5f)] private float maxCompression = 0.28f;
    [Tooltip("복원 진동 속도(Hz). 낮출수록 천천히 말랑하게 돌아옵니다.")]
    [SerializeField, Range(0.5f, 8f)] private float springFrequency = 2.8f;
    [Tooltip("잔떨림을 줄이는 정도. 높일수록 덜 출렁이며 돌아옵니다.")]
    [SerializeField, Range(0.1f, 0.95f)] private float dampingRatio = 0.55f;
    [Tooltip("작은 접촉에는 반응하지 않도록 하는 최소 충돌 속도")]
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.3f;

    [Header("Drag stretch")]
    [Tooltip("잡아당길 때 커서 방향으로 늘어나는 최대 비율")]
    [SerializeField, Range(0f, 0.35f)] private float maxDragStretch = 0.2f;

    private Vector3 restScale;
    private Quaternion restAxisRotation;
    private Quaternion restVisualRotation;
    private float compression;
    private float compressionVelocity;
    private float pendingImpactSpeed;
    private Vector2 pendingImpactNormal;
    private float axisAngle;
    private float targetAxisAngle;
    private float nextImpactTime;
    private bool isDeforming;
    private bool initialized;
    private bool isDragging;
    private float dragStretch;

    private void Awake()
    {
        if (squashAxis == null || visual == null || squashAxis == transform ||
            !visual.IsChildOf(squashAxis))
        {
            enabled = false;
            return;
        }

        restScale = squashAxis.localScale;
        restAxisRotation = squashAxis.localRotation;
        restVisualRotation = visual.localRotation;
        initialized = true;
    }

    private void OnCollisionEnter2D(Collision2D collision) => CollectImpact(collision);
    private void OnCollisionStay2D(Collision2D collision) => CollectImpact(collision);

    public void SetDragPull(Vector2 worldDirection, float amount)
    {
        if (!initialized || !isActiveAndEnabled) return;
        if (!isDragging)
            compressionVelocity -= Mathf.Max(0.5f, springFrequency) * 0.25f;
        isDragging = true;
        float maximum = Mathf.Max(0f, maxDragStretch);
        dragStretch = Mathf.Lerp(Mathf.Min(0.04f, maximum), maximum, Mathf.Clamp01(amount));
        if (worldDirection.sqrMagnitude > 0.00001f)
            SetDeformationAxis(Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg);
        isDeforming = true;
    }

    public void ReleaseDrag()
    {
        isDragging = false;
        dragStretch = 0f;
    }

    private void SetDeformationAxis(float angle)
    {
        if (!isDeforming) axisAngle = angle;
        // Opposite directions describe the same squash/stretch axis.
        float difference = Mathf.DeltaAngle(axisAngle, angle);
        if (difference > 90f) difference -= 180f;
        if (difference < -90f) difference += 180f;
        targetAxisAngle = axisAngle + difference;
    }

    private void CollectImpact(Collision2D collision)
    {
        if (!isActiveAndEnabled || !initialized || isDragging) return;

        // Use the strongest normal impact, not sliding speed or static contact pressure.
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            float speed = Mathf.Abs(Vector2.Dot(collision.relativeVelocity, normal));
            if (speed <= minimumImpactSpeed || speed <= pendingImpactSpeed) continue;
            pendingImpactSpeed = speed;
            pendingImpactNormal = normal;
        }
    }

    private void LateUpdate()
    {
        if (!initialized || Time.deltaTime <= 0f) return;

        float omega = 2f * Mathf.PI * Mathf.Max(0.5f, springFrequency);
        float limit = Mathf.Clamp(maxCompression, 0f, 0.5f);
        if (!isDragging && pendingImpactSpeed > minimumImpactSpeed && Time.time >= nextImpactTime && limit > 0f)
        {
            float amount = Mathf.Min(limit, (pendingImpactSpeed - minimumImpactSpeed) * Mathf.Max(0f, impactStrength));
            compressionVelocity = Mathf.Min(compressionVelocity + amount * omega * 2f, limit * omega * 2f);
            float impactAngle = Mathf.Atan2(pendingImpactNormal.y, pendingImpactNormal.x) * Mathf.Rad2Deg;
            SetDeformationAxis(impactAngle);
            nextImpactTime = Time.time + 0.06f;
            isDeforming = true;
        }
        pendingImpactSpeed = 0f;

        if (!isDeforming) return;
        float dt = Time.deltaTime;
        float damping = Mathf.Clamp(dampingRatio, 0.1f, 0.95f);
        float decay = damping * omega;
        float dampedOmega = omega * Mathf.Sqrt(1f - damping * damping);
        float sin = Mathf.Sin(dampedOmega * dt);
        float cos = Mathf.Cos(dampedOmega * dt);
        float envelope = Mathf.Exp(-decay * dt);
        float targetCompression = isDragging ? -dragStretch : 0f;
        float previousCompression = compression - targetCompression;

        // Exact damped-spring integration remains stable even during a slow frame.
        compression = targetCompression + envelope * (previousCompression * cos +
            (compressionVelocity + decay * previousCompression) / dampedOmega * sin);
        compressionVelocity = envelope * (compressionVelocity * cos -
            (decay * compressionVelocity + omega * omega * previousCompression) / dampedOmega * sin);

        if (compression > limit)
        {
            compression = limit;
            compressionVelocity = Mathf.Min(0f, compressionVelocity);
        }
        else if (compression < -Mathf.Max(limit * 0.35f, maxDragStretch * 1.25f))
        {
            compression = -Mathf.Max(limit * 0.35f, maxDragStretch * 1.25f);
            compressionVelocity = Mathf.Max(0f, compressionVelocity);
        }

        if (!isDragging && Mathf.Abs(compression) < 0.0001f && Mathf.Abs(compressionVelocity) < 0.001f)
        {
            ResetVisual();
            return;
        }

        axisAngle = Mathf.LerpAngle(axisAngle, targetAxisAngle, 1f - Mathf.Exp(-20f * dt));
        float localAngle = axisAngle - transform.eulerAngles.z - 90f;
        float squash = 1f - compression;
        squashAxis.localRotation = Quaternion.Euler(0f, 0f, localAngle);
        squashAxis.localScale = new Vector3(restScale.x / squash, restScale.y * squash, restScale.z);
        // Counter-rotation preserves the artwork's orientation while the full mask stack deforms.
        visual.localRotation = Quaternion.Euler(0f, 0f, -localAngle) * restVisualRotation;
    }

    private void OnDisable()
    {
        ReleaseDrag();
        pendingImpactSpeed = 0f;
        nextImpactTime = 0f;
        ResetVisual();
    }

    private void ResetVisual()
    {
        compression = 0f;
        compressionVelocity = 0f;
        isDeforming = false;
        if (!initialized) return;
        if (squashAxis != null)
        {
            squashAxis.localScale = restScale;
            squashAxis.localRotation = restAxisRotation;
        }
        if (visual != null) visual.localRotation = restVisualRotation;
    }
}
