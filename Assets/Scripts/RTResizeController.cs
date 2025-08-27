using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RTResizeController : MonoBehaviour
{
    [SerializeField] private RenderTexture[] rtArray;
    [SerializeField] private Camera[] camArray;

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
            rt.width = Screen.width;
            rt.height = Screen.height;
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
