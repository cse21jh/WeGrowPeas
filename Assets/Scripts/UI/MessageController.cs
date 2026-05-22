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
    [SerializeField] private GameObject timePrefab;     // 날짜 구분 프리팹
    [SerializeField] private GameObject recentPrefab;     // 선택형 메시지 (두 개의 텍스트 박스 / 플레이어 메시지)

    private GameObject currentPeaMessage;

    private GameObject typingMessage;
    private Coroutine _typingCoroutine;

    public void AddMessage(MessageSenderType sender, string messageContent, string bonusMessageContent = "", Action act1 = null, Action act2 = null)
    {

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

        FindAnyObjectByType<AutoScroll>()?.OnNewMessage();
    }

    public void AddTypingMessage(float time)
    {
        _typingCoroutine = StartCoroutine(Typing(time));
    }

    private IEnumerator Typing(float time)
    {
        typingMessage = Instantiate(messagePrefab, chatContent);
        typingMessage.GetComponent<MessageBoxBtnController>().SetText("...");
        FindAnyObjectByType<AutoScroll>()?.OnNewMessage();
        yield return new WaitForSeconds(time);
        Destroy(typingMessage);
        typingMessage = null;
        _typingCoroutine = null;
    }

    /// <summary>타이핑 중인 "..." 버블을 즉시 제거합니다.</summary>
    public void KillTypingMessage()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        if (typingMessage != null)
        {
            Destroy(typingMessage);
            typingMessage = null;
        }
    }

    private void OnDisable()
    {
        KillTypingMessage();
    }

    public void AddDay(int stage)
    {
        currentPeaMessage = Instantiate(timePrefab, chatContent);
        currentPeaMessage.GetComponent<MessageBoxBtnController>().SetText("---------- " + stage.ToString() + " 일차 ----------");
        FindAnyObjectByType<AutoScroll>()?.OnNewMessage();
    }

    public void AddNewChatSeperator()
    {
        currentPeaMessage = Instantiate(recentPrefab, chatContent);
        FindAnyObjectByType<AutoScroll>()?.OnNewMessage();
    }
}
