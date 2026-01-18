using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Messenger/Chat")]
public class Chat : ScriptableObject
{
    public ChatPartner chatPartner;
    public List<ChatMessage> messages;
}