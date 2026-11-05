using UnityEngine;

[CreateAssetMenu(menuName = "Messenger/ChatPartner")]
[LocGroup(LocPrefix, "name")]
public class ChatPartner : ScriptableObject
{
    private const string LocPrefix = "chatpartner";

    /// <summary>화면에 보여줄 이름. 읽음 상태의 저장 키에는 <see cref="chatPartnerName"/>을 쓴다.</summary>
    public string LocalizedName => Loc.T(LocKey.For(LocPrefix, name, "name"));

    [Tooltip("읽음 상태의 저장 키. 표시 이름은 번역 표에서 고치고, 이 값은 바꾸지 않는다.")]
    public string chatPartnerName;
    public Sprite chatPartnerImage;
    // 필요하다면 다른 정보 추가 (예: 프로필 메시지)
}