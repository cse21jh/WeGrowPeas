using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>치장 목록의 아이템 한 칸. 아이콘·이름·"사용중" 표시·잠금 표시.</summary>
public class CustomizeItemSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private GameObject selectedFrame;
    [Tooltip("보유하지 않았을 때 덮는 막(자물쇠 포함).")]
    [SerializeField] private GameObject lockedOverlay;
    [Tooltip("자물쇠 아래 해금 조건 글자(토큰 가격 / 업적).")]
    [SerializeField] private TMP_Text lockCaption;

    public CustomizeItemData Item { get; private set; }

    public void Setup(CustomizeItemData item, Action<CustomizeItemSlot> onClick)
    {
        Item = item;
        icon.sprite = item.icon;
        icon.enabled = item.icon != null;
        nameText.text = item.displayName;
        lockCaption.text = item.UnlockMethod == CustomizeUnlockMethod.Token
            ? $"({item.tokenPrice} G)"
            : "업적 달성 시 획득";
        SetEquipped(false);
        SetSelected(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void SetEquipped(bool on) => equippedBadge.SetActive(on);

    public void SetSelected(bool on) => selectedFrame.SetActive(on);

    /// <summary>보유하지 않은 아이템은 반투명한 막과 자물쇠를 덮어 잠긴 것으로 보여 준다.</summary>
    public void SetOwned(bool owned) => lockedOverlay.SetActive(!owned);
}
