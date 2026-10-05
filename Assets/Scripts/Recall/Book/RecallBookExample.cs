using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WeGrowPeas.RecallBook
{
    /// <summary>Eight entries per spread. Demo mode is separate from the user's saved records.</summary>
    public sealed class RecallBookExample : MonoBehaviour
    {
        [SerializeField] private BookPageTurner turner;
        [SerializeField] private bool useSavedRecords;
        [SerializeField] private TMP_Text[] titles;
        [SerializeField] private TMP_Text[] descriptions;
        [SerializeField] private RawImage[] photos;
        [SerializeField] private Button[] entries;
        [SerializeField] private TMP_Text leftNumber;
        [SerializeField] private TMP_Text rightNumber;
        [SerializeField] private Button previous;
        [SerializeField] private Button next;
        [SerializeField] private Texture demoPhoto;
        [SerializeField] private RecallDetailUI detailUI;
        [SerializeField] private UnityEvent<string> entrySelected = new UnityEvent<string>();
        private List<RecallIndexEntry> records;
        private readonly List<Texture2D> ownedPhotos = new List<Texture2D>();
        private readonly List<Texture2D> retiredPhotos = new List<Texture2D>();
        private int spread;
        private const int PerSpread = 8;
        public int SpreadIndex => spread;
        public int SpreadCount => Mathf.Max(1, Mathf.CeilToInt((useSavedRecords ? records?.Count ?? 0 : 24) / (float)PerSpread));

        private void Awake()
        {
            previous.onClick.AddListener(Previous);
            next.onClick.AddListener(Next);
            for (int i = 0; i < entries.Length; i++)
            {
                int slot = i;
                entries[i].onClick.AddListener(() => Select(slot));
            }
        }

        private void OnEnable()
        {
            spread = 0;
            if (useSavedRecords) records = RecallStore.GetEntries();
            RenderSpread();
        }

        private void LateUpdate()
        {
            previous.interactable = spread > 0 && !turner.IsTurning;
            next.interactable = spread + 1 < SpreadCount && !turner.IsTurning;
            // Old pictures must survive the synchronous source capture.
            if (!turner.IsTurning) Release(retiredPhotos);
        }

        public void Next() => Move(1);
        public void Previous() => Move(-1);

        private void Move(int direction)
        {
            int target = spread + direction;
            if (target < 0 || target >= SpreadCount) return;
            turner.TryTurn(direction > 0, () => { spread = target; RenderSpread(); });
        }

        private void RenderSpread()
        {
            retiredPhotos.AddRange(ownedPhotos);
            ownedPhotos.Clear();
            int count = useSavedRecords ? records?.Count ?? 0 : 24;
            for (int i = 0; i < entries.Length; i++)
            {
                int index = spread * PerSpread + i;
                bool exists = index < count;
                entries[i].gameObject.SetActive(exists);
                if (!exists) { photos[i].texture = null; continue; }
                if (useSavedRecords)
                {
                    var entry = records[index];
                    titles[i].text = $"{index + 1}번째 기록";
                    descriptions[i].text = $"{entry.day}일 · {entry.plantName}\n" +
                        DateTimeOffset.FromUnixTimeSeconds(entry.savedAtUnix).ToLocalTime().ToString("yyyy.MM.dd");
                    var texture = RecallStore.LoadImage(entry.id);
                    if (texture != null) ownedPhotos.Add(texture);
                    photos[i].texture = texture != null ? texture : demoPhoto;
                    photos[i].color = texture != null ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                }
                else
                {
                    titles[i].text = $"MEMORY {index + 1:00}";
                    descriptions[i].text = $"DAY {12 + index * 3}  /  PEA GARDEN\n2026.09.{index + 1:00}";
                    photos[i].texture = demoPhoto;
                    photos[i].color = Color.HSVToRGB(0.08f * (index % 4), 0.11f, 1);
                }
            }
            int total = SpreadCount * 2;
            leftNumber.text = count == 0 ? "아직 기록이 없어요" : $"{spread * 2 + 1} / {total}";
            rightNumber.text = $"{spread * 2 + 2} / {total}";
        }

        private void Select(int slot)
        {
            if (!useSavedRecords || turner.IsTurning) return;
            int index = spread * PerSpread + slot;
            if (records == null || index >= records.Count) return;
            string id = records[index].id;
            if (detailUI != null) detailUI.Show(id);
            entrySelected.Invoke(id);
        }

        private static void Release(List<Texture2D> textures)
        {
            foreach (var texture in textures) if (texture != null) Destroy(texture);
            textures.Clear();
        }

        private void OnDisable()
        {
            foreach (var photo in photos) if (photo != null) photo.texture = null;
            Release(ownedPhotos);
            Release(retiredPhotos);
        }
    }
}
