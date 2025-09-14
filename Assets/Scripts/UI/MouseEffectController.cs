using UnityEngine;
using UnityEngine.UI;

public class MouseEffectController : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image mouseWarningImage;
    [SerializeField] private Vector3 offset; // 마우스 위치 오프셋
    [SerializeField] private float minDistance = .5f; // 최소 거리 임계값

    [SerializeField] private Transform targetObject;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mouseWarningImage = GetComponent<Image>();
    }

    void Update()
    {
        // 마우스 위치를 그대로 RectTransform 위치로 적용
        rectTransform.position = Input.mousePosition + offset;
        rectTransform.rotation = RotationCalc();
        rectTransform.rotation = Quaternion.Euler(0, 0, rectTransform.rotation.eulerAngles.z + 90f); // 90도 회전 보정
    }

    private Quaternion RotationCalc()
    {
        if (targetObject == null)
        {
            mouseWarningImage.color = new Color(1, 1, 1, 0); // 투명하게
            return Quaternion.identity;
        }
        else
        {
            mouseWarningImage.color = new Color(1, 1, 1, 1); // 불투명하게
        }

        Vector3 mousePos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
        mouseWorldPos.z = 0f; // 2D 평면에서 Z축 고정

        Vector3 direction = mouseWorldPos - targetObject.position;  // 월드 좌표에서 방향 구하기
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if(direction.magnitude < minDistance) // 너무 가까우면 투명하게
        {
            mouseWarningImage.color = new Color(1, 1, 1, 0); // 투명하게
        }

        return Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void SetTarget(Transform bugTarget)
    {
        targetObject = bugTarget;
    }
}
