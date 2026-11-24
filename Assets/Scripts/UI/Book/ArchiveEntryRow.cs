using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WeGrowPeas.UI.Book
{
    public sealed class ArchiveEntryRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Image stamp;
        [SerializeField] private TMP_Text label;

        public void Bind(string title, Sprite picture, Sprite status, UnityAction select)
        {
            label.text = title;
            icon.sprite = picture;
            icon.enabled = picture != null;
            stamp.sprite = status;
            stamp.enabled = status != null;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(select);
        }

        public void SetSelected(bool selected)
        {
            background.color = new Color(1, 1, 1, selected ? 1 : 0.5f);
        }
    }
}
