using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Shovel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Grid grid;
    [SerializeField] private RectTransform shovelRectTransform;

    private bool isDragging = false;

    private Vector2 initialPos;

    // Start is called before the first frame update
    void Start()
    {
        initialPos = shovelRectTransform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
    
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        UpdatePosition(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        UpdatePosition(eventData);

        TryDestroyPlantUnderMouse();
        shovelRectTransform.localPosition = initialPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            UpdatePosition(eventData);
        }
    }

    private void UpdatePosition(PointerEventData eventData)
    {
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPos))
        {
            shovelRectTransform.localPosition = localPos;
        }
    }

    private void TryDestroyPlantUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            Plant plant = hit.collider.GetComponent<Plant>();
            
            grid.DestroyPlantByShovel( plant );

            //Debug.Log("[Shovel] Raycast Hit: " + hit.collider.name);
        }
    }
}
