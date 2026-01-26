using TMPro;
using UnityEngine;

public class FpsController : MonoBehaviour
{
    private TextMeshProUGUI txt;
    private float deltaTime = 0.0f;

    private void Start()
    {
        txt = GetComponent<TextMeshProUGUI>();
        // 0.5초마다 UpdateFps 함수를 실행합니다.
        InvokeRepeating(nameof(UpdateFps), 0f, 0.5f);
    }

    private void Update()
    {
        // 매 프레임 사이의 시간 간격을 누적 계산합니다.
        // 유니티 공식 문서에서 권장하는 보정 방식입니다.
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void UpdateFps()
    {
        if (txt == null) return;

        // FPS 계산 (1초 / 프레임 간격)
        float fps = 1.0f / deltaTime;

        // 소수점 첫째 자리까지 표시
        txt.text = string.Format("{0:0.0} FPS", fps);

        // 성능 상태에 따라 텍스트 색상을 변경하고 싶다면 아래 주석을 해제하세요.
        
        if (fps >= 60) txt.color = Color.green;
        else if (fps >= 30) txt.color = Color.yellow;
        else txt.color = Color.red;
        
    }
}
