using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeGrowPeas.UI
{
    public enum SettingsEntry
    {
        Language, BreedPopup, MessageAlarm,
        Resolution, WindowMode, Quality, VSync, FrameRate,
        BgmVolume, EffectVolume,
        Skip, WaveSkip, Breed, Shovel, Scouter, Phone, ResetKeys,
        Codex, MainMenu, Quit, AddGold, EndlessMode
    }

    public sealed class SettingsDialogRow : MonoBehaviour, IPointerEnterHandler
    {
        public SettingsEntry entry;
        public TMP_Text label;
        public TMP_Dropdown dropdown;
        public Slider slider;
        public Button button;
        public TMP_Text valueText;
        private SettingsDialogView owner;

        public string LabelKey => LocKey.For(SettingsDialogDefinition.LocPrefix, entry.ToString(), "name");
        public string DescriptionKey => LocKey.For(SettingsDialogDefinition.LocPrefix, entry.ToString(), "description");
        public void Bind(SettingsDialogView view)
        {
            owner = view;
            if (dropdown != null) dropdown.onValueChanged.AddListener(v => owner.ChangeOption(this, v));
            if (slider != null) slider.onValueChanged.AddListener(v => owner.ChangeVolume(this, v));
            if (button != null) button.onClick.AddListener(() => owner.Activate(this));
        }
        public void OnPointerEnter(PointerEventData eventData) => owner?.Describe(this);
    }
}
