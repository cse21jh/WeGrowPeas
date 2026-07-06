using System.Collections.Generic;
using UnityEngine;

public class PopupSystem
{
    private CloseablePopup defaultCloseablePrefab;
    private ToastPopup defaultToastPrefab;
    private Transform canvasParent;

    private Dictionary<GameObject, Queue<BasePopup>> popupPools = new Dictionary<GameObject, Queue<BasePopup>>();

    public PopupSystem(CloseablePopup closeablePrefab, ToastPopup toastPrefab, Transform parent)
    {
        defaultCloseablePrefab = closeablePrefab;
        defaultToastPrefab = toastPrefab;
        canvasParent = parent;
    }

    /// X 버튼이 있는 유연한 일반 팝업을 표시합니다.
    public CloseablePopup ShowCloseablePopup(string title, string content, Sprite sprite = null, System.Action onClose = null)
    {
        CloseablePopup popup = ShowPopup(defaultCloseablePrefab);
        popup.Setup(title, content, sprite, onClose);
        return popup;
    }

    /// 지정된 시간이 지난 후 자동으로 페이드아웃되며 사라지는 토스트 팝업을 표시합니다.
    public ToastPopup ShowToastPopup(string title, string content, Sprite sprite = null, float duration = 2.0f, System.Action onClose = null)
    {
        ToastPopup popup = ShowPopup(defaultToastPrefab);
        popup.SetupAndPlay(title, content, sprite, duration, onClose);
        return popup;
    }

    /// 어떤 팝업 프리팹이든 넘겨주면 알아서 풀링하여 띄워줍니다.
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
}
