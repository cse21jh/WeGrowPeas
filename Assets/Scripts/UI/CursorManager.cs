using UnityEngine;
using UnityEngine.UI;

public enum CursorType
{
    Default,
    Clickable,
    Clicked,
}

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;



    public Image cursorImage;   // 인스펙터에서 할당
    public Vector2 offset;      // 커서 중심 위치 조정용 (예: (16, -16))


    [System.Serializable]
    public struct CursorData
    {
        public CursorType type;
        public Sprite sprite;
    }
    public CursorData[] cursors;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        if (Cursor.visible == false)
        {
            cursorImage.rectTransform.position = Input.mousePosition;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SetCursor(CursorType.Clicked);
        }
        if(Input.GetMouseButtonUp(0))
        {
            SetCursor(CursorType.Default);
        }
    }

    // 게임 창이 포커스를 잃었을 때 (창을 벗어나거나 Alt+Tab 등)
    void OnApplicationFocus(bool hasFocus)
    {
        Cursor.visible = !hasFocus; // 창을 벗어나면 시스템 커서 보이게
        cursorImage.enabled = hasFocus; // UI 커서 이미지 숨김/표시
    }

    public void SetCursor(CursorType type)
    {
        foreach (var c in cursors)
        {
            if (c.type == type)
            {
                cursorImage.sprite = c.sprite;
                return;
            }
        }
    }
}
