using UnityEngine;
using UnityEngine.EventSystems;

public class UICursorChanger : MonoBehaviour, ICursorHover, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CursorType cursorType = CursorType.Default;

    public CursorType GetCursorType() => cursorType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorManager.Instance.SetCursor(cursorType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CursorManager.Instance.SetCursor(CursorType.Default);
    }
}