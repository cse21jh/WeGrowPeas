using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 설정창의 단축키 화면. 설정창이 씬마다 따로 있어서, 기존 라벨과 버튼을 복제해 실행 중에 만든다.
/// 폰트와 버튼 모양이 설정창과 자동으로 같아진다.
/// </summary>
public class KeyBindingPanel : MonoBehaviour
{
    private const float RowHeight = 24f;
    private const float FirstRowY = 62f;
    private const float LabelX = -75f;
    private const float KeyButtonX = 95f;
    private static readonly Vector2 KeyButtonSize = new Vector2(120f, 22f);
    private static readonly Vector2 FooterButtonSize = new Vector2(100f, 32f);
    private static readonly Vector2 OpenerPosition = new Vector2(-158.4f, -41f);
    private static readonly Vector2 OpenerSize = new Vector2(89.4f, 39f);

    private static int _escapeConsumedFrame = -1;

    private class Row
    {
        public GameAction action;
        public TMP_Text label;
        public TMP_Text key;
    }

    private readonly List<Row> rows = new List<Row>();
    private TMP_Text title;
    private TMP_Text resetLabel;
    private TMP_Text closeLabel;
    private TMP_Text openLabel;
    private GameAction? capturing;

    /// <summary>단축키 화면이 ESC를 처리했거나 처리할 상태면 설정창은 닫지 않는다.</summary>
    public static bool BlocksEscape => _escapeConsumedFrame == Time.frameCount;

    /// <summary>설정창에 단축키 버튼과 화면을 추가한다.</summary>
    public static void AddTo(Transform panel, RectTransform labelReference)
    {
        var labelTemplate = labelReference != null ? labelReference.GetComponentInChildren<TMP_Text>(true) : null;
        // 튜토리얼 설정창에는 도감 버튼이 없어서 종료 버튼을 쓴다.
        var buttonTemplate = (panel.Find("CodexButton") ?? panel.Find("MainMenuButton")) as RectTransform;
        if (labelTemplate == null || buttonTemplate == null)
        {
            Debug.LogWarning("[KeyBindingPanel] 설정창의 라벨이나 버튼을 찾지 못해 단축키 화면을 만들지 않습니다.");
            return;
        }

        var root = new GameObject(nameof(KeyBindingPanel), typeof(RectTransform));
        var rect = (RectTransform)root.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 컴포넌트 활성화 시점에 그려지므로 내용을 모두 만든 뒤에 켠다.
        root.SetActive(false);
        var view = root.AddComponent<KeyBindingPanel>();
        view.Build(panel, labelTemplate, buttonTemplate);
    }

    private void Build(Transform panel, TMP_Text labelTemplate, RectTransform buttonTemplate)
    {
        var background = gameObject.AddComponent<Image>();
        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            background.sprite = panelImage.sprite;
            background.type = panelImage.type;
            background.color = panelImage.color;
            background.pixelsPerUnitMultiplier = panelImage.pixelsPerUnitMultiplier;
        }

        var opener = CreateButton(buttonTemplate, panel, OpenerPosition, OpenerSize, Open);
        openLabel = opener.GetComponentInChildren<TMP_Text>(true);

        title = CreateLabel(labelTemplate, new Vector2(0f, 95f), new Vector2(300f, 30f), TextAlignmentOptions.Center);

        for (int i = 0; i < GameInput.Actions.Length; i++)
        {
            GameAction action = GameInput.Actions[i];
            float y = FirstRowY - i * RowHeight;

            var keyButton = CreateButton(buttonTemplate, transform, new Vector2(KeyButtonX, y), KeyButtonSize, () => BeginCapture(action));
            rows.Add(new Row
            {
                action = action,
                label = CreateLabel(labelTemplate, new Vector2(LabelX, y), new Vector2(180f, RowHeight), TextAlignmentOptions.Left),
                key = keyButton.GetComponentInChildren<TMP_Text>(true),
            });
        }

        var reset = CreateButton(buttonTemplate, transform, new Vector2(-60f, -92f), FooterButtonSize, ResetAll);
        resetLabel = reset.GetComponentInChildren<TMP_Text>(true);
        var close = CreateButton(buttonTemplate, transform, new Vector2(60f, -92f), FooterButtonSize, Close);
        closeLabel = close.GetComponentInChildren<TMP_Text>(true);

        Loc.LanguageChanged += RefreshOpener;
        RefreshOpener();
    }

    private TMP_Text CreateLabel(TMP_Text template, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        var text = Instantiate(template, transform, false);
        var localized = text.GetComponent<LocalizedText>();
        if (localized != null) Destroy(localized);

        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMax = template.fontSize;
        text.fontSizeMin = 10f;

        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private static Button CreateButton(RectTransform template, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var clone = Instantiate(template, parent, false);
        clone.anchoredPosition = position;
        clone.sizeDelta = size;

        foreach (var localized in clone.GetComponentsInChildren<LocalizedText>(true))
            Destroy(localized);

        var text = clone.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = 10f;
        }

        // 복제한 도감 버튼의 클릭 연결을 지운다.
        var button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);
        return button;
    }

    private void OnEnable()
    {
        Loc.LanguageChanged += Refresh;
        GameInput.BindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= Refresh;
        GameInput.BindingsChanged -= Refresh;
        CancelCapture();
    }

    private void OnDestroy()
    {
        Loc.LanguageChanged -= RefreshOpener;
    }

    private void Update()
    {
        if (capturing.HasValue)
        {
            UpdateCapture();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _escapeConsumedFrame = Time.frameCount;
            Close();
        }
    }

    private void UpdateCapture()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _escapeConsumedFrame = Time.frameCount;
            CancelCapture();
            Refresh();
            return;
        }

        if (!Input.anyKeyDown) return;

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (!GameInput.IsBindable(key) || !Input.GetKeyDown(key)) continue;

            GameAction action = capturing.Value;
            capturing = null;
            GameInput.EndCapture();
            GameInput.SetKey(action, key);
            Refresh();
            return;
        }
    }

    private void BeginCapture(GameAction action)
    {
        PlayClick();
        // 선택된 버튼이 남아 있으면 Space나 Enter가 버튼을 다시 누른다.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        capturing = action;
        GameInput.BeginCapture();
        Refresh();
    }

    private void CancelCapture()
    {
        if (!capturing.HasValue) return;
        capturing = null;
        GameInput.EndCapture();
    }

    private void Open()
    {
        PlayClick();
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    private void Close()
    {
        PlayClick();
        gameObject.SetActive(false);
    }

    private void ResetAll()
    {
        PlayClick();
        CancelCapture();
        GameInput.ResetToDefaults();
    }

    private static void PlayClick()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
    }

    private void RefreshOpener()
    {
        openLabel.text = Loc.T("settings.keybinding.title");
    }

    private void Refresh()
    {
        title.text = Loc.T("settings.keybinding.title");
        resetLabel.text = Loc.T("settings.keybinding.reset");
        closeLabel.text = Loc.T("game.common.close");

        foreach (var row in rows)
        {
            row.label.text = Loc.T("settings.keybinding." + row.action);
            row.key.text = capturing == row.action ? Loc.T("settings.keybinding.press") : GameInput.GetKeyName(row.action);
        }
    }
}
