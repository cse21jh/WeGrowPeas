using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CloseablePopup : BasePopup
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI maxTitleText;
    [SerializeField] private TextMeshProUGUI minTitleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image popupImage;
    [SerializeField] private Button maxCloseButton;
    [SerializeField] private Button minCloseButton;

    protected override void Awake()
    {
        base.Awake();
        if (maxCloseButton != null && minCloseButton)
        {
            maxCloseButton.onClick.AddListener(Close);
            minCloseButton.onClick.AddListener(Close);
        }
    }

    /// 팝업의 본문 텍스트 및 이미지를 설정하고, 닫힐 때 실행할 콜백을 지정합니다.
    public void Setup(string title = null, string content = null, Sprite sprite = null, System.Action onClose = null)
    {
        if (maxTitleText != null && minTitleText != null)
        {
            if (!string.IsNullOrEmpty(title))
            {
                maxTitleText.gameObject.SetActive(true);
                maxTitleText.text = title;
                minTitleText.gameObject.SetActive(true);
                minTitleText.text = title;
            }
            else
            {
                maxTitleText.gameObject.SetActive(false);
                minTitleText.gameObject.SetActive(false);
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
    }
}
