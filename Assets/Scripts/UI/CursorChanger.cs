using UnityEngine;

public class CursorChanger : MonoBehaviour
{
    [SerializeField] private CursorType cursorType = CursorType.Clickable;

    public CursorType GetCursorType() => cursorType;


    private void OnMouseOver()
    {
        CursorManager.Instance.SetCursor(cursorType);
        if (Input.GetMouseButton(0))
        {
            CursorManager.Instance.SetCursor(CursorType.Clicked);
        }
    }

    void OnMouseExit()
    {
        CursorManager.Instance.SetCursor(CursorType.Default);
    }
}
