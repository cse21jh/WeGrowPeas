using System.Collections.Generic;
using UnityEngine;

public class PopupSystem
{
    private CloseablePopup defaultCloseablePrefab;
    private ToastPopup defaultToastPrefab;
    private HoverTooltipUI defaultTooltipPrefab;
    private Transform canvasParent;

    private Dictionary<GameObject, Queue<BasePopup>> popupPools = new Dictionary<GameObject, Queue<BasePopup>>();

    public PopupSystem(CloseablePopup closeablePrefab, ToastPopup toastPrefab, HoverTooltipUI tooltipPrefab, Transform parent)
    {
        defaultCloseablePrefab = closeablePrefab;
        defaultToastPrefab = toastPrefab;
        defaultTooltipPrefab = tooltipPrefab;
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

    public HoverTooltipUI ShowHoverTooltip(Vector2 position, Sprite iconSprite, string description, System.Action onClose = null)
    {
        HoverTooltipUI popup = ShowPopup(defaultTooltipPrefab);
        popup.Setup(iconSprite, description, onClose);

        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }

        return popup;
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
