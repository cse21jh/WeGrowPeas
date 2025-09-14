using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class UICursorChanger : MonoBehaviour, ICursorHover, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CursorType cursorType = CursorType.Default;

    [SerializeField] private bool isMouseOver = false;

    public CursorType GetCursorType() => cursorType;

    
    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
    }

    private void Update()
    {
        if (isMouseOver)
        {
            CursorManager.Instance.SetCursor(cursorType);
            if (Input.GetMouseButton(0))
            {
                CursorManager.Instance.SetCursor(CursorType.Clicked);
            }
        }
        else
        {
            CursorManager.Instance.SetCursor(CursorType.Default);
        }
    }
}