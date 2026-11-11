using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
[AddComponentMenu("UI/Phone/Phone Toggle Hover")]
public sealed class PhoneToggleHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private PhonePanelMotion motion;

    public void OnPointerEnter(PointerEventData eventData) => motion?.SetHovered(true);
    public void OnPointerExit(PointerEventData eventData) => motion?.SetHovered(false);
    private void OnDisable() => motion?.ClearHover();
}
