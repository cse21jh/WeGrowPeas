using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WeGrowPeas.UI.Book
{
    /// <summary>One archive entrance; the existing recall layouts remain owned by RecallUIController.</summary>
    [DisallowMultipleComponent]
    public sealed class ArchiveBookController : MonoBehaviour
    {
        public enum Tab { Recall, Codex, Achievement }

        [SerializeField] private GameObject panel;
        [SerializeField] private RecallUIController recall;
        [SerializeField] private BookLayoutController book;
        [SerializeField] private BookCloseMotion motion;
        [SerializeField] private ArchiveCatalogView codex;
        [SerializeField] private int recallLayout = 1;
        [SerializeField] private int codexLayout = 3;
        [SerializeField] private int achievementLayout = 4;
        [SerializeField] private Image currentIcon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Button currentButton;
        [SerializeField] private Sprite[] tabIcons;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private RectTransform menuRoot;
        [SerializeField] private Button[] menuButtons;
        [SerializeField] private Image[] menuIcons;
        [SerializeField] private TMP_Text[] menuLabels;
        [SerializeField] private GameObject[] recallNavigation;
        [Header("Tab transition (unscaled seconds)")]
        [SerializeField, FormerlySerializedAs("liftDuration"), Min(0.01f)] private float popOutDuration = 0.16f;
        [SerializeField, FormerlySerializedAs("returnDuration"), Min(0.01f)] private float popInDuration = 0.22f;
        [SerializeField, Min(0), Tooltip("버튼과 텍스트가 차례로 움직이기 시작하는 간격")]
        private float popStagger = 0.055f;
        [SerializeField, Range(0,0.3f), Tooltip("팝업 시 살짝 커지는 정도")]
        private float popOvershoot = 0.1f;
        [Header("탭 마우스 피드백")]
        [SerializeField, Range(.8f,1)] private float hoverScale = .95f;
        [SerializeField] private Color hoverTint = new Color(.86f,.86f,.86f,1);
        [SerializeField, Min(.01f)] private float hoverDuration = .1f;
        [SerializeField, Range(.7f,1)] private float pressedScale = .9f;
        [SerializeField, Min(.01f)] private float punchDuration = .16f;
        [SerializeField] private AnimationCurve punchCurve = new AnimationCurve(
            new Keyframe(0,.9f,0,0), new Keyframe(.24f,.86f,0,0),
            new Keyframe(.64f,1.08f,0,0), new Keyframe(1,1,0,0));
        private Tab current;
        private readonly Tab[] destinations = new Tab[2];
        private Coroutine transition;
        private Vector2 restingMenuPosition;
        [System.NonSerialized] private bool initialized;
        private bool wasOpen;
        private PopupElement[] popupElements;
        private TabFeedback[] feedback;

        private sealed class TabFeedback
        {
            public bool pointerInside, pressed, punching;
            public float hover, punchTime;
        }

        private sealed class PopupElement
        {
            public Graphic graphic;
            public Vector3 position;
            public Vector3 scale;
            public Vector3 centerOffset;
            public Color color;
            private float popupSize = 1, popupAlpha = 1, feedbackSize = 1;
            private Color feedbackTint = Color.white;

            public PopupElement(Graphic target)
            {
                graphic = target;
                var rect = target.rectTransform;
                position = rect.anchoredPosition3D;
                scale = rect.localScale;
                centerOffset = rect.localRotation * Vector3.Scale(rect.rect.center, scale);
                color = target.color;
            }

            public void Set(float size, float alpha)
            {
                popupSize = size; popupAlpha = alpha;
                Apply();
            }

            public void SetFeedback(float size,Color tint)
            {
                feedbackSize = size; feedbackTint = tint;
                Apply();
            }

            private void Apply()
            {
                float size = popupSize * feedbackSize;
                var rect = graphic.rectTransform;
                rect.localScale = scale * size;
                // Scale around the visible center without changing the authored pivot or placement.
                rect.anchoredPosition3D = position + centerOffset * (1 - size);
                var tinted = color * feedbackTint;
                tinted.a = color.a * popupAlpha;
                graphic.color = tinted;
            }
        }

        public Tab CurrentTab => current;
        public bool IsOpen => panel != null && panel.activeSelf;
        public bool IsSwitching { get; private set; }
        private int escapeFrame = -1;
        public bool ConsumedEscapeThisFrame => escapeFrame == Time.frameCount;
        public bool CanSwitch => IsOpen && !IsSwitching && !book.IsTurning &&
                                 !recall.HasResultOverlay && (motion == null || !motion.IsPlaying);

        private void Awake() => Initialize();
        private void OnEnable()
        {
            // Script reloads discard the runtime popup cache and event delegates while playing.
            Initialize();
            Loc.LanguageChanged += RefreshLanguage;
        }
        private void OnDisable()
        {
            Loc.LanguageChanged -= RefreshLanguage;
            CancelTransition();
        }

        private void OnDestroy()
        {
            if (!initialized || book == null) return;
            foreach (var layout in book.Layouts) layout.onShow.RemoveListener(RefreshNavigation);
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            restingMenuPosition = menuRoot.anchoredPosition;
            // The current tab is the top row of the same popup sequence as the other tabs.
            popupElements = new PopupElement[(menuButtons.Length + 1) * 2];
            popupElements[0] = new PopupElement(currentIcon);
            popupElements[1] = new PopupElement(title);
            feedback = new TabFeedback[menuButtons.Length + 1];
            for (int i = 0; i < feedback.Length; i++) feedback[i] = new TabFeedback();
            if (currentButton != null)
            {
                BindPointer(currentButton,0);
                currentButton.onClick.AddListener(PunchCurrentTab);
            }
            // Run after destination activation, before its page-turn snapshot is captured.
            foreach (var layout in book.Layouts) layout.onShow.AddListener(RefreshNavigation);
            for (int i = 0; i < menuButtons.Length; i++)
            {
                int slot = i;
                menuButtons[i].onClick.AddListener(() => ClickMenuTab(slot));
                BindPointer(menuButtons[i],i + 1);
                popupElements[(i + 1) * 2] = new PopupElement(menuIcons[i]);
                popupElements[(i + 1) * 2 + 1] = new PopupElement(menuLabels[i]);
            }
        }

        private void Update()
        {
            if (wasOpen && !IsOpen) CancelTransition();
            wasOpen = IsOpen;
            if (IsOpen && (current != Tab.Recall || IsSwitching) && Input.GetKeyDown(KeyCode.Escape))
            {
                escapeFrame = Time.frameCount;
                Close();
            }
            if (IsOpen) menuGroup.interactable = CanSwitch;
            if (currentButton != null) currentButton.interactable = CanSwitch;
        }

        private void BindPointer(Button button,int row)
        {
            var pointer = button.GetComponent<ArchiveTabPointer>() ?? button.gameObject.AddComponent<ArchiveTabPointer>();
            pointer.Configure(this,row);
        }

        public void SetTabPointer(int row,bool inside)
        {
            if (feedback == null || row < 0 || row >= feedback.Length) return;
            feedback[row].pointerInside = inside;
            if (!inside) feedback[row].pressed = false;
        }

        public void SetTabPressed(int row,bool pressed)
        {
            if (feedback == null || row < 0 || row >= feedback.Length) return;
            feedback[row].pressed = pressed && CanSwitch;
        }

        private void LateUpdate()
        {
            if (!initialized || !IsOpen || (motion != null && motion.IsPlaying)) return;
            for (int row = 0; row < feedback.Length; row++)
            {
                var state = feedback[row];
                if (book.Turner.ReduceMotion) state.punching = false;
                float target = state.pointerInside && CanSwitch ? 1 : 0;
                state.hover = Mathf.MoveTowards(state.hover,target,Time.unscaledDeltaTime / Mathf.Max(.01f,hoverDuration));
                float amount = Mathf.SmoothStep(0,1,state.hover);
                float size = book.Turner.ReduceMotion ? 1 : Mathf.Lerp(1,hoverScale,amount);
                if (!book.Turner.ReduceMotion && state.punching)
                {
                    state.punchTime += AnimationDelta;
                    size *= punchCurve != null && punchCurve.length > 0
                        ? punchCurve.Evaluate(Mathf.Clamp01(state.punchTime / Mathf.Max(.01f,punchDuration))) : 1;
                    if (state.punchTime >= punchDuration) state.punching = false;
                }
                else if (!book.Turner.ReduceMotion && state.pressed && CanSwitch) size *= pressedScale;
                var tint = Color.Lerp(Color.white,hoverTint,amount);
                popupElements[row * 2].SetFeedback(size,tint);
                popupElements[row * 2 + 1].SetFeedback(size,tint);
            }
        }

        private void StartPunch(int row)
        {
            feedback[row].pressed = false;
            feedback[row].punchTime = 0;
            feedback[row].punching = !book.Turner.ReduceMotion;
        }

        private void PunchCurrentTab()
        {
            if (CanSwitch) StartPunch(0);
        }

        private void ClickMenuTab(int slot)
        {
            if (!CanSwitch) return;
            int target = (int)destinations[slot];
            if (book.Turner.ReduceMotion) { SelectTab(target); return; }
            StartPunch(slot + 1);
            transition = StartCoroutine(SwitchTab((Tab)target,slot + 1));
        }

        public void Open() => OpenTab((int)current);

        public void OpenTab(int tab)
        {
            if (tab < 0 || tab > 2) return;
            Initialize();
            if (IsOpen) { SelectTab(tab); return; }
            current = (Tab)tab;
            panel.SetActive(true);
            recall.PrepareArchiveRecall();
            book.ShowImmediate(LayoutFor(current));
            RefreshHeader();
            RefreshNavigation();
            wasOpen = true;
            if (motion != null) motion.PlayOpen(null);
        }

        public void OpenCodexCategory(int category)
        {
            if (IsOpen && current == Tab.Codex)
            {
                codex.SelectCategory(category);
                return;
            }
            if (IsOpen && !CanSwitch) return;
            codex.SetCategoryBeforeOpen(category);
            OpenTab((int)Tab.Codex);
        }

        public void SelectTab(int tab)
        {
            if (tab < 0 || tab > 2 || tab == (int)current || !CanSwitch) return;
            if (book.Turner.ReduceMotion)
            {
                if (book.TryShow(LayoutFor((Tab)tab), false))
                {
                    current = (Tab)tab;
                    RefreshHeader();
                    RefreshNavigation();
                }
                return;
            }
            transition = StartCoroutine(SwitchTab((Tab)tab));
        }

        private IEnumerator SwitchTab(Tab target,int clickedRow = -1)
        {
            IsSwitching = true;
            menuGroup.interactable = false;
            if (clickedRow >= 0)
                while (feedback[clickedRow].punching) yield return null;
            yield return PopTabs(false);

            if (!book.TryShow(LayoutFor(target)))
            {
                transition = null;
                RestoreMotion();
                yield break;
            }
            current = target;
            RefreshHeader();
            RefreshNavigation();
            yield return PopTabs(true);
            while (book.IsTurning) yield return null;
            transition = null;
            RestoreMotion();
        }

        private IEnumerator PopTabs(bool appearing)
        {
            float duration = appearing ? popInDuration : popOutDuration;
            float total = duration + popStagger * (popupElements.Length - 1);
            for (float elapsed = 0; elapsed < total; elapsed += AnimationDelta)
            {
                for (int step = 0; step < popupElements.Length; step++)
                {
                    // Bottom row first on exit; top row first on entry. Icon precedes its label.
                    int row = appearing ? step / 2 : popupElements.Length / 2 - 1 - step / 2;
                    var element = popupElements[row * 2 + step % 2];
                    float t = Mathf.Clamp01((elapsed - popStagger * step) / duration);
                    float size;
                    if (appearing)
                    {
                        float u = t - 1;
                        float overshoot = popOvershoot * 17;
                        size = 1 + (overshoot + 1) * u * u * u + overshoot * u * u;
                    }
                    else if (t < .25f)
                        size = 1 + popOvershoot * Mathf.Sin(t * Mathf.PI * 2);
                    else
                        size = (1 + popOvershoot) * (1 - Mathf.SmoothStep(0,1,(t - .25f) / .75f));
                    float alpha = appearing ? Mathf.SmoothStep(0,1,t * 2) : 1 - Mathf.SmoothStep(0,1,t);
                    element.Set(size,alpha);
                }
                yield return null;
            }
            foreach (var element in popupElements) element.Set(appearing ? 1 : 0,appearing ? 1 : 0);
        }

        public void Close()
        {
            if (motion != null && motion.IsPlaying) return;
            CancelTransition();
            recall.CloseRecall();
        }

        private int LayoutFor(Tab tab) => tab == Tab.Recall ? recallLayout : tab == Tab.Codex ? codexLayout : achievementLayout;

        // A first-use page snapshot can stall a frame; still show the requested motion afterward.
        private static float AnimationDelta => Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        private static string Label(Tab tab) => tab == Tab.Recall
            ? Loc.Or("archive.recall", "회상") : tab == Tab.Codex
            ? Loc.Or("archive.codex", "도감") : Loc.Or("archive.achievement", "업적");

        private void RefreshHeader()
        {
            title.text = Label(current);
            title.maxVisibleCharacters = int.MaxValue;
            currentIcon.sprite = tabIcons[(int)current];
            int slot = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i == (int)current) continue;
                destinations[slot] = (Tab)i;
                menuIcons[slot].sprite = tabIcons[i];
                menuLabels[slot].text = Label((Tab)i);
                slot++;
            }
        }

        private void RefreshNavigation()
        {
            bool showRecall = book.CurrentIndex != codexLayout && book.CurrentIndex != achievementLayout;
            foreach (var control in recallNavigation)
                if (control != null) control.SetActive(showRecall);
        }

        private void RefreshLanguage()
        {
            if (!initialized) return;
            CancelTransition();
            RefreshHeader();
        }

        private void CancelTransition()
        {
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (initialized) RestoreMotion();
        }

        private void RestoreMotion()
        {
            IsSwitching = false;
            menuRoot.anchoredPosition = restingMenuPosition;
            menuGroup.alpha = 1;
            menuGroup.interactable = true;
            title.maxVisibleCharacters = int.MaxValue;
            if (feedback != null)
                foreach (var state in feedback)
                {
                    state.hover = 0; state.pressed = state.punching = false;
                }
            if (popupElements != null)
                foreach (var element in popupElements)
                {
                    element?.SetFeedback(1,Color.white);
                    element?.Set(1,1);
                }
        }
    }
}
