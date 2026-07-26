using UnityEngine;
using UnityEngine.UI;

public class WhiteCircle : MonoBehaviour
{
    [HideInInspector] public Vector3 targetWorldPos;
    [HideInInspector] public bool isFixedUIPos;
    [HideInInspector] public Vector2 fixedUIPos;
    [HideInInspector] public RectTransform targetUIRect;
    [HideInInspector] public RectTransform spawnArea;
    [HideInInspector] public Camera mainCam;

    private RectTransform rectTransform;
    private float initialOrthoSize = 0f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 클릭 판정을 방해하지 않도록 Raycast Target을 끕니다.
        Image img = GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = false;
        }
    }

    void Update()
    {
        if (spawnArea == null || rectTransform == null) return;

        if (isFixedUIPos)
        {
            // Fixed UI Position Mode
            rectTransform.anchoredPosition = fixedUIPos;
            rectTransform.localScale = Vector3.one;
        }
        else if (targetUIRect != null)
        {
            // UI Tracking Mode
            rectTransform.position = targetUIRect.position;
            rectTransform.localScale = Vector3.one;
        }
        else
        {
            // World Tracking Mode
            if (mainCam == null) return;

            if (initialOrthoSize == 0f)
            {
                initialOrthoSize = mainCam.orthographicSize;
            }

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(mainCam, targetWorldPos);
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnArea, screenPos, mainCam.transform.parent != null ? null : null, out localPos); 

            rectTransform.anchoredPosition = localPos;

            if (initialOrthoSize > 0f && mainCam.orthographicSize > 0f)
            {
                float scale = initialOrthoSize / mainCam.orthographicSize;
                rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
