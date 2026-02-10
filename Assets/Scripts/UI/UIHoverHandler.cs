using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class UIHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action onHoverEnter;
    private Action onHoverExit;

    public void Setup(Action onEnter, Action onExit)
    {
        onHoverEnter = onEnter;
        onHoverExit = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }
}
