using DG.Tweening;
using TMPro;
using UnityEngine;

public class MsgCanvasController : MonoBehaviour
{
    [Header("Open/Close Animation Settings")]
    [SerializeField] private RectTransform Top_OpenRect;
    [SerializeField] private RectTransform Top_CloseRect;
    [SerializeField] private RectTransform Panel_OpenRect;
    [SerializeField] private RectTransform Panel_CloseRect;

    [SerializeField] private RectTransform Top_People;
    [SerializeField] private RectTransform Top_Chat;

    [SerializeField] private RectTransform PeoplePanel;
    [SerializeField] private RectTransform ChatPanel;

    [SerializeField] private float OpenCloseDuration = 0.5f;
    [SerializeField] private Ease OpenCloseEase = Ease.InOutSine;


    [Space(10)]
    [Header("Info")]
    [SerializeField] private GameObject PeopleList; // 각 채팅방 프리팹을 담고 있을 부모 오브젝트
    [SerializeField] private GameObject PeoplePrefab; // 각 채팅방 프리팹(MsgBtn)

    [SerializeField] private TextMeshProUGUI TotalUnreadCountText;


    #region 패널 열고 닫기
    /// <summary>
    /// 사람들 패널에서 특정 채팅방을 클릭하면 실행되는 함수.
    /// 해당 인물과의 대화 데이터를 불러와서 채팅 패널을 열어야 함.
    /// "준하에몽 현민에몽 도와줘요"
    /// </summary>
    public void OpenChatPanel(
        // 여기도 필요한거 있으면 추가해줘용
        )
    {
        // 여기다가 정보 불러와서 ChatMessageList에 넣는 코드 작성해야함.
        // 그리고 채팅 패널이 열리면 나가기 버튼 오른쪽에 있는 숫자도 업데이트 해줘야 함.
        // TotalUnreadCountText.text = "0"; 이런식으로


        Top_Chat.gameObject.SetActive(true);
        ChatPanel.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(Top_Chat.DOAnchorPosX(Top_OpenRect.anchoredPosition.x, OpenCloseDuration).SetEase(OpenCloseEase));
        seq.Join(ChatPanel.DOAnchorPosX(Panel_OpenRect.anchoredPosition.x, OpenCloseDuration).SetEase(OpenCloseEase));

        seq.OnComplete(() =>
        {
            Top_People.gameObject.SetActive(false);
            PeoplePanel.gameObject.SetActive(false);
        });


    }

    public void CloseChatPanel()
    {
        Top_People.gameObject.SetActive(true);
        PeoplePanel.gameObject.SetActive(true);


        Sequence seq = DOTween.Sequence();

        seq.Append(Top_Chat.DOAnchorPosX(Top_CloseRect.anchoredPosition.x, OpenCloseDuration).SetEase(OpenCloseEase));
        seq.Join(ChatPanel.DOAnchorPosX(Panel_CloseRect.anchoredPosition.x, OpenCloseDuration).SetEase(OpenCloseEase));

        seq.OnComplete(() =>
        {
            Top_Chat.gameObject.SetActive(false);
            ChatPanel.gameObject.SetActive(false);
        });
    }
    #endregion

    #region 채팅방 정보 불러오기

    /// <summary>
    /// 사람들 패널이 보여질 때, 각 채팅방에서 아직 안 읽은 메시지 수와 가장 마지막 메시지를 미리보기로 보여주기 위해 채팅방 정보를 불러오는 함수.
    /// "준하에몽 현민에몽 도와줘요"
    /// </summary>

    // 일단은 컴파일 되게 ChatData라는 클래스가 없으니 주석처리 함.
    // 기존에 사용하던 채팅 데이터 활용해서 사용하면 될 듯
    /*
    private void LoadChatRoomInfo(ChatData[] chatRooms)
    {
        // 여기에 채팅방 정보 불러오는 코드 작성해야함.

        foreach (var chatRoom in chatRooms) // chatRooms는 채팅방 정보를 담고 있는 리스트라고 가정
        {
            GameObject msgBtn = Instantiate(PeoplePrefab, PeopleList.transform).SetParent(PeopleList);
            MsgBtnController controller = msgBtn.GetComponent<MsgBtnController>();
            // 채팅방 정보 설정
            controller.SetUp(chatRoom.ProfileImage, chatRoom.SenderName, chatRoom.LastMessage, chatRoom.UnreadCount.ToString());
        }

        int totalCount = chatRooms.Sum(room => room.UnreadCount); // 총 안 읽은 메시지 수 계산
        if(totalCount > 99){
            TotalUnreadCountText.text = "99+";
        }
        else{
            TotalUnreadCountText.text = totalCount.ToString(); // 총 안 읽은 메시지 수 업데이트
        }
    }
    */
    #endregion
}
