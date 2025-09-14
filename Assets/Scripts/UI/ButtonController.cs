using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonController : MonoBehaviour, ICursorHover, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform buttonRect;

    [Header("Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f; // 마우스 오버 시 크기
    [SerializeField] private bool enableShake = false; // 흔들림 효과 활성화 여부
    [Range(0f, 1f), SerializeField] private float shakeStrength = 1f; // 흔들리는 정도
    private Vector3 originalScale; // 원래 크기
    private Quaternion originalRotation;
    [SerializeField] private float animationDuration = 0.2f; // 애니메이션 지속 시간
    [SerializeField] private Ease easeType; // 애니메이션 이징

    [SerializeField] private RectTransform onMouseIcon;

    [SerializeField] private CursorType cursorType = CursorType.Clickable;

    public CursorType GetCursorType() => cursorType;


    void Start()
    {
        buttonRect = GetComponent<RectTransform>();
        originalRotation = buttonRect.rotation;
        originalScale = buttonRect.localScale; // 원래 크기 저장
    }


    private void OnDisable()
    {
        if (buttonRect != null)
        {
            // 버튼이 비활성화될 때 원래 상태로 복원
            buttonRect.DOKill(); // 기존 애니메이션 초기화
            buttonRect.localScale = originalScale;
            buttonRect.rotation = originalRotation;
            if (onMouseIcon != null)
            {
                onMouseIcon.gameObject.SetActive(false);
                onMouseIcon.localScale = new Vector3(0, 0, 0);
            }
        }
    }


    public void Click()
    {
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 버튼 위에 올려졌을 때
        CursorManager.Instance.SetCursor(cursorType);


        buttonRect.DOKill(); // 기존 애니메이션 초기화
        buttonRect.DOScale(hoverScale, animationDuration).SetEase(easeType).SetUpdate(true);
        //buttonRect.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), animationDuration).SetEase(easeType).SetUpdate(true);

        if (enableShake)
        {
            buttonRect.DOShakeRotation(animationDuration, 10 * shakeStrength, (int)(50 * shakeStrength), 90, false).SetUpdate(true);
        }

        if(onMouseIcon != null)
        {
            onMouseIcon.gameObject.SetActive(true);
            onMouseIcon.DOScale(hoverScale, animationDuration).SetEase(easeType).SetUpdate(true);
        }

        //buttonRect.DOShakeRotation(animationDuration, 10 * shakeStrength, (int)(50 * shakeStrength), 90, false).SetUpdate(true);
        buttonRect.rotation = originalRotation;
    }


    private void OnMouseOver()
    {
        CursorManager.Instance.SetCursor(cursorType);
        if (Input.GetMouseButton(0))
        {
            CursorManager.Instance.SetCursor(CursorType.Clicked);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 버튼을 떠났을 때
        CursorManager.Instance.SetCursor(CursorType.Default);
        buttonRect.DOKill(); // 기존 애니메이션 초기화
        buttonRect.DOScale(originalScale, animationDuration).SetEase(easeType).SetUpdate(true);

        if (onMouseIcon != null)
        {
            onMouseIcon.gameObject.SetActive(false);
            onMouseIcon.DOScale(new Vector3(0,0,0), animationDuration).SetEase(easeType).SetUpdate(true);
        }

        //buttonRect.DOShakeRotation(animationDuration, 10 * shakeStrength, (int)(50 * shakeStrength), 90, false).SetUpdate(true);
        buttonRect.rotation = originalRotation;
    }
}
