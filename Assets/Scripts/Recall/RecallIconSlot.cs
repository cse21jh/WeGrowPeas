using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타임라인에서 쓰는 아이콘 한 칸. 밭 칸·아이템·저주가 모두 같은 슬롯을 쓴다.
/// 마우스를 올리면 설명을 넘겨준다.
/// </summary>
public class RecallIconSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    private string _description;
    private Action<string> _onHover;
    private Action _onHoverExit;

    /// <param name="count">0 이하면 숫자를 감춘다.</param>
    public void Setup(RecallLookup.Entry entry, int count, Action<string> onHover, Action onHoverExit)
    {
        bool hasIcon = entry.icon != null;

        if (icon != null)
        {
            icon.sprite = entry.icon;
            icon.enabled = hasIcon;
        }

        if (countText != null)
        {
            // 아이콘이 없는 항목(아직 그림이 없는 식물 등)은 이름 첫 글자로라도 구분되게 한다.
            string label = count > 0
                ? count.ToString()
                : (!hasIcon && !string.IsNullOrEmpty(entry.name) ? entry.name.Substring(0, 1) : "");

            countText.text = label;
            countText.gameObject.SetActive(!string.IsNullOrEmpty(label));
        }

        _description = string.IsNullOrEmpty(entry.description)
            ? entry.name
            : $"{entry.name}\n{entry.description}";

        _onHover = onHover;
        _onHoverExit = onHoverExit;
    }

    /// <summary>빈 밭 칸처럼 아이콘 없이 자리만 잡는 경우.</summary>
    public void SetupEmpty(string description, Action<string> onHover, Action onHoverExit)
    {
        if (icon != null) icon.enabled = false;
        if (countText != null) countText.gameObject.SetActive(false);

        _description = description;
        _onHover = onHover;
        _onHoverExit = onHoverExit;

        SetBackground(NormalColor);
    }

    /// <summary>그때 아직 넓히지 않았던 밭 칸. 자리는 보여주되 어둡게 깔아 둔다.</summary>
    public void SetupLocked(string description, Action<string> onHover, Action onHoverExit)
    {
        SetupEmpty(description, onHover, onHoverExit);
        SetBackground(LockedColor);
    }

    /// <summary>그날 새로 생긴 항목을 눈에 띄게 한다.</summary>
    public void SetHighlighted(bool on) => SetBackground(on ? HighlightColor : NormalColor);

    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color LockedColor = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.3f, 0.35f);

    private void SetBackground(Color c)
    {
        if (background != null) background.color = c;
    }

    public void OnPointerEnter(PointerEventData eventData) => _onHover?.Invoke(_description);
    public void OnPointerExit(PointerEventData eventData) => _onHoverExit?.Invoke();
}
