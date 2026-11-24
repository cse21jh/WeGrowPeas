using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WeGrowPeas.UI.Book
{
    /// <summary>Live, read-only catalog data on a pair of ordinary book layout roots.</summary>
    public sealed class ArchiveCatalogView : MonoBehaviour
    {
        [SerializeField] private bool achievements;
        [SerializeField] private BookLayoutController book;
        [SerializeField] private ArchiveBookController archive;
        [SerializeField] private ScrollRect listScroll;
        [SerializeField] private ArchiveEntryRow rowTemplate;
        [SerializeField] private TMP_Text count;
        [SerializeField] private TMP_Text empty;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text detailPrice;
        [SerializeField] private GameObject priceRoot;
        [SerializeField] private ScrollRect detailScroll;
        [SerializeField] private Image detailIcon;
        [SerializeField] private Image detailStamp;
        [SerializeField] private ArchiveProgressBar progressBar;
        [SerializeField] private TMP_Text progressValue;
        [SerializeField] private Sprite completeStamp;
        [SerializeField] private Sprite inProgressStamp;
        [SerializeField] private Sprite achievementFallbackIcon;
        [SerializeField] private Button[] categoryButtons;
        private readonly List<ArchiveEntryRow> rows = new List<ArchiveEntryRow>();
        private readonly List<AchievementData> achievementData = new List<AchievementData>();
        private List<CodexEntry> entries = new List<CodexEntry>();
        private CodexProgress.Category category;
        private int selected = -1;
        private bool refreshPending;

        public int EntryCount => achievements ? achievementData.Count : entries.Count;
        public int SelectedIndex => selected;
        public CodexProgress.Category Category => category;

        private void OnEnable()
        {
            Loc.LanguageChanged += RequestRefresh;
            CodexProgress.OnStatChanged += OnStatChanged;
            AchievementSystem.OnUnlocked += OnUnlocked;
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= RequestRefresh;
            CodexProgress.OnStatChanged -= OnStatChanged;
            AchievementSystem.OnUnlocked -= OnUnlocked;
        }

        private void Start()
        {
            for (int i = 0; categoryButtons != null && i < categoryButtons.Length; i++)
            {
                int target = i;
                categoryButtons[i].onClick.AddListener(() => SelectCategory(target));
            }
        }

        private void OnStatChanged(string key, int value) => RequestRefresh();
        private void OnUnlocked(AchievementData data) => RequestRefresh();
        private void RequestRefresh() => refreshPending = true;

        private void LateUpdate()
        {
            if (refreshPending && !book.IsTurning && !archive.IsSwitching) Refresh();
        }

        // Invoked by Layout.OnShow before BookPageTurner captures the destination page.
        public void Refresh()
        {
            refreshPending = false;
            if (achievements)
            {
                achievementData.Clear();
                foreach (var definition in AchievementSystem.All)
                    if (definition != null && !string.IsNullOrEmpty(definition.id)) achievementData.Add(definition);
            }
            else entries = CodexCatalog.Get(category);

            while (rows.Count < EntryCount)
            {
                var row = Instantiate(rowTemplate, listScroll.content);
                row.name = "Entry " + rows.Count;
                rows.Add(row);
            }
            for (int i = 0; i < rows.Count; i++)
            {
                bool visible = i < EntryCount;
                rows[i].gameObject.SetActive(visible);
                if (!visible) continue;
                int index = i;
                if (achievements)
                {
                    var data = achievementData[i];
                    bool done = AchievementSystem.IsUnlocked(data.id);
                    bool known = done || !data.hidden;
                    rows[i].Bind(known ? data.LocalizedTitle : "???", known ? data.icon != null ? data.icon : achievementFallbackIcon : null,
                        done ? completeStamp : inProgressStamp, () => SelectEntry(index));
                }
                else
                {
                    var entry = entries[i];
                    bool known = !entry.locked && entry.discovered;
                    rows[i].Bind(entry.locked ? Loc.T("game.common.locked") : known ? entry.displayName : "???",
                        known ? entry.icon : null, null, () => SelectEntry(index));
                }
            }
            empty.gameObject.SetActive(EntryCount == 0);
            empty.text = Loc.Or("archive.empty", "아직 표시할 기록이 없어요.");
            selected = EntryCount == 0 ? -1 : Mathf.Clamp(selected, 0, EntryCount - 1);
            RenderSelection();
            for (int i = 0; categoryButtons != null && i < categoryButtons.Length; i++)
            {
                bool active = i == (int)category;
                categoryButtons[i].interactable = !active;
                var bookmark = categoryButtons[i].GetComponent<ArchiveBookmark>();
                if (bookmark != null) bookmark.SetSelected(active);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(listScroll.content);
        }

        public void SelectCategory(int index)
        {
            if (achievements || index < 0 || index > 3 || (int)category == index ||
                book.IsTurning || archive.IsSwitching) return;
            bool forward = index > (int)category;
            book.Turner.TryTurn(forward, () =>
            {
                category = (CodexProgress.Category)index;
                selected = 0;
                Refresh();
                listScroll.verticalNormalizedPosition = 1;
            });
        }

        public void SetCategoryBeforeOpen(int index)
        {
            category = (CodexProgress.Category)Mathf.Clamp(index, 0, 3);
            selected = 0;
        }

        public void SelectEntry(int index)
        {
            if (index < 0 || index >= EntryCount || book.IsTurning || archive.IsSwitching) return;
            selected = index;
            RenderSelection();
        }

        private void RenderSelection()
        {
            for (int i = 0; i < rows.Count; i++) rows[i].SetSelected(i == selected);
            count.text = $"{(selected < 0 ? 0 : selected + 1)} / {EntryCount}";
            if (selected < 0)
            {
                detailTitle.text = Loc.Or(achievements ? "archive.achievement" : "archive.codex", achievements ? "업적" : "도감");
                detailText.text = Loc.Or("archive.empty", "아직 표시할 기록이 없어요.");
                if (detailIcon != null) detailIcon.enabled = false;
                if (detailStamp != null) detailStamp.enabled = false;
                if (progressBar != null) progressBar.SetProgress(0);
                if (progressValue != null) progressValue.text = "0 / 0";
                if (priceRoot != null) priceRoot.SetActive(false);
                return;
            }
            if (achievements)
            {
                var data = achievementData[selected];
                bool done = AchievementSystem.IsUnlocked(data.id);
                bool known = done || !data.hidden;
                detailTitle.text = known ? data.LocalizedTitle : "???";
                detailText.text = known ? data.LocalizedDescription : Loc.T("game.codex.undiscovered");
                int current = AchievementSystem.GetProgress(data);
                int target = Mathf.Max(1, data.target);
                progressBar.SetProgress(known ? current / (float)target : 0);
                progressValue.text = known ? $"{current} / {target}" : "? / ?";
                detailStamp.enabled = true;
                detailStamp.sprite = done ? completeStamp : inProgressStamp;
            }
            else
            {
                var entry = entries[selected];
                bool known = !entry.locked && entry.discovered;
                detailTitle.text = entry.locked ? Loc.T("game.common.locked") : known ? entry.displayName : "???";
                detailText.text = entry.locked
                    ? string.IsNullOrEmpty(entry.unlockHint) ? Loc.T("shop.unlock.default") : entry.unlockHint
                    : known ? entry.archiveDetail ?? entry.detail : Loc.T("game.codex.undiscovered");
                detailIcon.sprite = known ? entry.icon : null;
                detailIcon.enabled = known && entry.icon != null;
                if (priceRoot != null) priceRoot.SetActive(category == CodexProgress.Category.Item);
                if (detailPrice != null) detailPrice.text = known && entry.price >= 0 ? entry.price.ToString("N0") : "???";
            }
            detailScroll.verticalNormalizedPosition = 1;
        }
    }
}
