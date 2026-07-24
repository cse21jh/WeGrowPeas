using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>도감 좌측 목록의 한 항목. 미발견이면 ??? 로 표시.</summary>
public class CodexListSlot : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;

    public void Setup(CodexEntry entry, int index, Action<int> onClick)
    {
        // 잠김(해금 안 됨) → "잠김", 해금됐지만 미발견 → "???", 발견 → 이름
        if (label != null)
            label.text = entry.locked ? "잠김" : (entry.discovered ? entry.displayName : "???");

        if (icon != null)
        {
            bool show = !entry.locked && entry.discovered && entry.icon != null;
            icon.enabled = show;
            if (show) icon.sprite = entry.icon;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(index));
        }
    }
}
