using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특수 아이템 선택 UI.
/// - 미수령 선물이 있으면 화면에 "선물" 버튼이 계속 떠 있음 (수령 전까지 유지).
/// - 버튼 클릭 → 전체화면 3택 패널. 후보는 연 순간 롤되며, 닫았다 열어도 같은 후보 유지(리롤 방지).
/// </summary>
public class SpecialItemUIController : MonoBehaviour
{
    [Header("Gift Button (미수령 선물 있을 때 표시)")]
    [SerializeField] private GameObject giftButtonRoot;
    [SerializeField] private TMP_Text giftCountText;

    [Header("Choice Panel (전체화면 3택)")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button[] cardButtons;      // 카드 3개
    [SerializeField] private TMP_Text[] cardNames;
    [SerializeField] private TMP_Text[] cardDescs;
    [SerializeField] private Image[] cardIcons;

    private List<SpecialItemData> _candidates; // 이번 선물의 후보(선택/소진 전까지 고정)

    private void Update()
    {
        bool hasGift = SpecialItemSystem.PendingGifts > 0;
        if (giftButtonRoot != null && giftButtonRoot.activeSelf != hasGift)
            giftButtonRoot.SetActive(hasGift);
        if (hasGift && giftCountText != null)
            giftCountText.text = SpecialItemSystem.PendingGifts > 1 ? $"x{SpecialItemSystem.PendingGifts}" : "";
    }

    /// <summary>선물 버튼 클릭 → 3택 패널 열기.</summary>
    public void OpenChoicePanel()
    {
        if (SpecialItemSystem.PendingGifts <= 0) return;

        if (_candidates == null || _candidates.Count == 0)
        {
            string plant = GameManager.Instance != null ? GameManager.Instance.currentPlant : "완두콩";
            _candidates = SpecialItemSystem.RollCandidates(plant);
        }

        if (_candidates.Count == 0)
        {
            Debug.Log("[SpecialItem] 획득 가능한 아이템이 없습니다 (전부 보유)");
            return;
        }

        BuildCards();
        if (choicePanel != null) choicePanel.SetActive(true);
    }

    public void CloseChoicePanel()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        // 후보는 유지 — 다시 열면 같은 3개 (리롤 악용 방지)
    }

    private void BuildCards()
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool active = _candidates != null && i < _candidates.Count;
            if (cardButtons[i] != null) cardButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            var item = _candidates[i];
            if (cardNames != null && i < cardNames.Length && cardNames[i] != null)
                cardNames[i].text = item.displayName;
            if (cardDescs != null && i < cardDescs.Length && cardDescs[i] != null)
                cardDescs[i].text = item.description;
            if (cardIcons != null && i < cardIcons.Length && cardIcons[i] != null)
            {
                cardIcons[i].enabled = item.icon != null;
                if (item.icon != null) cardIcons[i].sprite = item.icon;
            }

            int idx = i;
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => Choose(idx));
        }
    }

    private void Choose(int index)
    {
        if (_candidates == null || index < 0 || index >= _candidates.Count) return;

        SpecialItemSystem.Acquire(_candidates[index]);
        _candidates = null; // 다음 선물은 새로 롤
        CloseChoicePanel();
        GameEvents.RequestSaveGame();
    }
}
