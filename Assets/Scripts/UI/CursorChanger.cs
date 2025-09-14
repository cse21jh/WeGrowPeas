using UnityEngine;

public class CursorChanger : MonoBehaviour
{
    [SerializeField] private CursorType cursorType = CursorType.Clickable;

    public CursorType GetCursorType() => cursorType;

    void OnMouseEnter()
    {
        CursorManager.Instance.SetCursor(cursorType);
    }

    void OnMouseExit()
    {
        CursorManager.Instance.SetCursor(CursorType.Default);
    }
}
