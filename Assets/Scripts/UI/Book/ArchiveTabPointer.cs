using UnityEngine;
using UnityEngine.EventSystems;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Pointer events stay on the fixed hit area; the archive composes all artwork motion.</summary>
    [DisallowMultipleComponent]
    public sealed class ArchiveTabPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private ArchiveBookController archive;
        private int row;

        public void Configure(ArchiveBookController owner, int index) { archive = owner; row = index; }
        public void OnPointerEnter(PointerEventData eventData) => archive?.SetTabPointer(row,true);
        public void OnPointerExit(PointerEventData eventData) => archive?.SetTabPointer(row,false);
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) archive?.SetTabPressed(row,true);
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) archive?.SetTabPressed(row,false);
        }
        private void OnDisable() => archive?.SetTabPointer(row,false);
    }
}
