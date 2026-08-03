using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 특수 아이템 선택 UI.
/// - 미수령 선물이 있으면 화면에 "선물" 버튼이 계속 떠 있음 (수령 전까지 유지).
/// - 버튼 클릭 → 전체화면 3택 패널. 후보는 연 순간 롤되며, 닫았다 열어도 같은 후보 유지(리롤 방지).
/// - 카드 한 장의 표시/버튼은 <see cref="SpecialItemCard"/>가 담당한다.
/// </summary>
public class SpecialItemUIController : MonoBehaviour
{
    [Header("Gift Button (미수령 선물 있을 때 표시)")]
    [SerializeField] private GameObject giftButtonRoot;
    [SerializeField] private TMP_Text giftCountText;

    [Header("Choice Panel (전체화면 3택)")]
    [SerializeField] private GameObject choicePanel;
    [Tooltip("선택지 카드들. 순서대로 후보가 배정된다.")]
    [SerializeField] private SpecialItemCard[] cards;

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
            _candidates = SpecialItemSystem.RollCandidates(CurrentPlant);

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
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;

            var item = (_candidates != null && i < _candidates.Count) ? _candidates[i] : null;
            int slot = i;

            cards[i].Bind(
                item,
                canReroll: SpecialItemSystem.CanRerollSlot(slot),
                onSelect: () => Choose(slot),
                onReroll: () => Reroll(slot));
        }
    }

    private void Choose(int slot)
    {
        if (_candidates == null || slot < 0 || slot >= _candidates.Count) return;

        SpecialItemSystem.Acquire(_candidates[slot]);
        _candidates = null; // 다음 선물은 새로 롤
        CloseChoicePanel();
        GameEvents.RequestSaveGame();
    }

    /// <summary>카드별 리롤 — 해당 자리만 다른 아이템으로 교체한다.</summary>
    private void Reroll(int slot)
    {
        if (SpecialItemSystem.PendingGifts <= 0) return;
        if (_candidates == null || slot < 0 || slot >= _candidates.Count) return;
        if (!SpecialItemSystem.CanRerollSlot(slot)) return;

        var replacement = SpecialItemSystem.RollReplacement(CurrentPlant, _candidates, slot);
        if (replacement == null)
        {
            Debug.Log("[SpecialItem] 교체할 다른 아이템이 없습니다.");
            return;
        }

        if (!SpecialItemSystem.UseSlotReroll(slot)) return;

        SoundManager.Instance?.PlayEffect("Button");
        _candidates[slot] = replacement;

        BuildCards();
        GameEvents.RequestSaveGame(); // 리롤 횟수 보존
    }

    private static string CurrentPlant =>
        GameManager.Instance != null ? GameManager.Instance.currentPlant : "완두콩";
}
