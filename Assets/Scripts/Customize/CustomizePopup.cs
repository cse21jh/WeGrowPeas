using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치장 화면의 간단한 확인 창. 메시지 + 확인 버튼(+ 취소 버튼).
/// 구매 확인, 토큰 부족 안내, 업적 해금 안내에 쓴다.
/// </summary>
public class CustomizePopup : MonoBehaviour
{
    [Tooltip("켜고 끌 창 전체(어두운 배경 포함).")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmLabel;
    [SerializeField] private Button cancelButton;

    private Action _onConfirm;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
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
    }

    private void OnConfirm()
    {
        var callback = _onConfirm;
        Hide();
        callback?.Invoke();
    }
}
