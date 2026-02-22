using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Shovel : MonoBehaviour, IPointerDownHandler
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
        if (Input.GetKeyDown(KeyCode.A))
        {

            if (!isDragging)
            {
                grid.isDraggingShovel = true;
                isDragging = true;
                grid.ShowAllPriceSign();
                UpdatePosition();
            }
            else
            {
                ResetShovel();
            }
        }

        if (isDragging)
        {
            if(!IsEnabled)
            {
                ResetShovel();
                return;
            }
            UpdatePosition();
        }        
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsEnabled) return;

        if (isDragging) // 삽을 들고 있는 경우
        {
            TryDestroyUnderMouse(); // 뭔가 팔거나 삽을 처음 위치로 이동
        }
        else // 삽 들기 시작
        {
            grid.isDraggingShovel = true;
            isDragging = true;
            grid.ShowAllPriceSign();
            UpdatePosition();
        }
    }

    /*
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
    */

    private void UpdatePosition()
    {
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
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
            Plant target = plant;
            if (plant != null && !plant.isDying)
            {
                SoundManager.Instance.PlayEffect("Shovel");                
                if(plant.Die(DeathCause.Shovel)) // false라면 페트병 제거
                {
                    economyManager.AddSellCount(plant.speciesname);
                    economyManager.AddGold(plant.GetSellingPrice());
                    GameEvents.RaisePeaSold(target);
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

            // 3) 황금 비료 제거
            // 토양에서 위치를 가져와서 황금 비료가 있는지 확인
            Soil soil = hit.collider.GetComponent<Soil>();
            if (soil != null)
            {
                int idx = soil.GridIndex;
                if (grid.HasGoldSoil(idx))
                {
                    // 식물이 있는 경우 제거 불가
                    if (!grid.plantGrid.ContainsKey(idx))
                    {
                        SoundManager.Instance.PlayEffect("Shovel");
                        grid.RemoveGoldSoil(idx);
                        return;
                    }
                }
            }
        }

        // 4) 맨땅 클릭
        ResetShovel();
    }

    private void ResetShovel()
    {
        isDragging = false;
        grid.HideAllPriceSign();
        grid.isDraggingShovel = false;
        shovelRectTransform.localPosition = initialPos;
    }
}
