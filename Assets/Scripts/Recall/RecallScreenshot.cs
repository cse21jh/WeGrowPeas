using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 회상 목록에 쓸 농장 사진을 남긴다.
///
/// 기본은 메인 카메라를 RenderTexture에 직접 렌더하는 방식이다.
/// Screen Space - Overlay 캔버스는 카메라 렌더에 포함되지 않으므로 UI가 자연히 빠지고,
/// 해상도도 화면 크기와 무관하게 고정할 수 있다.
/// 이 방식이 실패하면(렌더 파이프라인 사정 등) 화면 캡처로 물러선다 — UI가 같이 찍히지만
/// 사진이 아예 없는 것보다 낫다.
/// </summary>
public static class RecallScreenshot
{
    /// <summary>썸네일 가로 크기(px). 세로는 화면 비율을 따라간다.</summary>
    public const int DefaultWidth = 640;

    /// <summary>한 프레임이 다 그려진 뒤 사진을 찍는다. 실패하면 null을 넘긴다.</summary>
    public static IEnumerator CaptureRoutine(Action<byte[]> onDone, int targetWidth = DefaultWidth)
    {
        // 렌더가 끝난 뒤여야 카메라를 다시 돌려도 화면이 깨지지 않는다.
        yield return new WaitForEndOfFrame();

        byte[] png = null;

        try
        {
            png = CaptureFromCamera(targetWidth);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Recall] 카메라 캡처 실패, 화면 캡처로 대체합니다: {e.Message}");
        }

        if (png == null || png.Length == 0)
        {
            try
            {
                png = CaptureFromScreen(targetWidth);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Recall] 스크린샷 실패: {e.Message}");
                png = null;
            }
        }

        onDone?.Invoke(png);
    }

    /// <summary>화면 비율을 유지한 썸네일 크기.</summary>
    private static void GetSize(int targetWidth, out int w, out int h)
    {
        w = Mathf.Max(16, targetWidth);
        h = Mathf.Max(16, Mathf.RoundToInt(w * (float)Screen.height / Mathf.Max(1, Screen.width)));
    }

    private static byte[] CaptureFromCamera(int targetWidth)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Recall] 메인 카메라를 찾지 못했습니다.");
            return null;
        }

        GetSize(targetWidth, out int w, out int h);

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;
        Texture2D tex = null;

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            return tex.EncodeToPNG();
        }
        finally
        {
            // 카메라를 원래 상태로 돌려놓지 않으면 이후 화면이 검게 나온다.
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            if (tex != null) UnityEngine.Object.Destroy(tex);
        }
    }

    /// <summary>폴백: 화면을 그대로 받아 썸네일 크기로 줄인다(UI 포함).</summary>
    private static byte[] CaptureFromScreen(int targetWidth)
    {
        Texture2D full = ScreenCapture.CaptureScreenshotAsTexture();
        if (full == null) return null;

        GetSize(targetWidth, out int w, out int h);

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        RenderTexture prevActive = RenderTexture.active;
        Texture2D tex = null;

        try
        {
            Graphics.Blit(full, rt);

            RenderTexture.active = rt;
            tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            return tex.EncodeToPNG();
        }
        finally
        {
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            if (tex != null) UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(full);
        }
    }
}
