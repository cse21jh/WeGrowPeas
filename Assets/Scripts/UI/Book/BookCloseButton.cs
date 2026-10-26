using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.UI.Book
{
    /// <summary>A page-corner button whose two drawings keep independent RectTransforms.</summary>
    [AddComponentMenu("UI/Book/Book Close Button")]
    public sealed class BookCloseButton : Button
    {
        [SerializeField] private Image normalVisual;
        [SerializeField] private Image highlightedVisual;
        [SerializeField] private Image pageImage;
        [SerializeField] private Sprite normalPage;
        [SerializeField] private Sprite highlightedPage;
        [SerializeField] private Image bookBaseImage;
        [SerializeField] private Sprite normalBookBase;
        [SerializeField] private Sprite highlightedBookBase;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            // Deliberately bypass Selectable's Sprite Swap and color transitions. In particular,
            // xBtn1 must not be squeezed into xBtn0's smaller rectangle.
            bool expanded = IsActive() && IsInteractable() &&
                (state == SelectionState.Highlighted || state == SelectionState.Pressed ||
                 state == SelectionState.Selected);
            ApplyVisuals(expanded);
        }

        protected override void InstantClearState()
        {
            base.InstantClearState();
            ApplyVisuals(false);
        }

        private void ApplyVisuals(bool expanded)
        {
            // Keep the GameObjects alive so pointer events still reach this common Button parent.
            // The larger drawing includes the original hit area and can receive clicks itself.
            if (normalVisual != null) normalVisual.enabled = !expanded;
            if (highlightedVisual != null) highlightedVisual.enabled = expanded;
            var page = expanded ? highlightedPage : normalPage;
            if (pageImage != null && page != null && pageImage.sprite != page)
                pageImage.sprite = page;
            var bookBase = expanded ? highlightedBookBase : normalBookBase;
            if (bookBaseImage != null && bookBase != null && bookBaseImage.sprite != bookBase)
                bookBaseImage.sprite = bookBase;
        }
    }
}
