using UnityEngine;

public class CursorChanger : MonoBehaviour
{
    [SerializeField] private CursorType cursorType = CursorType.Clickable;

    public CursorType GetCursorType() => cursorType;


    private void OnMouseOver()
    {
        if (CursorManager.Instance != null)
            CursorManager.Instance.SetCursor(cursorType);
        if (Input.GetMouseButton(0))
        {
            if (CursorManager.Instance != null)
                CursorManager.Instance.SetCursor(CursorType.Clicked);
        }
    }

    void OnMouseExit()
    {
        if(CursorManager.Instance != null)
            CursorManager.Instance.SetCursor(CursorType.Default);
    }
}
