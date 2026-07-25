using System;
using UnityEngine;

/// <summary>
/// 채팅 메시지 한 개의 데이터.
/// 같은 StageId가 연속되는 동안에는 첫 메시지에만 프로필이 표시됩니다.
/// </summary>
[Serializable]
public sealed class ChatMessageData
{
    [SerializeField]
    private int stageId;

    [SerializeField, TextArea(1, 8)]
    private string message;

    public int StageId => stageId;
    public string Message => message;

    public ChatMessageData(int stageId, string message)
    {
        this.stageId = stageId;
        this.message = message;
    }
}
