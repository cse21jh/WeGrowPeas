using UnityEngine;

[CreateAssetMenu(menuName = "Messenger/ChatPartner")]
public class ChatPartner : ScriptableObject
{
    public string chatPartnerName;
    public Sprite chatPartnerImage;
    // 필요하다면 다른 정보 추가 (예: 프로필 메시지)
}