using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 회상 목록의 카드 하나. 그 런의 농장 사진과 날짜·일수를 보여준다.
/// 사진은 파일에서 읽은 런타임 텍스처라 <see cref="RawImage"/>로 붙인다.
/// </summary>
public class RecallListSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RawImage photo;
    [SerializeField] private GameObject noPhotoMark;
    [SerializeField] private TMP_Text dateLabel;
    [SerializeField] private TMP_Text infoLabel;

    /// <param name="texture">농장 사진. 없으면 null (사진 없이 기록만 남은 경우).</param>
    public void Setup(RecallIndexEntry entry, Texture2D texture, Action<string> onClick)
    {
        if (entry == null) return;

        if (photo != null)
        {
            photo.texture = texture;
            photo.enabled = texture != null;
        }
        if (noPhotoMark != null) noPhotoMark.SetActive(texture == null);

        if (dateLabel != null)
            dateLabel.text = DateTimeOffset.FromUnixTimeSeconds(entry.savedAtUnix)
                .ToLocalTime().ToString("yyyy.MM.dd HH:mm");

        if (infoLabel != null)
        {
            string info = $"{entry.day}일";
            if (!string.IsNullOrEmpty(entry.plantName)) info += $" · {entry.plantName}";
            if (entry.dawnStage > 0) info += $" · 승천 {entry.dawnStage}";
            infoLabel.text = info;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            string id = entry.id;
            button.onClick.AddListener(() => onClick?.Invoke(id));
        }
    }
}
