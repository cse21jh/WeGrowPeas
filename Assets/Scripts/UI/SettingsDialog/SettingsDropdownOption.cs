using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeGrowPeas.UI
{
    public sealed class SettingsDropdownOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image highlight;
        [SerializeField] private Image divider;
        [SerializeField] private Color selectedColor = new Color32(207, 148, 109, 255);
        [SerializeField] private Color hoverColor = new Color(1, 1, 1, 0.10f);
        private Toggle toggle;
        private bool hovered, focused;
        private void OnEnable() { toggle = GetComponent<Toggle>(); toggle.onValueChanged.AddListener(OnValueChanged); Refresh(); }
        private void OnDisable() { if (toggle != null) toggle.onValueChanged.RemoveListener(OnValueChanged); hovered = focused = false; }
        private void OnValueChanged(bool value) => Refresh();
        public void SetLast(bool last) { if (divider != null) divider.enabled = !last; }
        private void Refresh()
        {
            if (highlight == null || toggle == null) return;
            highlight.color = toggle.isOn ? selectedColor : hovered || focused ? hoverColor : Color.clear;
        }
        public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
        public void OnSelect(BaseEventData data) { focused = true; Refresh(); }
        public void OnDeselect(BaseEventData data) { focused = false; Refresh(); }
    }
}
