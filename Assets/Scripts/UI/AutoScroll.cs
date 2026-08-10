using UnityEngine;
using UnityEngine.UI;

public class AutoScroll : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    // 사용자가 스크롤을 위로 올려놨는지 확인
    private bool IsAtBottom()
    {
        // verticalNormalizedPosition은 1 = 맨 위, 0 = 맨 아래
        return scrollRect.verticalNormalizedPosition <= 0.01f;
    }

    // 새로운 메시지가 추가될 때 호출
    public void OnNewMessage()
    {
        // 사용자가 맨 아래를 보고 있을 때만 자동 스크롤
        if (IsAtBottom())
        {
            Canvas.ForceUpdateCanvases(); // 레이아웃 강제 갱신
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
