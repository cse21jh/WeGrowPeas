using UnityEngine;

public class FixCameraAspect : MonoBehaviour
{
    public float targetAspect = 16f / 9f;
    Camera cam;
    void Awake() { cam = GetComponent<Camera>(); Apply(); }
    void Update() { Apply(); }

    void Apply()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1f)  // 세로 기준(좌우에 여백)
            cam.rect = new Rect(0f, (1f - scaleHeight) * 0.5f, 1f, scaleHeight);
        else                   // 가로 기준(위아래에 여백)
        {
            float scaleWidth = 1f / scaleHeight;
            cam.rect = new Rect((1f - scaleWidth) * 0.5f, 0f, scaleWidth, 1f);
        }
    }
}
