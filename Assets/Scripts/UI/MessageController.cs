using UnityEngine;
using TMPro;
using System;
using System.Collections;

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

    private GameObject typingMessage;

    public void AddMessage(MessageSenderType sender, string messageContent, string bonusMessageContent = "", Action act1 = null, Action act2 = null)
    {
        Debug.Log(sender + ": " + messageContent);

        if (sender == MessageSenderType.player)
        {
            selectionPrefab = Instantiate(selectionPrefab, chatContent);
            selectionPrefab.GetComponent<SelectionBoxController>().SetText(messageContent, bonusMessageContent, act1, act2);
        }
        else
        {
            currentPeaMessage = Instantiate(messagePrefab, chatContent);
            currentPeaMessage.GetComponent<MessageBoxBtnController>().SetText(messageContent);
        }

        FindAnyObjectByType<AutoScroll>().OnNewMessage();
    }

    public void AddTypingMessage(float time)
    {
        StartCoroutine(Typing(time));
    }

    private IEnumerator Typing(float time)
    {
        typingMessage = Instantiate(messagePrefab, chatContent);
        typingMessage.GetComponent<MessageBoxBtnController>().SetText("...");
        FindAnyObjectByType<AutoScroll>().OnNewMessage();
        yield return new WaitForSeconds(time);
        Destroy(typingMessage);
        typingMessage = null;
    }

    private void OnDisable()
    {
        if (typingMessage != null)
        {
            Destroy(typingMessage);
            typingMessage = null;
        }
    }

    public void AddDay(int stage)
    {
        currentPeaMessage = Instantiate(messagePrefab, chatContent);
        currentPeaMessage.GetComponent<MessageBoxBtnController>().SetText(stage.ToString());
        FindAnyObjectByType<AutoScroll>().OnNewMessage();
}
}
