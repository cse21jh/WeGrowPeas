using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.Image;

public class GlowCanvasController : MonoBehaviour
{
    [SerializeField] private RectTransform[] originalTransform;
    [SerializeField] private RectTransform[] glowTransform;

    [SerializeField] private CanvasGroup[] glowCanvasGroups;

    [SerializeField] private bool isGlowActive = false;


    private void Start()
    {
        glowCanvasGroups = GetComponentsInChildren<CanvasGroup>();
    }

    public void SyncUI(RectTransform original, RectTransform glow)
    {
        Debug.Log("변경 전 " + original.anchoredPosition + " / " + glow.anchoredPosition);
        glow.anchoredPosition = original.anchoredPosition;
        Debug.Log("변경 후 " + original.anchoredPosition + " / " + glow.anchoredPosition);

        glow.rotation = original.rotation;
        //glow.localScale = original.localScale;

        // 4. SizeDelta 동기화
        //glow.sizeDelta = original.sizeDelta;

        // 5. Pivot과 Anchor도 같아야 정확한 위치에 찍힙니다.
        //glow.pivot = original.pivot;
        //glow.anchorMin = original.anchorMin;
        //glow.anchorMax = original.anchorMax;
    }


    public void ToggleGlow(bool state)
    {
        isGlowActive = state;
        for (int i = 0; i < originalTransform.Length; i++)
        {
            if (isGlowActive)
            {
                SyncUI(originalTransform[i], glowTransform[i]);
                //glowTransform[i].position = originalTransform[i].position;
                //glowTransform[i].sizeDelta = originalTransform[i].sizeDelta;
                glowTransform[i].gameObject.SetActive(true);
            }
            else
            {
                glowTransform[i].gameObject.SetActive(false);
            }
        }

        if (isGlowActive)
        {
            GlowTransition(0f, 1f, 1f);
        }
        else
        {
            GlowTransition(1f, 0f, 1f);
        }
    }

    private void Update()
    {
        if(isGlowActive)
            UpdateRect();
    }

    private void UpdateRect()
    {
        for(int i = 0; i < originalTransform.Length; i++)
        {
            glowTransform[i].gameObject.SetActive(originalTransform[i].gameObject.activeSelf);
            glowTransform[i].anchoredPosition = originalTransform[i].anchoredPosition;
            glowTransform[i].rotation = originalTransform[i].rotation;
        }
    }

    private void GlowTransition(float start, float end, float duration)
    {

        DOTween.To(() => start, x =>
            {
                foreach (var cg in glowCanvasGroups)
                {
                    cg.alpha = x;
                }
            }, end, duration);
    }
}
