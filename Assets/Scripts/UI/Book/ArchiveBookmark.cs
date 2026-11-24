using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Figma white PNG layers, independently tinted without stretching their artwork.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ArchiveBookmark : MonoBehaviour
    {
        [Header("Figma palette — editable in the Inspector")]
        [SerializeField, InspectorName("바탕 색상")] private Color faceColor = Color.white;
        [SerializeField, InspectorName("아래쪽 음영 색상")] private Color depthColor = Color.white;
        [SerializeField, InspectorName("테두리 색상")] private Color outlineColor = Color.white;
        [SerializeField, InspectorName("그림자 색상")] private Color shadowColor = new Color(0, 0, 0, 0.25f);
        [SerializeField, InspectorName("아이콘 면 색상")] private Color iconFillColor = Color.white;
        [SerializeField, InspectorName("아이콘 선 색상")] private Color iconInkColor = Color.white;
        [Header("Separate white PNG layers")]
        [SerializeField] private Image[] faces;
        [SerializeField] private Image[] depths;
        [SerializeField] private Image[] outlines;
        [SerializeField] private Image[] shadows;
        [SerializeField] private Image iconFill;
        [SerializeField] private Image iconInk;
        [SerializeField] private GameObject selectedShape;
        [SerializeField] private GameObject normalShape;
        [SerializeField] private TMP_Text label;
        [SerializeField] private bool selected;
        private bool paletteDirty = true;

        private void OnEnable() { ApplyPalette(); SetSelected(selected); }
        private void OnValidate() => paletteDirty = true;
        private void Update() { if (paletteDirty) ApplyPalette(); }

        public void SetSelected(bool value)
        {
            selected = value;
            if (selectedShape != null) selectedShape.SetActive(value);
            if (normalShape != null) normalShape.SetActive(!value);
            if (label != null) label.gameObject.SetActive(value);
            var button = GetComponent<Button>();
            if (button != null) button.interactable = !value;
            var rect = (RectTransform)transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value ? 125 : 95);
        }

        public void ApplyPalette()
        {
            paletteDirty = false;
            Tint(faces, faceColor); Tint(depths, depthColor);
            Tint(outlines, outlineColor); Tint(shadows, shadowColor);
            if (iconFill != null) iconFill.color = iconFillColor;
            if (iconInk != null) iconInk.color = iconInkColor;
        }

        private static void Tint(Image[] images, Color color)
        {
            if (images == null) return;
            foreach (var image in images) if (image != null) image.color = color;
        }
    }
}
