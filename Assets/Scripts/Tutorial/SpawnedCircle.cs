using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnedCircle : MonoBehaviour
{
    [SerializeField] private GameObject whiteCirclePrefab;

    private RectTransform spawnArea;

    private void Awake()
    {
        Transform child = transform.Find("SpawnedCircle");

        if (child != null )
        {
            spawnArea = child.GetComponent<RectTransform>();
        }
    }

    public void ShowCircle(Vector3 worldPos, Vector2 size)
    {
        GameObject wcp = Instantiate(whiteCirclePrefab, spawnArea);

        RectTransform rt = wcp.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        
        WhiteCircle wc = wcp.GetComponent<WhiteCircle>();
        if (wc != null)
        {
            wc.targetWorldPos = worldPos;
            wc.spawnArea = spawnArea;
            wc.mainCam = Camera.main;
        }
    }

    public void ShowUICircle(RectTransform uiRect, Vector2 size)
    {
        GameObject wcp = Instantiate(whiteCirclePrefab, spawnArea);

        RectTransform rt = wcp.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        
        WhiteCircle wc = wcp.GetComponent<WhiteCircle>();
        if (wc != null)
        {
            wc.targetUIRect = uiRect;
            wc.spawnArea = spawnArea;
        }
    }

    public void ShowUICircle(Vector2 anchoredPos, Vector2 size)
    {
        GameObject wcp = Instantiate(whiteCirclePrefab, spawnArea);

        RectTransform rt = wcp.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        
        WhiteCircle wc = wcp.GetComponent<WhiteCircle>();
        if (wc != null)
        {
            wc.isFixedUIPos = true;
            wc.fixedUIPos = anchoredPos;
            wc.spawnArea = spawnArea;
        }
    }

    public void FlushSpawnedCircleCanvas()
    {
        foreach (Transform child in spawnArea)
        {
            Destroy(child.gameObject);
        }
    }
}
