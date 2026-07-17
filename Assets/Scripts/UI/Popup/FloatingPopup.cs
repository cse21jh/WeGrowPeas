using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingPopup : BasePopup
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI contentText;

    private Sequence autoCloseSequence;

    public void SetupAndPlay(string text, float delay = 2.0f, System.Action onClose = null)
    {
        onCloseCallback = onClose;

        // 프리팹이 비활성화 상태로 Instantiate 될 경우 Awake가 실행되지 않아 null인 상태 방지
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (contentText != null)
        {
            contentText.text = text;
        }

        // 중복 실행 방지를 위해 실행 중인 시퀀스 및 진행 중인 페이드 트윈 정리
        if (autoCloseSequence != null && autoCloseSequence.IsActive())
        {
            autoCloseSequence.Kill();
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        gameObject.SetActive(true);

        // DOTween 시퀀스를 사용하여 delay초 대기 후 페이드아웃 및 닫기 처리 실행
        Debug.Log($"[FloatingPopup] SetupAndPlay called. text: {text}, delay: {delay}");
        autoCloseSequence = DOTween.Sequence()
            .AppendInterval(delay)
            .OnComplete(() => {
                Debug.Log("[FloatingPopup] Interval completed. Starting FadeOut.");
                FadeOutAndClose(0.5f);
            })
            .SetUpdate(true); // timeScale의 영향을 받지 않음
    }

    private void FadeOutAndClose(float fadeDuration)
    {
        Debug.Log($"[FloatingPopup] FadeOutAndClose called. duration: {fadeDuration}");
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    Debug.Log("[FloatingPopup] FadeOut completed. Deactivating.");
                    gameObject.SetActive(false);
                    OnPopupClosed?.Invoke(this); // 이벤트 발생
                    onCloseCallback?.Invoke();
                    onCloseCallback = null;
                });
        }
        else
        {
            Debug.LogWarning("[FloatingPopup] canvasGroup is null in FadeOutAndClose!");
            gameObject.SetActive(false);
            OnPopupClosed?.Invoke(this);
            onCloseCallback?.Invoke();
            onCloseCallback = null;
        }
    }

    /// 즉시 토스트 팝업을 닫고 비활성화합니다.
    public override void Close()
    {
        if (autoCloseSequence != null && autoCloseSequence.IsActive())
        {
            autoCloseSequence.Kill();
        }

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
        }

        SoundManager.Instance?.PlayEffect("Button");

        gameObject.SetActive(false);
        OnPopupClosed?.Invoke(this);
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }

    private void OnDisable()
    {
        if (autoCloseSequence != null && autoCloseSequence.IsActive())
        {
            autoCloseSequence.Kill();
        }
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
    }
}
