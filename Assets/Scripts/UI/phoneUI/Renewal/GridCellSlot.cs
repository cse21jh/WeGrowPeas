using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 밭 정보 팝업의 칸 하나(GridPrefab). 효과 아이콘 몇 개와 넘친 개수, 선택 표시를 관리한다.
///
/// 어떤 오브젝트를 쓸지는 인스펙터에서 지정한다. 예전에는 자식 이름으로 찾았는데,
/// 이름을 바꾸면 아무 경고 없이 그 부분만 비어 버려서 위험했다.
/// </summary>
public class GridCellSlot : MonoBehaviour
{
    [Tooltip("효과 아이콘. 겹쳐 놓은 순서대로 넣는다(앞쪽이 위에 보이는 것).")]
    [SerializeField] private Image[] icons;

    [Tooltip("아이콘 칸을 넘긴 효과 개수 표시")]
    [SerializeField] private TMP_Text amountText;

    [Tooltip("개수 표시의 그림자/외곽선용 사본. 없으면 비워둔다.")]
    [SerializeField] private TMP_Text amountUnderlayText;

    [SerializeField] private GameObject selectedFrame;
    [SerializeField] private Button button;

    /// <summary>보여줄 수 있는 아이콘 수. 인스펙터에 넣은 만큼이다.</summary>
    public int IconCapacity => icons != null ? icons.Length : 0;

    public void SetIcon(int slot, Sprite sprite, Color color)
    {
        if (icons == null || slot < 0 || slot >= icons.Length) return;

        Image image = icons[slot];
        if (image == null) return;

        image.sprite = sprite;
        image.color = color;
        image.gameObject.SetActive(sprite != null);
    }

    public void HideIcon(int slot)
    {
        if (icons == null || slot < 0 || slot >= icons.Length) return;
        if (icons[slot] != null) icons[slot].gameObject.SetActive(false);
    }

    /// <summary>아이콘 칸에 못 들어간 효과 수. 0이면 표시를 숨긴다.</summary>
    public void SetOverflow(int count)
    {
        string text = count > 0 ? $"{count}+" : "";

        SetTextValue(amountText, text);
        SetTextValue(amountUnderlayText, text);
    }

    public void SetSelected(bool on)
    {
        if (selectedFrame != null) selectedFrame.SetActive(on);
    }

    public void SetClick(Action onClick)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        if (onClick != null) button.onClick.AddListener(() => onClick());
    }

    private static void SetTextValue(TMP_Text text, string value)
    {
        if (text == null) return;

        text.text = value;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }
}
