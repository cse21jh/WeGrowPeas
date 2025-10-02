using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Shovel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Grid grid;
    [SerializeField] protected EconomyManager economyManager;
    [SerializeField] private RectTransform shovelRectTransform;

    

    private bool isDragging = false;

    private Vector2 initialPos;

    public bool IsEnabled { get; set; } = true;

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
        if (!IsEnabled) return;
        isDragging = true;
        grid.ShowAllPriceSign();
        UpdatePosition(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsEnabled) return;
        isDragging = false;
        UpdatePosition(eventData);

        TryDestroyUnderMouse();
        grid.HideAllPriceSign();
        shovelRectTransform.localPosition = initialPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsEnabled)
        {
            grid.HideAllPriceSign();
            return;
        }
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

    private void TryDestroyUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"[Shovel] Raycast Hit: {hit.collider.gameObject.name}");

            // 1) 식물 제거
            Plant plant = hit.collider.GetComponent<Plant>();
            if (plant != null && !plant.isDying)
            {
                SoundManager.Instance.PlayEffect("Shovel");                
                if(plant.Die(DeathCause.Shovel)) // false라면 페트병 제거
                {
                    economyManager.AddSellCount(plant.speciesname);
                    economyManager.AddGold(plant.GetSellingPrice());
                }
                return;
            }

            // 2) 비료 마커 제거
            FertilizerMarker fertMarker = hit.collider.GetComponent<FertilizerMarker>();
            if (fertMarker != null && fertMarker.IsOn)
            {
                SoundManager.Instance.PlayEffect("Shovel");

                int col = fertMarker.transform.parent.GetSiblingIndex();

                grid.RemoveFertilizerAt(col);

                return;
            }
        }
    }
}
