using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    public PopupSystem Popup { get; private set; }

    [Header("Popup Settings")]
    [SerializeField] private CloseablePopup defaultCloseablePrefab;
    [SerializeField] private ToastPopup defaultToastPrefab;
    [SerializeField] private HoverTooltipUI defaultTooltipPrefab;
    [SerializeField] private Transform popupCanvasParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        Popup = new PopupSystem(defaultCloseablePrefab, defaultToastPrefab, defaultTooltipPrefab, popupCanvasParent);
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Popup?.CleanupOnSceneChange();
    }
}
