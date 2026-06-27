using UnityEngine;
using UnityEngine.UI;

public class MouseEffectController : MonoBehaviour
{
    private static readonly Color OpaqueWhite = new Color(1, 1, 1, 1);
    private static readonly Color TransparentWhite = new Color(1, 1, 1, 0);

    private RectTransform rectTransform;
    private Image mouseWarningImage;
    private Camera mainCamera;
    private Color lastAppliedColor;

    [SerializeField] private Vector3 offset;
    [SerializeField] private float minDistance = .5f;
    [SerializeField] private Transform targetObject;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mouseWarningImage = GetComponent<Image>();
        mainCamera = Camera.main;
        lastAppliedColor = mouseWarningImage.color;
    }

    void Update()
    {
        rectTransform.position = Input.mousePosition + offset;
        float z = RotationCalcZ();
        rectTransform.rotation = Quaternion.Euler(0f, 0f, z + 90f);
    }

    private float RotationCalcZ()
    {
        if (targetObject == null)
        {
            ApplyColor(TransparentWhite);
            return 0f;
        }

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            ApplyColor(TransparentWhite);
            return 0f;
        }

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 targetPos = targetObject.position;
        float dx = mouseWorldPos.x - targetPos.x;
        float dy = mouseWorldPos.y - targetPos.y;

        if ((dx * dx + dy * dy) < (minDistance * minDistance))
        {
            ApplyColor(TransparentWhite);
        }
        else
        {
            ApplyColor(OpaqueWhite);
        }

        return Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
    }

    private void ApplyColor(Color c)
    {
        if (lastAppliedColor == c) return;
        mouseWarningImage.color = c;
        lastAppliedColor = c;
    }

    public void SetTarget(Transform bugTarget)
    {
        targetObject = bugTarget;
    }
}
