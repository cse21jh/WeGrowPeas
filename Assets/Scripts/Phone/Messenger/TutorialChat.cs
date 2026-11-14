using UnityEngine;

[CreateAssetMenu(menuName = "Messenger/Tutorial Chat")]
[LocGroup(LocPrefix)]
public sealed class TutorialChat : Chat
{
    private const string LocPrefix = "tutorialchat";
    protected override string LocalizationPrefix => LocPrefix;
}
