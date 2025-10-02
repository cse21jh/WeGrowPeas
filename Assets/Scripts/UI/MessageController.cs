using UnityEngine;
using TMPro;

public class MessageController : MonoBehaviour
{
    public enum MessageSenderType
    {
        none,
        pea,
        player
    }

    [SerializeField] private Transform chatContent; // ScrollView 안 Content

    [SerializeField] private GameObject messagePrefab;     // 일반 메시지 (한 개의 텍스트 박스 / 완두콩 메시지)
    [SerializeField] private GameObject selectionPrefab;     // 선택형 메시지 (두 개의 텍스트 박스 / 플레이어 메시지)

    private GameObject currentPeaMessage;

    public void AddMessage(MessageSenderType sender, string messageContent)
    {
        Debug.Log(sender + ": " + messageContent);

        if (sender == MessageSenderType.player)
        {
            selectionPrefab = Instantiate(selectionPrefab, chatContent);
            selectionPrefab.GetComponent<SelectionBoxController>().SetText("좋아!", "싫어!");
        }
        else
        {
            currentPeaMessage = Instantiate(messagePrefab, chatContent);
            currentPeaMessage.GetComponent<MessageBoxBtnController>().SetText(messageContent);
        }
    }
}
