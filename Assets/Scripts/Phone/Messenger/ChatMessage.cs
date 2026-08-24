using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ChatMessage
{
    [Tooltip("메시지 도착을 유발하는 트리거 ID. 예: '3', 'GoldPlant'")]
    public string triggerId;

    [Tooltip("이 메시지를 반드시 확인해야 게임이 진행되면 체크")]
    public bool isMandatory = false;

    [Tooltip("필수 메시지 팝업에서 외부 행동 완료 신호가 올 때까지 다음 버튼을 잠금")]
    public bool waitForAdvanceSignal = false;

    [TextArea(3, 5)]
    public string messageText;

    [Tooltip("이전 메시지 표시 후 이 메시지가 나타나기까지의 시간(초)")]
    public float delayAfterPrevious = 1.5f;
}
