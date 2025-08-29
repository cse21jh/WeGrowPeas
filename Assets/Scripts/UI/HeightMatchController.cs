using UnityEditor;
using UnityEngine;


public class HeightMatchController : MonoBehaviour
{
    RectTransform rectTransform;
    [SerializeField] RectTransform canvas;

    [SerializeField] private float sizeMatch;
    [SerializeField] private float pivotValue;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, canvas.sizeDelta.y * sizeMatch);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, canvas.sizeDelta.y * pivotValue); 
    }

    [ExecuteInEditMode]
    private void Update()
    {
        //Debug.Log(canvas.sizeDelta.y);
    }
}
