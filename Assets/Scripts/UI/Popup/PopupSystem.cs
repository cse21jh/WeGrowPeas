using System.Collections.Generic;
using UnityEngine;

public class PopupSystem
{
    private CloseablePopup defaultCloseablePrefab;
    private ToastPopup defaultToastPrefab;
    private CurseTooltipUI defaultTooltipPrefab;
    private BreedPopup defaultBreedPopupPrefab;
    private FloatingPopup defaultFloatingPopupPrefab;
    private UnlockPopup defaultUnlockPopupPrefab;
    private Transform canvasParent;

    private BreedPopup activeBreedPopupInstance;
    private FloatingPopup activeFloatingPopupInstance;
    private FloatingPopup activeGuidePopupInstance; // 지속 안내(설치형 아이템 선택 대기 등)
    private UnlockPopup activeUnlockPopupInstance;

    private Dictionary<GameObject, Queue<BasePopup>> popupPools = new Dictionary<GameObject, Queue<BasePopup>>();

    public PopupSystem(CloseablePopup closeablePrefab, ToastPopup toastPrefab, CurseTooltipUI tooltipPrefab, BreedPopup breedPrefab, FloatingPopup floatingPrefab, UnlockPopup unlockPrefab, Transform parent)
    {
        defaultCloseablePrefab = closeablePrefab;
        defaultToastPrefab = toastPrefab;
        defaultTooltipPrefab = tooltipPrefab;
        defaultBreedPopupPrefab = breedPrefab;
        defaultFloatingPopupPrefab = floatingPrefab;
        defaultUnlockPopupPrefab = unlockPrefab;
        canvasParent = parent;
    }

    public CloseablePopup ShowCloseablePopup(string title, string content, Sprite sprite = null, System.Action onClose = null)
    {
        CloseablePopup popup = ShowPopup(defaultCloseablePrefab);
        popup.Setup(title, content, sprite, onClose);
        return popup;
    }

    public ToastPopup ShowToastPopup(string title, string content, Sprite sprite = null, float duration = 2.0f, System.Action onClose = null)
    {
        ToastPopup popup = ShowPopup(defaultToastPrefab);
        popup.SetupAndPlay(title, content, sprite, duration, onClose);
        return popup;
    }

    public CurseTooltipUI ShowCurseTooltip(Vector2 position, Sprite iconSprite, string description, int daysLeft = -1, System.Action onClose = null)
    {
        CurseTooltipUI popup = ShowPopup(defaultTooltipPrefab);
        popup.Setup(iconSprite, description, daysLeft, onClose);

        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }

        return popup;
    }

    public BreedPopup ShowBreedPopup(Plant plant, System.Action onClose = null)
    {
        return ShowBreedPopup(defaultBreedPopupPrefab, plant, onClose);
    }

    public BreedPopup ShowBreedPopup(BreedPopup prefab, Plant plant, System.Action onClose = null)
    {
        if (prefab == null) return null;

        if (activeBreedPopupInstance == null)
        {
            activeBreedPopupInstance = Object.Instantiate(prefab, canvasParent);
            activeBreedPopupInstance.OnPopupClosed = (closedPopup) => {
                // Do not pool this instance to preserve its position
            };
        }

        activeBreedPopupInstance.Setup(plant, onClose);

        if (!activeBreedPopupInstance.gameObject.activeSelf)
        {
            activeBreedPopupInstance.Open();
        }

        return activeBreedPopupInstance;
    }

    public void CloseBreedPopup()
    {
        if (activeBreedPopupInstance != null && activeBreedPopupInstance.gameObject.activeSelf)
        {
            activeBreedPopupInstance.Close();
        }
    }

    public FloatingPopup ShowFloatingPopup(string text, float delay = 2.0f, System.Action onClose = null)
    {
        return ShowFloatingPopup(defaultFloatingPopupPrefab, text, delay, onClose);
    }

    public FloatingPopup ShowFloatingPopup(FloatingPopup prefab, string text, float delay = 2.0f, System.Action onClose = null)
    {
        if (prefab == null) return null;

        if (activeFloatingPopupInstance == null)
        {
            activeFloatingPopupInstance = Object.Instantiate(prefab, canvasParent);
            activeFloatingPopupInstance.OnPopupClosed = (closedPopup) => {
                // Do not pool this instance to preserve its position
            };
        }

        activeFloatingPopupInstance.SetupAndPlay(text, delay, onClose);

        if (!activeFloatingPopupInstance.gameObject.activeSelf)
        {
            activeFloatingPopupInstance.Open();
        }

        return activeFloatingPopupInstance;
    }

    /// <summary>
    /// 지속 안내를 표시한다. HideGuide()를 부를 때까지 계속 떠 있는다.
    /// (설치형 아이템 구매 후 "식물/토양을 선택해주세요" 등. FloatingPopup 프리팹을 재사용한다.)
    /// </summary>
    public FloatingPopup ShowGuide(string text)
    {
        if (defaultFloatingPopupPrefab == null) return null;

        if (activeGuidePopupInstance == null)
        {
            activeGuidePopupInstance = Object.Instantiate(defaultFloatingPopupPrefab, canvasParent);
            activeGuidePopupInstance.OnPopupClosed = (closedPopup) => { }; // 풀에 반환하지 않음
        }

        // SetupPersistent가 SetActive(true) + raycast 비차단까지 처리(Open을 부르면 raycast를 다시 막으므로 호출 X)
        activeGuidePopupInstance.SetupPersistent(text);
        return activeGuidePopupInstance;
    }

    /// <summary>지속 안내를 즉시 숨긴다.</summary>
    public void HideGuide()
    {
        if (activeGuidePopupInstance != null)
            activeGuidePopupInstance.HideNow();
    }

    public UnlockPopup ShowUnlockPopup(List<ItemData> items, System.Action onClose = null)
    {
        return ShowUnlockPopup(defaultUnlockPopupPrefab, items, onClose);
    }

    public UnlockPopup ShowUnlockPopup(UnlockPopup prefab, List<ItemData> items, System.Action onClose = null)
    {
        if (prefab == null) return null;

        if (activeUnlockPopupInstance == null)
        {
            activeUnlockPopupInstance = Object.Instantiate(prefab, canvasParent);
            activeUnlockPopupInstance.OnPopupClosed = (closedPopup) => {
                // Do not pool this instance to preserve its position
            };
        }

        activeUnlockPopupInstance.Setup(items, onClose);

        if (!activeUnlockPopupInstance.gameObject.activeSelf)
        {
            activeUnlockPopupInstance.Open();
        }

        return activeUnlockPopupInstance;
    }

    public T ShowPopup<T>(T prefab) where T : BasePopup
    {
        if (prefab == null) return null;

        GameObject key = prefab.gameObject;

        // 해당 프리팹 전용 풀이 없다면 생성
        if (!popupPools.ContainsKey(key))
        {
            popupPools[key] = new Queue<BasePopup>();
        }

        Queue<BasePopup> pool = popupPools[key];
        T popup;

        if (pool.Count > 0)
        {
            popup = (T)pool.Dequeue();
        }
        else
        {
            popup = Object.Instantiate(prefab, canvasParent);
            // 닫힐 때 이 프리팹 전용 풀에 다시 들어가도록 콜백 등록
            popup.OnPopupClosed = (closedPopup) => pool.Enqueue(closedPopup);
        }

        popup.Open();
        return popup;
    }


    public void CleanupOnSceneChange()
    {
        activeBreedPopupInstance = null;
        activeFloatingPopupInstance = null;
        activeGuidePopupInstance = null;
        activeUnlockPopupInstance = null;
        foreach (var pair in popupPools)
        {
            Queue<BasePopup> pool = pair.Value;
            while (pool.Count > 0)
            {
                BasePopup popup = pool.Dequeue();
                if (popup != null)
                {
                    Object.Destroy(popup.gameObject);
                }
            }
        }
        popupPools.Clear();

        if (canvasParent != null)
        {
            BasePopup[] activePopups = canvasParent.GetComponentsInChildren<BasePopup>(true);
            foreach (var popup in activePopups)
            {
                if (popup != null)
                {
                    Object.Destroy(popup.gameObject);
                }
            }
        }
    }
}
