using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform buttonRect;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f; // 마우스 오버 시 크기
    [Range(0f, 1f), SerializeField] private float shakeStrength = 1f; // 흔들리는 정도
    private Vector3 originalScale; // 원래 크기
    private Quaternion originalRotation;
    [SerializeField] private float animationDuration = 0.2f; // 애니메이션 지속 시간
    [SerializeField] private Ease easeType; // 애니메이션 이징


    void Start()
    {
        buttonRect = GetComponent<RectTransform>();
        originalRotation = buttonRect.rotation;
        originalScale = buttonRect.localScale; // 원래 크기 저장
    }


    public void Click()
    {
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 버튼 위에 올려졌을 때


        buttonRect.DOKill(); // 기존 애니메이션 초기화
        buttonRect.DOScale(hoverScale, animationDuration).SetEase(easeType).SetUpdate(true);
        //buttonRect.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), animationDuration).SetEase(easeType).SetUpdate(true);
        //buttonRect.DOShakeRotation(animationDuration, 10 * shakeStrength, (int)(50 * shakeStrength), 90, false).SetUpdate(true);
        buttonRect.rotation = originalRotation;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 버튼을 떠났을 때
        buttonRect.DOKill(); // 기존 애니메이션 초기화
        buttonRect.DOScale(originalScale, animationDuration).SetEase(easeType).SetUpdate(true);
        //buttonRect.DOShakeRotation(animationDuration, 10 * shakeStrength, (int)(50 * shakeStrength), 90, false).SetUpdate(true);
        buttonRect.rotation = originalRotation;
    }
}
