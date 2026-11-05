using UnityEngine;

[CreateAssetMenu(menuName = "Messenger/ChatPartner")]
[LocGroup(LocPrefix)]
public class ChatPartner : ScriptableObject
{
    private const string LocPrefix = "chatpartner";

    /// <summary>화면에 보여줄 이름. 읽음 상태의 저장 키에는 <see cref="chatPartnerName"/>을 쓴다.</summary>
    public string LocalizedName => Loc.Or(LocKey.For(LocPrefix, name, "name"), chatPartnerName);

    [LocText("name")] public string chatPartnerName;
    public Sprite chatPartnerImage;
    // 필요하다면 다른 정보 추가 (예: 프로필 메시지)
}