using TMPro;
using UnityEngine;

/// <summary>
/// TMP 텍스트를 번역 표의 키와 연결한다. 언어가 바뀌면 자동으로 다시 그린다.
/// 키가 표에 없으면 원문을 그대로 둔다.
///
/// 화면의 글자가 원문도, 이 컴포넌트가 넣은 번역도 아니면 코드가 넣은 값으로 보고 건드리지 않는다.
/// 그래서 코드가 채우는 텍스트에 붙어 있어도 값을 덮어쓰지 않는다.
/// 값을 직접 지정할 때는 <see cref="SetKey"/>를 쓴다.
///
/// 번역문은 원문보다 긴 경우가 많아 원문 언어가 아닐 때만 글자를 칸에 맞게 줄인다.
/// 원문부터 칸을 넘치게 쓰는 레이아웃은 줄이면 글자만 작아지므로 그대로 둔다.
/// </summary>
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    [Tooltip("연결할 때의 원문. 화면 값과 비교해 코드가 넣은 값을 구분한다.")]
    [SerializeField] private string sourceText;

    [Tooltip("번역문이 길 때 원래 글자 크기의 몇 배까지 줄일지. 칸이 아주 좁은 제목은 더 낮춘다.")]
    [Range(0.2f, 1f)]
    [SerializeField] private float minFontScale = 0.6f;

    private TMP_Text text;
    private string applied;
    private bool originalAutoSizing;
    private float originalFontSize;
    private bool? sourceFits;
    private TextWrappingModes originalWrapping;
    private (string name, object value)[] args;

    /// <summary>연결할 때의 원문.</summary>
    public string SourceText => sourceText;

    /// <summary>에디터 도구가 키와 원문을 연결할 때 쓴다.</summary>
    public void Link(string newKey, string source)
    {
        key = newKey;
        sourceText = source;
    }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.LogWarning($"[Loc] TMP 텍스트가 없는 오브젝트입니다: {name}", this);
            return;
        }

        if (string.IsNullOrEmpty(sourceText)) sourceText = text.text;

        originalAutoSizing = text.enableAutoSizing;
        originalFontSize = text.fontSize;
        originalWrapping = text.textWrappingMode;
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
    }

    /// <summary>키와 자리표시자 값을 바꾸고 다시 그린다.</summary>
    public void SetKey(string newKey, params (string name, object value)[] newArgs)
    {
        key = newKey;
        args = newArgs;
        Apply();
    }

    private void Refresh()
    {
        if (text == null || string.IsNullOrEmpty(key)) return;

        string current = text.text;
        if (current == sourceText || current == applied) Apply();
    }

    private void Apply()
    {
        if (text == null || string.IsNullOrEmpty(key)) return;

        applied = GameInput.FormatHints(Loc.Or(key, sourceText, args));
        text.text = applied;
        ApplyFit();
    }

    private void ApplyFit()
    {
        if (originalAutoSizing) return;

        bool shrink = Loc.Language != LocTable.SourceLanguage && SourceFits();
        text.enableAutoSizing = shrink;

        // 한 단어는 줄바꿈하면 중간에서 끊기므로 줄바꿈 없이 줄인다.
        bool singleWord = applied.IndexOf(' ') < 0 && applied.IndexOf('\n') < 0;
        text.textWrappingMode = shrink && singleWord ? TextWrappingModes.NoWrap : originalWrapping;

        if (shrink)
        {
            text.fontSizeMin = originalFontSize * minFontScale;
            text.fontSizeMax = originalFontSize;
        }
        else
        {
            text.fontSize = originalFontSize;
        }
    }

    private bool SourceFits()
    {
        if (sourceFits.HasValue) return sourceFits.Value;

        Rect rect = text.rectTransform.rect;
        if (rect.width < 1f || rect.height < 1f) return true; // 레이아웃이 아직 안 잡혔다

        Vector2 size = text.GetPreferredValues(sourceText, rect.width, 0f);
        bool wraps = text.textWrappingMode != TextWrappingModes.NoWrap;
        sourceFits = wraps ? size.y <= rect.height + 1f : size.x <= rect.width + 1f;
        return sourceFits.Value;
    }
}
