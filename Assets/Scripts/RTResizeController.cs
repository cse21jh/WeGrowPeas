using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RTResizeController : MonoBehaviour
{
    [SerializeField] private RenderTexture[] rtArray;
    [SerializeField] private Camera[] camArray;

    /// <summary>외곽선 RT 해상도 배율. <see cref="GraphicsQuality"/>가 정한다.</summary>
    public static float OutlineResolutionScale = 1f;

    private static bool IsOutlineTexture(RenderTexture rt)
    {
        return rt != null && rt.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>품질이 바뀌었을 때 열려 있는 씬의 RT를 다시 만든다.</summary>
    public static void ResizeAll()
    {
        foreach (var controller in FindObjectsByType<RTResizeController>(FindObjectsSortMode.None))
            controller.ResizeRenderTextures();
    }

    private int previousWidth;
    private int previousHeight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        previousWidth = Screen.width;
        previousHeight = Screen.height;

        StartCoroutine(DelayResizeRT(0.1f));
    }

    private IEnumerator DelayResizeRT(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        ResizeRenderTextures();
    }

    public void ResizeRenderTextures()
    {
        foreach(RenderTexture rt in rtArray)
        {
            if (rt != null)
            {
                rt.Release();
            }
            // 외곽선 RT는 품질 설정에 따라 낮은 해상도로 그린다. 화면에 늘려 그려도 외곽선은 거의 티가 나지 않는다.
            float scale = IsOutlineTexture(rt) ? Mathf.Clamp(OutlineResolutionScale, 0.25f, 1f) : 1f;
            rt.width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * scale));
            rt.height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale));
            rt.Create();

            
        }

        foreach (Camera camera in camArray)
        {
            if (camera != null)
            {
                camera.ResetAspect();
            }
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            ResizeRenderTextures();
        }


        if (Screen.width != previousWidth || Screen.height != previousHeight)
        {
            // 해상도 변경 감지 시 로직 실행
            Debug.Log($"해상도 변경 감지: {Screen.width}x{Screen.height}");
            ResizeRenderTextures();

            // 예: UI 재배치, 카메라 조정 등
            previousWidth = Screen.width;
            previousHeight = Screen.height;
        }
    }
}
