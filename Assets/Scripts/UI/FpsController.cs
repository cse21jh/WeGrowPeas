using TMPro;
using UnityEngine;

public class FpsController : MonoBehaviour
{
    private TextMeshProUGUI txt;
    private float deltaTime = 0.0f;
    private int lastDisplayedFps = -1;
    private Color lastColor;

    private void Start()
    {
        txt = GetComponent<TextMeshProUGUI>();
        lastColor = txt != null ? txt.color : Color.white;
        InvokeRepeating(nameof(UpdateFps), 0f, 0.5f);
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void UpdateFps()
    {
        if (txt == null) return;

        int fps = Mathf.RoundToInt(1.0f / deltaTime);
        if (fps == lastDisplayedFps) return;
        lastDisplayedFps = fps;

        txt.SetText("{0} FPS", fps);

        Color target = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
        if (target != lastColor)
        {
            txt.color = target;
            lastColor = target;
        }
    }
}
