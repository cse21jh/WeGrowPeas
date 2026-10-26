using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WeGrowPeas.RecallBook;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Switches arbitrary pairs of uGUI content roots inside a shared book shell.</summary>
    [AddComponentMenu("UI/Book/Book Layout Controller")]
    [DisallowMultipleComponent]
    public sealed class BookLayoutController : MonoBehaviour
    {
        [Serializable]
        public sealed class Layout
        {
            public string name = "Layout";
            [Tooltip("A content container below LeftPage; leave empty for a blank left page.")]
            public RectTransform leftContent;
            [Tooltip("A content container below RightPage; leave empty for a blank right page.")]
            public RectTransform rightContent;
            [Tooltip("Populate this layout synchronously, after activation and before the turn captures it.")]
            public UnityEvent onShow = new UnityEvent();
        }

        [SerializeField] private BookPageTurner turner;
        [SerializeField] private Layout[] layouts = Array.Empty<Layout>();
        [SerializeField, Min(0)] private int initialLayout;
        [Tooltip("Optional. These navigate the Layouts list, not pages in an inventory or record list.")]
        [SerializeField] private Button previous;
        [SerializeField] private Button next;
        private int currentIndex = -1;
        private bool changing;

        public int CurrentIndex => currentIndex;
        public int LayoutCount => layouts.Length;
        public IReadOnlyList<Layout> Layouts => layouts;
        public BookPageTurner Turner => turner;
        public bool IsTurning => turner != null && turner.IsTurning;

        private void Reset() => turner = GetComponent<BookPageTurner>();

        private void OnEnable()
        {
            if (turner == null) turner = GetComponent<BookPageTurner>();
            if (previous != null) previous.onClick.AddListener(Previous);
            if (next != null) next.onClick.AddListener(Next);
            if (TryValidate(out string problem))
                Commit(Mathf.Clamp(currentIndex < 0 ? initialLayout : currentIndex, 0, layouts.Length - 1));
            else Debug.LogWarning("[Book UI] " + problem, this);
            RefreshNavigation();
        }

        private void OnDisable()
        {
            if (previous != null) previous.onClick.RemoveListener(Previous);
            if (next != null) next.onClick.RemoveListener(Next);
        }

        private void LateUpdate() => RefreshNavigation();

        // void wrappers are available in Button.onClick's Inspector menu.
        public void Show(int index) => TryShow(index);
        public void ShowImmediate(int index) => TryShow(index, false);
        public void Next() => TryShow(currentIndex + 1);
        public void Previous() => TryShow(currentIndex - 1);

        /// <summary>Indices are zero-based. Repeated input during a turn is rejected.</summary>
        public bool TryShow(int index, bool animate = true)
        {
            if (!isActiveAndEnabled || changing || IsTurning || index == currentIndex ||
                index < 0 || index >= layouts.Length) return false;
            if (!TryValidate(out string problem))
            {
                Debug.LogWarning("[Book UI] " + problem, this);
                return false;
            }
            if (!animate || currentIndex < 0)
            {
                Commit(index);
                return true;
            }
            // The source remains visible until TryTurn captures it. Only then replace the UI.
            return turner.TryTurn(index > currentIndex, () => Commit(index));
        }

        private void Commit(int index)
        {
            changing = true;
            try
            {
                currentIndex = index;
                for (int i = 0; i < layouts.Length; i++)
                {
                    if (i == index) continue;
                    SetVisible(layouts[i], false);
                }
                SetVisible(layouts[index], true);
                layouts[index].onShow?.Invoke();
                Canvas.ForceUpdateCanvases();
            }
            finally
            {
                changing = false;
                RefreshNavigation();
            }
        }

        private static void SetVisible(Layout layout, bool visible)
        {
            if (layout.leftContent != null) layout.leftContent.gameObject.SetActive(visible);
            if (layout.rightContent != null) layout.rightContent.gameObject.SetActive(visible);
        }

        private void RefreshNavigation()
        {
            bool ready = currentIndex >= 0 && !changing && !IsTurning;
            if (previous != null) previous.interactable = ready && currentIndex > 0;
            if (next != null) next.interactable = ready && currentIndex + 1 < layouts.Length;
        }

        public bool TryValidate(out string problem)
        {
            problem = null;
            if (turner == null || turner.LeftPage == null || turner.RightPage == null)
                problem = "Assign a Page Turner with both paper pages.";
            else if (layouts.Length == 0) problem = "Add at least one layout.";
            else
            {
                var roots = new List<RectTransform>();
                foreach (var layout in layouts)
                {
                    if (layout == null) { problem = "Layouts cannot contain a null entry."; break; }
                    if (!CheckRoot(layout.leftContent, turner.LeftPage, roots) ||
                        !CheckRoot(layout.rightContent, turner.RightPage, roots))
                    {
                        problem = "Each content root must be a separate child of its corresponding paper page. " +
                            "Do not assign the paper itself, share a root between layouts, or nest layout roots.";
                        break;
                    }
                }
            }
            return problem == null;
        }

        private static bool CheckRoot(RectTransform root, RectTransform page, List<RectTransform> roots)
        {
            if (root == null) return true;
            if (root == page || !root.IsChildOf(page)) return false;
            foreach (var other in roots)
                if (root == other || root.IsChildOf(other) || other.IsChildOf(root)) return false;
            roots.Add(root);
            return true;
        }
    }
}
