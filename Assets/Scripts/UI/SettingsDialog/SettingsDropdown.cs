using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.UI
{
    /// <summary>Figma menu geometry with a seven-row viewport and selected-option scrolling.</summary>
    public sealed class SettingsDropdown : TMP_Dropdown
    {
        private GameObject list;
        private bool layoutPending;
        protected override GameObject CreateDropdownList(GameObject template)
        {
            list = base.CreateDropdownList(template); layoutPending = true;
            return list;
        }
        private void LateUpdate()
        {
            if (!layoutPending || list == null) return;
            layoutPending = false;
            var scroll = list.GetComponent<ScrollRect>();
            var options = list.GetComponentsInChildren<SettingsDropdownOption>();
            for (int i = 0; i < options.Length; i++) options[i].SetLast(i == options.Length - 1);
            if (scroll == null || options.Length == 0) return;
            const float rowHeight = 48, border = 5;
            var rect = (RectTransform)list.transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(7, options.Length) * rowHeight + border * 2);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, options.Length * rowHeight);
            for (int i = 0; i < options.Length; i++)
            {
                var row = (RectTransform)options[i].transform;
                row.anchorMin = row.anchorMax = row.pivot = new Vector2(0, 1);
                row.anchoredPosition = new Vector2(0, -i * rowHeight);
            }
            Canvas.ForceUpdateCanvases();
            float range = Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height);
            float offset = Mathf.Clamp(value * rowHeight - (scroll.viewport.rect.height - rowHeight) * 0.5f, 0, range);
            scroll.verticalNormalizedPosition = range > 0 ? 1 - offset / range : 1;
        }
    }
}
