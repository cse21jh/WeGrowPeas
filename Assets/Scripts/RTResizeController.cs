using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RTResizeController : MonoBehaviour
{
    [SerializeField] private RenderTexture[] rtArray;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
    }
}
