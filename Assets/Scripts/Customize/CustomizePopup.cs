using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치장 화면의 간단한 확인 창. 메시지 + 확인 버튼(+ 취소 버튼).
/// 구매 확인, 토큰 부족 안내, 업적 해금 안내, 저장하지 않고 나갈 때 확인에 쓴다.
/// </summary>
public class CustomizePopup : MonoBehaviour
{
    [Tooltip("켜고 끌 창 전체(어두운 배경 포함).")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmLabel;
    [SerializeField] private Button cancelButton;

    [Header("크기")]
    [Tooltip("글이 길면 높이를 늘릴 창 상자. 비우면 크기를 바꾸지 않는다.")]
    [SerializeField] private RectTransform box;
    [Tooltip("창이 커질 수 있는 최대 높이.")]
    [SerializeField] private float maxBoxHeight = 320f;

    private Action _onConfirm;
    private float _baseBoxHeight;
    private float _baseMessageHeight;

    /// <summary>창이 떠 있는가.</summary>
    public bool IsOpen => root.activeSelf;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);

        // 씬에 놓인 크기를 기본(최소) 크기로 쓴다.
        if (box != null)
        {
            _baseBoxHeight = box.sizeDelta.y;
            _baseMessageHeight = messageText.rectTransform.rect.height;
        }
        Hide();
    }

    /// <summary>확인·취소 두 버튼으로 묻는다. 확인을 누르면 창을 닫고 onConfirm을 부른다.</summary>
    public void Ask(string message, string confirmText, Action onConfirm)
    {
        Open(message, confirmText, onConfirm, showCancel: true);
    }

    /// <summary>확인 버튼 하나로 알리기만 한다.</summary>
    public void Notice(string message)
    {
        Open(message, "확인", null, showCancel: false);
    }

    public void Hide()
    {
        _onConfirm = null;
        root.SetActive(false);
    }

    private void Open(string message, string confirmText, Action onConfirm, bool showCancel)
    {
        messageText.text = message;
        confirmLabel.text = confirmText;
        _onConfirm = onConfirm;
        cancelButton.gameObject.SetActive(showCancel);
        root.SetActive(true);
        FitBoxToMessage();
    }

    /// <summary>
    /// 글이 기본 크기 칸에 다 들어가지 않으면 창 높이를 늘린다(최대 <see cref="maxBoxHeight"/>).
    /// 글자 크기는 최대 크기 기준으로 재서, 자동 크기 조절로 글자가 작아지기 전에 창이 먼저 커지게 한다.
    /// </summary>
    private void FitBoxToMessage()
    {
        if (box == null) return;

        float width = messageText.rectTransform.rect.width;
        float fontSize = messageText.enableAutoSizing ? messageText.fontSizeMax : messageText.fontSize;
        bool auto = messageText.enableAutoSizing;
        messageText.enableAutoSizing = false;
        float original = messageText.fontSize;
        messageText.fontSize = fontSize;
        float needed = messageText.GetPreferredValues(messageText.text, width, 0f).y;
        messageText.fontSize = original;
        messageText.enableAutoSizing = auto;

        float extra = Mathf.Max(0f, needed - _baseMessageHeight);
        float height = Mathf.Min(_baseBoxHeight + extra, Mathf.Max(_baseBoxHeight, maxBoxHeight));
        box.sizeDelta = new Vector2(box.sizeDelta.x, height);
    }

    private void OnConfirm()
    {
        var callback = _onConfirm;
        Hide();
        callback?.Invoke();
    }
}
