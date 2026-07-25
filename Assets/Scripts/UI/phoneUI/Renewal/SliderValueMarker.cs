using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueMarker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private RectTransform markerRoot;
    [SerializeField] private RectTransform markerTextRect;
    [SerializeField] private TMP_Text markerText;
    [SerializeField] private RectTransform arrow;
    [SerializeField] private RectTransform limitRect;
    [SerializeField] private RectTransform sliderArea;

    [Header("Position")]
    [SerializeField] private float textDistance = 35f;
    [SerializeField] private float arrowDistance = 12f;
    [SerializeField] private float horizontalPadding = 8f;

    private readonly Vector3[] _corners = new Vector3[4];

    private void OnEnable()
    {
        slider.onValueChanged.AddListener(UpdateMarker);
    }

    private void Start()
    {
        UpdateMarker(slider.value);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(UpdateMarker);
    }


    private void UpdateMarker(float _)
    {
        float value = slider.normalizedValue;

        // 슬라이더 바 위에서의 X 위치
        float markerX = Mathf.Lerp(
            sliderArea.rect.xMin + horizontalPadding,
            sliderArea.rect.xMax - horizontalPadding,
            value
        );

        // MarkerRoot는 SliderArea의 자식이어야 함
        markerRoot.anchoredPosition = new Vector2(markerX, 0f);

        bool placeBelow = value < 0.5f;
        float direction = placeBelow ? -1f : 1f;

        // 화살표 위치
        arrow.anchoredPosition = new Vector2(
            0f,
            direction * arrowDistance
        );

        // 화살표 방향
        arrow.localRotation = Quaternion.Euler(
            0f,
            0f,
            placeBelow ? 180f : 0f
        );

        // 박스의 좌우 경계
        limitRect.GetWorldCorners(_corners);

        float left =
            sliderArea.InverseTransformPoint(_corners[0]).x
            + horizontalPadding;

        float right =
            sliderArea.InverseTransformPoint(_corners[2]).x
            - horizontalPadding;

        float halfTextWidth = markerTextRect.rect.width * 0.5f;

        // 텍스트 중심 위치를 박스 내부로 제한
        float textCenterX = Mathf.Clamp(
            markerX,
            left + halfTextWidth,
            right - halfTextWidth
        );

        markerTextRect.anchoredPosition = new Vector2(
            textCenterX - markerX,
            direction * textDistance
        );

        // 좌우 끝에서는 정렬 변경
        if (markerX <= left + halfTextWidth)
            markerText.alignment = TextAlignmentOptions.Left;
        else if (markerX >= right - halfTextWidth)
            markerText.alignment = TextAlignmentOptions.Right;
        else
            markerText.alignment = TextAlignmentOptions.Center;
    }
}
