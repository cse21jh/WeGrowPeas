using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WeGrowPeas.UI
{
    /// <summary>Standalone settings window backed by the game's shared device settings.</summary>
    public sealed class SettingsDialogView : MonoBehaviour
    {
        [Serializable] public sealed class TabEvent : UnityEvent<int> { }
        [SerializeField] private GameObject modal;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private CanvasGroup[] tabVisuals;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text descriptionTitle;
        [SerializeField] private TMP_Text saveNotice;
        [SerializeField] private GameObject koreanTitle;
        [SerializeField] private TMP_Text translatedTitle;
        [SerializeField] private SettingsDialogDefinition[] definitions;
        [SerializeField] private SettingsDialogMotion motion;
        [SerializeField] private bool openOnStart = true;
        [SerializeField] private TabEvent onTabChanged = new TabEvent();
        [SerializeField] private UnityEvent onClosed = new UnityEvent();
        private SettingsDialogRow[] rows;
        private SettingsDialogRow described, capturing;
        private List<Vector2Int> resolutions;
        private List<FullScreenMode> windowModes;
        private List<int> frameRates;
        private GameObject previousSelection, lastSelection;
        private bool savePending, ownsPause, previousInputBlock, previousNavigation, dropdownWasOpen;
        private float saveAt, previousTimeScale;
        private int captureFrame, escapeFrame = -1;
        private Action afterClose;
        private static readonly KeyCode[] Keys = ((KeyCode[])Enum.GetValues(typeof(KeyCode))).Where(GameInput.IsBindable).ToArray();
        private static readonly string[] TabKeys = { "general", "graphics", "sound", "controls" };
        public bool IsOpen => modal != null && modal.activeSelf;
        public bool IsClosing => motion != null && motion.IsClosing;
        public int SelectedTab { get; private set; }
        public bool IsCapturing => capturing != null;
        public bool ConsumedEscapeThisFrame => escapeFrame == Time.frameCount;
        public TabEvent OnTabChanged => onTabChanged;
        public UnityEvent OnClosed => onClosed;

        private void Awake()
        {
            // Shared Canvas prefabs can contain this dialog; its own scaler needs a root Canvas.
            if (transform.parent != null && transform.parent.GetComponentInParent<Canvas>() != null)
                transform.SetParent(null, false);
            rows = GetComponentsInChildren<SettingsDialogRow>(true);
            foreach (var row in rows) row.Bind(this);
            closeButton.onClick.AddListener(Close);
            for (int i = 0; i < tabButtons.Length; i++)
            { int tab = i; tabButtons[i].onClick.AddListener(() => SelectTab(tab)); }
            BuildDisplayChoices();
            if (!openOnStart) modal.SetActive(false);
        }
        private void OnEnable()
        {
            Loc.LanguageChanged += Refresh; GameInput.BindingsChanged += Refresh; GraphicsQuality.Changed += Refresh;
            if (IsOpen) Open();
        }
        private void Start()
        { SelectTab(0); if (openOnStart) Open(); }
        private void OnDisable()
        {
            Loc.LanguageChanged -= Refresh; GameInput.BindingsChanged -= Refresh; GraphicsQuality.Changed -= Refresh;
            CancelCapture(); Flush(); afterClose = null; motion?.ResetClosed(); ReleasePause();
        }
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();
        private void Update()
        {
            if (savePending && Time.unscaledTime >= saveAt) Flush();
            if (!IsOpen || ConsumedEscapeThisFrame) return;
            if (IsClosing) return;
            if (capturing != null)
            {
                if (Time.frameCount <= captureFrame) return;
                if (Input.GetKeyDown(KeyCode.Escape)) { escapeFrame = Time.frameCount; CancelCapture(); Refresh(); return; }
                foreach (var key in Keys) if (Input.GetKeyDown(key)) { AcceptKey(key); break; }
                return;
            }
            var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selection != lastSelection)
            {
                lastSelection = selection;
                var row = selection != null ? selection.GetComponentInParent<SettingsDialogRow>() : null;
                if (row != null) Describe(row);
            }
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            var codex = CodexUIController.Instance;
            if (codex != null && (codex.IsOpen || codex.ConsumedEscapeThisFrame)) return;
            if (ProfileSelectUI.BlocksEscape || KeyBindingPanel.BlocksEscape) return;
            escapeFrame = Time.frameCount;
            if (dropdownWasOpen || rows.Any(r => r.dropdown != null && r.dropdown.IsExpanded))
            { HideDropdowns(); return; }
            Close();
        }
        private void LateUpdate() => dropdownWasOpen = rows.Any(r => r.dropdown != null && r.dropdown.IsExpanded);
        public void Open()
        {
            // SettingUI가 이 프레임의 Esc로 창을 열었다면 Update에서 같은 입력으로 닫지 않는다.
            if (!IsOpen && Input.GetKeyDown(KeyCode.Escape)) escapeFrame = Time.frameCount;
            afterClose = null;
            if (!ownsPause)
            {
                previousTimeScale = Time.timeScale;
                previousInputBlock = ClickRouter.Instance != null && ClickRouter.Instance.IsBlockedByUI;
                Time.timeScale = 0;
                if (ClickRouter.Instance != null) ClickRouter.Instance.IsBlockedByUI = true;
                ownsPause = true;
                if (EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
            }
            modal.SetActive(true); Refresh(); motion?.Open();
        }
        public void Close() => CloseThen(null);
        private void CloseThen(Action completed)
        {
            if (!IsOpen) { completed?.Invoke(); return; }
            if (IsClosing) return;
            afterClose = completed;
            CancelCapture(); HideDropdowns(); Flush();
            if (motion != null) motion.Close(FinishClose); else FinishClose();
        }
        private void FinishClose()
        {
            modal.SetActive(false); ReleasePause();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
            var completed = afterClose; afterClose = null;
            onClosed.Invoke();
            completed?.Invoke();
        }
        private void ReleasePause()
        {
            if (!ownsPause) return;
            ownsPause = false; Time.timeScale = previousTimeScale;
            if (ClickRouter.Instance != null) ClickRouter.Instance.IsBlockedByUI = previousInputBlock;
        }
        public void SelectTab(int index)
        {
            if (index < 0 || index >= pages.Length) return;
            CancelCapture(); HideDropdowns(); SelectedTab = index;
            for (int i = 0; i < pages.Length; i++)
            { pages[i].SetActive(i == index); tabVisuals[i].alpha = i == index ? 1f : 0.5f; }
            Canvas.ForceUpdateCanvases();
            var scroll = pages[index].GetComponentInChildren<ScrollRect>();
            if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            described = pages[index].GetComponentInChildren<SettingsDialogRow>();
            Refresh(); onTabChanged.Invoke(index);
        }
        private void HideDropdowns()
        { foreach (var row in rows) if (row.dropdown != null) row.dropdown.Hide(); }
        private void BuildDisplayChoices()
        {
            var current = SettingsDisplay.Current;
            resolutions = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height))
                .Concat(new[] { new Vector2Int(Screen.width, Screen.height), new Vector2Int(current.width, current.height), new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
                .Where(r => r.x > 0 && r.y > 0).Distinct().OrderByDescending(r => r.x).ThenByDescending(r => r.y).ToList();
            windowModes = new List<FullScreenMode> { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
            if (current.fullScreenMode >= 0 && current.fullScreenMode <= 3 && !windowModes.Contains((FullScreenMode)current.fullScreenMode))
                windowModes.Add((FullScreenMode)current.fullScreenMode);
            frameRates = new List<int> { -1, 30, 60, 120, 144, 240 };
            if (!frameRates.Contains(current.frameRate) && current.frameRate >= 15) frameRates.Add(current.frameRate);
        }
        public void Refresh()
        {
            if (rows == null) return;
            var data = DeviceSettings.Data;
            var display = SettingsDisplay.Current;
            bool korean = Loc.Language == "ko";
            koreanTitle.SetActive(korean); translatedTitle.gameObject.SetActive(!korean);
            translatedTitle.text = T("title"); descriptionTitle.text = T("description"); saveNotice.text = T("autosave");
            for (int i = 0; i < tabButtons.Length; i++)
                foreach (var text in tabVisuals[i].GetComponentsInChildren<TMP_Text>(true)) text.text = T(TabKeys[i]);
            foreach (var row in rows)
            {
                row.label.text = Label(row);
                switch (row.entry)
                {
                    case SettingsEntry.Language:
                        Options(row, Loc.AvailableLanguages.Select(Loc.GetLanguageName), Loc.AvailableLanguages.ToList().IndexOf(Loc.Language)); break;
                    case SettingsEntry.BreedPopup: Options(row, OnOff(), data.showBreedPopup ? 1 : 0); break;
                    case SettingsEntry.MessageAlarm: Options(row, OnOff(), data.playAlarmForSeenMessages ? 1 : 0); break;
                    case SettingsEntry.EndlessMode: Options(row, OnOff(), EndlessMode.Enabled ? 1 : 0); break;
                    case SettingsEntry.Resolution:
                        Options(row, resolutions.Select(r => r.x + "x" + r.y), resolutions.IndexOf(new Vector2Int(display.width, display.height)));
                        row.dropdown.interactable = SettingsDisplay.SupportsWindowSettings; break;
                    case SettingsEntry.WindowMode:
                        Options(row, windowModes.Select(m => T("mode." + m)), windowModes.IndexOf((FullScreenMode)display.fullScreenMode));
                        row.dropdown.interactable = SettingsDisplay.SupportsWindowSettings; break;
                    case SettingsEntry.Quality:
                        Options(row, new[] { Loc.T("settings.graphics.low"), Loc.T("settings.graphics.medium"), Loc.T("settings.graphics.high") }, (int)GraphicsQuality.Current); break;
                    case SettingsEntry.VSync: Options(row, OnOff(), display.vSyncCount > 0 ? 1 : 0); break;
                    case SettingsEntry.FrameRate:
                        Options(row, frameRates.Select(r => r == -1 ? T("unlimited") : r + " FPS"), frameRates.IndexOf(display.frameRate));
                        row.dropdown.interactable = display.vSyncCount == 0; break;
                    case SettingsEntry.BgmVolume: Volume(row, data.bgmVolume); break;
                    case SettingsEntry.EffectVolume: Volume(row, data.effectVolume); break;
                    case SettingsEntry.ResetKeys: row.valueText.text = T("reset"); break;
                    case SettingsEntry.Codex: row.valueText.text = T("open"); row.button.interactable = CodexUIController.Instance != null; break;
                    case SettingsEntry.MainMenu:
                        row.valueText.text = T("return"); row.button.interactable = SceneLoader.Instance != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneLoader.StartSceneName; break;
                    case SettingsEntry.Quit: row.valueText.text = T("quit"); row.button.interactable = Application.platform != RuntimePlatform.WebGLPlayer; break;
                    case SettingsEntry.AddGold: row.valueText.text = "+1000"; row.button.interactable = GameManager.Instance != null && GameManager.Instance.economyManager != null; break;
                    default: row.valueText.text = capturing == row ? T("pressKey") : GameInput.GetKeyName(ActionFor(row.entry)); break;
                }
            }
            if (described != null) Describe(described);
        }
        private static string T(string key) => Loc.T("settings.dialog." + key);
        public SettingsDialogDefinition GetDefinition(SettingsEntry entry) => definitions?.FirstOrDefault(d => d != null && d.Entry == entry);
        private string Label(SettingsDialogRow row) => GetDefinition(row.entry)?.Label ?? Loc.T(row.LabelKey);
        private static IEnumerable<string> OnOff() => new[] { T("off"), T("on") };
        private static void Options(SettingsDialogRow row, IEnumerable<string> labels, int selected)
        {
            row.dropdown.ClearOptions(); row.dropdown.AddOptions(labels.ToList());
            row.dropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, row.dropdown.options.Count - 1)); row.dropdown.RefreshShownValue();
        }
        private static void Volume(SettingsDialogRow row, float value)
        { row.slider.SetValueWithoutNotify(Mathf.Clamp01(value)); row.valueText.text = Mathf.RoundToInt(value * 100) + "%"; }
        private static GameAction ActionFor(SettingsEntry entry) => (GameAction)Enum.Parse(typeof(GameAction), entry.ToString());
        public void Describe(SettingsDialogRow row)
        {
            if (capturing != null && row != capturing) return;
            described = row; descriptionTitle.text = Label(row);
            string text = GetDefinition(row.entry)?.Description ?? Loc.T(row.DescriptionKey);
            if ((row.entry == SettingsEntry.Resolution || row.entry == SettingsEntry.WindowMode) && !SettingsDisplay.SupportsWindowSettings) text = T("browserWindow");
            if (row.entry == SettingsEntry.FrameRate && SettingsDisplay.Current.vSyncCount > 0) text += "\n\n" + T("vsyncPriority");
            if ((row.entry == SettingsEntry.Codex && CodexUIController.Instance == null) || (row.entry == SettingsEntry.MainMenu && SceneLoader.Instance == null)) text += "\n\n" + T("gameSceneRequired");
            if (row.entry == SettingsEntry.AddGold && GameManager.Instance == null) text += "\n\n" + T("gameSceneRequired");
            if (row.entry == SettingsEntry.Quit && Application.platform == RuntimePlatform.WebGLPlayer) text = T("browserQuit");
            if (capturing == row) text = T("captureHelp");
            description.text = text;
        }
        public void ChangeOption(SettingsDialogRow row, int value)
        {
            Describe(row);
            switch (row.entry)
            {
                case SettingsEntry.Language: Loc.SetLanguage(Loc.AvailableLanguages[value]); Refresh(); return;
                case SettingsEntry.BreedPopup: DeviceSettings.Data.showBreedPopup = value == 1; break;
                case SettingsEntry.MessageAlarm: MessengerSaveSystem.PlayAlarmForSeenMessages = value == 1; break;
                case SettingsEntry.EndlessMode: EndlessMode.Enabled = value == 1; Refresh(); return;
                case SettingsEntry.Resolution:
                    if (!SettingsDisplay.SupportsWindowSettings) return;
                    SettingsDisplay.Edit().width = resolutions[value].x; SettingsDisplay.Edit().height = resolutions[value].y; break;
                case SettingsEntry.WindowMode: if (!SettingsDisplay.SupportsWindowSettings) return; SettingsDisplay.Edit().fullScreenMode = (int)windowModes[value]; break;
                case SettingsEntry.Quality: GraphicsQuality.Set((GraphicsQualityLevel)value); Refresh(); return;
                case SettingsEntry.VSync: SettingsDisplay.Edit().vSyncCount = value; break;
                case SettingsEntry.FrameRate: if (SettingsDisplay.Current.vSyncCount > 0) return; SettingsDisplay.Edit().frameRate = frameRates[value]; break;
            }
            if (row.entry >= SettingsEntry.Resolution && row.entry <= SettingsEntry.FrameRate) SettingsDisplay.Apply(DeviceSettings.Data.display);
            savePending = true; Flush(); Refresh();
        }
        public void ChangeVolume(SettingsDialogRow row, float value)
        {
            value = Mathf.Clamp01(value);
            var sound = SoundManager.Instance;
            if (row.entry == SettingsEntry.BgmVolume)
            { DeviceSettings.Data.bgmVolume = value; if (sound != null) sound.ChangeBGMVolume(value); }
            else
            { DeviceSettings.Data.effectVolume = value; if (sound != null) sound.ChangeEffectVolume(value); }
            row.valueText.text = Mathf.RoundToInt(value * 100) + "%";
            Describe(row); savePending = true; saveAt = Time.unscaledTime + 0.25f;
        }
        public void Activate(SettingsDialogRow row)
        {
            CancelCapture(); Describe(row);
            switch (row.entry)
            {
                case SettingsEntry.ResetKeys: GameInput.ResetToDefaults(); Refresh(); return;
                case SettingsEntry.Codex: OpenCodex(); return;
                case SettingsEntry.MainMenu: ReturnToMainMenu(); return;
                case SettingsEntry.Quit: QuitGame(); return;
                case SettingsEntry.AddGold: GameManager.Instance?.economyManager?.AddGold(1000); return;
            }
            capturing = row; captureFrame = Time.frameCount;
            if (EventSystem.current != null)
            { previousNavigation = EventSystem.current.sendNavigationEvents; EventSystem.current.sendNavigationEvents = false; EventSystem.current.SetSelectedGameObject(null); }
            GameInput.BeginCapture(); Refresh();
        }
        public bool AcceptKey(KeyCode key)
        {
            if (capturing == null || !GameInput.IsBindable(key)) return false;
            var action = ActionFor(capturing.entry); CancelCapture(); GameInput.SetKey(action, key); Refresh(); return true;
        }
        public void CancelCapture()
        {
            if (capturing == null) return;
            capturing = null; GameInput.EndCapture();
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = previousNavigation;
        }
        public void Flush()
        { if (!savePending) return; savePending = false; DeviceSettings.Save(); }
        public void OpenCodex()
        {
            var codex = CodexUIController.Instance; if (codex == null) return;
            CancelCapture(); HideDropdowns(); Flush(); codex.OpenCodex();
        }
        public void ReturnToMainMenu()
        {
            if (SceneLoader.Instance == null) return;
            CloseThen(() => SceneLoader.Instance?.LoadStartScene());
        }
        public void QuitGame()
        {
            CloseThen(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                if (Application.platform != RuntimePlatform.WebGLPlayer) Application.Quit();
#endif
            });
        }
    }
}
