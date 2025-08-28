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
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnArea, screenPos, null, out localPos);

        GameObject wcp = Instantiate(whiteCirclePrefab, spawnArea);

        RectTransform rt = wcp.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;
        rt.sizeDelta = size;
    }


    public void FlushSpawnedCircleCanvas()
    {
        foreach (Transform child in spawnArea)
        {
            Destroy(child.gameObject);
        }
    }
}
