using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Messenger/Chat")]
[LocGroup(LocPrefix)]
public class Chat : ScriptableObject, ILocListSource
{
    private const string LocPrefix = "chat";

    public ChatPartner chatPartner;
    public List<ChatMessage> messages;

    public bool useSaveSlotReadStateOnly;

    /// <summary>화면에 보여줄 메시지 문구.</summary>
    public string GetText(ChatMessage message)
        => Loc.T(LocKey.For(LocPrefix, name, message.locId));

    public IEnumerable<string> GetLocIds() => messages.Where(m => m != null).Select(m => m.locId);

    public bool EnsureLocIds() => LocKey.EnsureUniqueIds(messages, m => m.locId, (m, id) => m.locId = id);
}
