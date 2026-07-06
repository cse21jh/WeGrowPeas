using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePopup : MonoBehaviour
{
    protected CanvasGroup canvasGroup;
    protected RectTransform rectTransform;

    public System.Action<BasePopup> OnPopupClosed;
    protected System.Action onCloseCallback;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public virtual void Open()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = Vector3.one;
    }

    /// 즉시 팝업을 닫고 매니저의 오브젝트 풀로 반환합니다.
    public virtual void Close()
    {
        canvasGroup.blocksRaycasts = false;

        // 버튼 클릭 효과음 재생
        SoundManager.Instance?.PlayEffect("Button");

        gameObject.SetActive(false);
        OnPopupClosed?.Invoke(this); // 오브젝트 풀에 반환
        onCloseCallback?.Invoke();
        onCloseCallback = null; // 재사용 시 잔여 콜백 오작동 방지
    }
}
