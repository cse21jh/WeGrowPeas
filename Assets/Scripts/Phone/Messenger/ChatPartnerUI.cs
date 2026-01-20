using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시

public class ChatPartnerUI : MonoBehaviour
{
    [SerializeField] private Image chatPartnerImage;
    [SerializeField] private TMP_Text chatPartnerNameText;
    [SerializeField] private TMP_Text lastMessageText;

    [SerializeField] private GameObject mandatoryunreadIndicator; // 필수 메시지 알림
    [SerializeField] private GameObject notMandatoryUnreadIndicator; // 선택 메시지 알림

    private Chat chat;
    private MessengerApp messengerApp;

    // *** 변경점 2: Setup 메서드 파라미터 변경 ***
    // bool hasUnread 대신 UnreadInfo를 받아 더 많은 정보를 처리
    public void Setup(Chat chat, string previewMessage, UnreadInfo unreadInfo, MessengerApp appController)
    {
        this.chat = chat;
        this.messengerApp = appController;

        chatPartnerImage.sprite = chat.chatPartner.chatPartnerImage;
        chatPartnerNameText.text = chat.chatPartner.chatPartnerName;
        lastMessageText.text = previewMessage; // 미리보기 메시지로 텍스트 설정

        // 모든 알림을 일단 끈다
        mandatoryunreadIndicator.SetActive(false);
        notMandatoryUnreadIndicator.SetActive(false);

        // 안 읽은 메시지 정보에 따라 적절한 알림을 켠다
        if (unreadInfo.hasUnread)
        {
            if (unreadInfo.hasMandatory)
            {
                mandatoryunreadIndicator.SetActive(true);
            }
            else
            {
                notMandatoryUnreadIndicator.SetActive(true);
            }
        }

        GetComponent<Button>().onClick.AddListener(OnItemClick);
        GetComponent<Button>().onClick.AddListener(PhoneManager.Instance.PhoneTouchEffect);
    }

    private void OnItemClick()
    {
        messengerApp.OpenChatRoom(chat);
    }

}
