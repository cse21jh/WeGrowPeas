using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프로필 선택 화면의 카드 1장. 데이터가 있으면 플레이 시간·마지막 저장 시각을, 없으면 "비어있음"을 보여준다.
/// 표시만 담당하고, 클릭 처리는 <see cref="ProfileSelectUI"/>가 넘겨준 콜백이 한다.
/// </summary>
public class ProfileCardUI : MonoBehaviour
{
    [SerializeField] private Button cardButton;
    [SerializeField] private TMP_Text titleText;

    [Header("데이터 있음")]
    [SerializeField] private GameObject filledGroup;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text updatedText;

    [Header("데이터 없음")]
    [SerializeField] private GameObject emptyGroup;

    [Header("기타")]
    [SerializeField] private Button deleteButton;
    [Tooltip("현재(마지막으로 플레이한) 프로필 표시")]
    [SerializeField] private GameObject currentMarker;

    public void Bind(ProfileSummary summary, bool isCurrent, Action onSelect, Action onDelete)
    {
        if (titleText != null) titleText.text = $"세이브 프로필 {summary.index + 1}";

        if (filledGroup != null) filledGroup.SetActive(summary.exists);
        if (emptyGroup != null) emptyGroup.SetActive(!summary.exists);

        if (summary.exists)
        {
            if (playTimeText != null) playTimeText.text = summary.PlayTimeText;
            if (updatedText != null) updatedText.text = FormatDate(summary.LastSavedLocal);
        }

        if (currentMarker != null) currentMarker.SetActive(isCurrent);

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => onSelect?.Invoke());
        }

        if (deleteButton != null)
        {
            // 비어 있는 프로필은 지울 게 없다.
            deleteButton.gameObject.SetActive(summary.exists);
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke());
        }
    }

    /// <summary>"2026년 9월 29일 오후 4:04"</summary>
    private static string FormatDate(DateTime t)
    {
        string ampm = t.Hour < 12 ? "오전" : "오후";
        int hour12 = t.Hour % 12 == 0 ? 12 : t.Hour % 12;
        return $"{t.Year}년 {t.Month}월 {t.Day}일 {ampm} {hour12}:{t.Minute:00}";
    }
}
