using UnityEngine;

public class UIElementSway : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float positionAmplitude = 4f;
    [SerializeField] private float rotationAmplitude = 1.5f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private float phase = 0f;

    private Vector2 baseAnchoredPosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        baseAnchoredPosition = target.anchoredPosition;
        baseRotation = target.localRotation;
    }

    private void Update()
    {
        float t = Time.time * speed + phase;
        float sway = Mathf.Sin(t);

        target.anchoredPosition = baseAnchoredPosition + new Vector2(
            sway * positionAmplitude,
            0f
        );

        target.localRotation = baseRotation * Quaternion.Euler(
            0f,
            0f,
            sway * rotationAmplitude
        );
    }
}
