using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class UIManager : Singleton<UIManager>
{
    public PopupSystem Popup { get; private set; }
    private bool showBreedPopupSetting = true;


    //프로필 데이터 저장    
    public bool ShowBreedPopupSetting => showBreedPopupSetting;


    [Header("Popup Settings")]
    [SerializeField] private CloseablePopup defaultCloseablePrefab;
    [SerializeField] private ToastPopup defaultToastPrefab;
    [SerializeField] private CurseTooltipUI defaultTooltipPrefab;
    [SerializeField] private BreedPopup defaultBreedPopupPrefab;
    [SerializeField] private FloatingPopup defaultFloatingPopupPrefab;
    [SerializeField] private UnlockPopup defaultUnlockPopupPrefab;
    [SerializeField] private Transform popupCanvasParent;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        Popup = new PopupSystem(defaultCloseablePrefab, defaultToastPrefab, defaultTooltipPrefab, defaultBreedPopupPrefab, defaultFloatingPopupPrefab, defaultUnlockPopupPrefab, popupCanvasParent);
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

    public void LoadUIManager(ProfileData profileData)
    {
        showBreedPopupSetting = profileData.showBreedPopupSetting;
    }

    public void SetBreedPopupSetting(bool val)
    {
        showBreedPopupSetting = val;
        if (!val && Popup != null)
        {
            Popup.CloseBreedPopup();
        }
    }
}
