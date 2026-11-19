using UnityEngine;

namespace WeGrowPeas.UI
{
    /// <summary>One setting's stable localization identity; text lives only in the shared CSV table.</summary>
    [LocGroup(LocPrefix, "name", "description")]
    [CreateAssetMenu(menuName = "WeGrowPeas/UI/Settings Description", fileName = "SettingsDescription")]
    public sealed class SettingsDialogDefinition : ScriptableObject, ILocKeyed
    {
        public const string LocPrefix = "settings-dialog";
        [SerializeField] private SettingsEntry entry;
        public SettingsEntry Entry => entry;
        public string LocId => entry.ToString();
        public string LabelKey => LocKey.For(LocPrefix, LocId, "name");
        public string DescriptionKey => LocKey.For(LocPrefix, LocId, "description");
        public string Label => Loc.T(LabelKey);
        public string Description => Loc.T(DescriptionKey);
    }
}
