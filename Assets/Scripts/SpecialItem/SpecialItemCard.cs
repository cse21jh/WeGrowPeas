using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특수 아이템 선택지 카드 한 장.
/// 카드 오브젝트(프리팹)에 붙이고, 아이콘·이름·설명·선택 버튼·리롤 버튼을 자기 안에서 관리한다.
/// </summary>
public class SpecialItemCard : MonoBehaviour
{
    [Header("Card")]
    [SerializeField] private Button selectButton;   // 카드 전체(또는 선택 버튼)
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    [Header("Reroll")]
    [SerializeField] private Button rerollButton;   // 이 카드만 다시 뽑기

    /// <summary>이 카드가 표시 중인 아이템.</summary>
    public SpecialItemData Data { get; private set; }

    /// <summary>
    /// 카드 내용을 채우고 콜백을 연결한다.
    /// </summary>
    /// <param name="item">표시할 아이템</param>
    /// <param name="canReroll">이 카드의 리롤이 남아 있는가</param>
    /// <param name="onSelect">카드를 골랐을 때</param>
    /// <param name="onReroll">이 카드를 다시 뽑을 때</param>
    public void Bind(SpecialItemData item, bool canReroll, Action onSelect, Action onReroll)
    {
        Data = item;

        bool hasItem = item != null;
        gameObject.SetActive(hasItem);
        if (!hasItem) return;

        if (nameText != null) nameText.text = item.displayName;
        if (descText != null) descText.text = item.description;
        if (icon != null)
        {
            icon.enabled = item.icon != null;
            if (item.icon != null) icon.sprite = item.icon;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke());
        }

        if (rerollButton != null)
        {
            rerollButton.interactable = canReroll; // 소진되면 비활성화
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
        }
    }

    /// <summary>리롤 가능 여부만 갱신.</summary>
    public void SetRerollAvailable(bool canReroll)
    {
        if (rerollButton != null) rerollButton.interactable = canReroll;
    }

    public void Hide() => gameObject.SetActive(false);
}
