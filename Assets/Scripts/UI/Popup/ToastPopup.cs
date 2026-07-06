using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ToastPopup : BasePopup
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image popupImage;

    private Sequence autoCloseSequence;

    /// 토스트 팝업의 데이터를 설정하고 페이드아웃 타이머를 시작합니다.
    public void SetupAndPlay(string title = null, string content = null, Sprite sprite = null, float delay = 2.0f, System.Action onClose = null)
    {
        if (titleText != null)
        {
            if (!string.IsNullOrEmpty(title))
            {
                titleText.gameObject.SetActive(true);
                titleText.text = title;
            }
            else
            {
                titleText.gameObject.SetActive(false);
            }
        }

        if (contentText != null)
        {
            if (!string.IsNullOrEmpty(content))
            {
                contentText.gameObject.SetActive(true);
                contentText.text = content;
            }
            else
            {
                contentText.gameObject.SetActive(false);
            }
        }
        onCloseCallback = onClose;

        if (popupImage != null)
        {
            if (sprite != null)
            {
                popupImage.gameObject.SetActive(true);
                popupImage.sprite = sprite;
            }
            else
            {
                popupImage.gameObject.SetActive(false);
            }
        }

        base.Open();

        // 중복 실행 방지를 위해 실행 중인 시퀀스가 있다면 중단
        if (autoCloseSequence != null && autoCloseSequence.IsActive())
        {
            autoCloseSequence.Kill();
        }

        // DOTween 시퀀스를 사용하여 가비지 없이 대기 후 닫기 실행
        autoCloseSequence = DOTween.Sequence()
            .AppendInterval(delay)
            .OnComplete(Close)
            .SetUpdate(true); // timeScale의 영향을 받지 않음
    }

    /// 페이드아웃 애니메이션과 함께 토스트 팝업을 닫고 매니저의 오브젝트 풀로 반환합니다.
    public override void Close()
    {
        canvasGroup.blocksRaycasts = false;

        SoundManager.Instance?.PlayEffect("Button");
        canvasGroup.DOFade(0f, 0.5f).SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnPopupClosed?.Invoke(this); // 오브젝트 풀에 반환
                onCloseCallback?.Invoke();
                onCloseCallback = null; // 재사용 시 잔여 콜백 오작동 방지
            });
    }

    private void OnDisable()
    {
        if (autoCloseSequence != null && autoCloseSequence.IsActive())
        {
            autoCloseSequence.Kill();
        }
    }
}
