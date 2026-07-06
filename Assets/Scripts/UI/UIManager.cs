using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    // 각 UI 서브시스템들을 제공하는 프로퍼티
    public PopupSystem Popup { get; private set; }

    [Header("Popup Settings")]
    [SerializeField] private CloseablePopup defaultCloseablePrefab;
    [SerializeField] private ToastPopup defaultToastPrefab;
    [SerializeField] private Transform popupCanvasParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // UIManager의 초기화 시점에 팝업 서브시스템 인스턴스 생성
        Popup = new PopupSystem(defaultCloseablePrefab, defaultToastPrefab, popupCanvasParent);

        Popup.ShowCloseablePopup("아털써", "아털써는..어쩌구저쩌구...");
    }
}
