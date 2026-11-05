using TMPro;
using UnityEngine;

/// <summary>
/// TMP 텍스트를 번역 표의 키와 연결한다. 언어가 바뀌면 자동으로 다시 그린다.
/// 키가 표에 없으면 원문을 그대로 둔다.
///
/// 화면의 글자가 원문도, 이 컴포넌트가 넣은 번역도 아니면 코드가 넣은 값으로 보고 건드리지 않는다.
/// 그래서 코드가 채우는 텍스트에 붙어 있어도 값을 덮어쓰지 않는다.
/// 값을 직접 지정할 때는 <see cref="SetKey"/>를 쓴다.
/// </summary>
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    [Tooltip("연결할 때의 원문. 화면 값과 비교해 코드가 넣은 값을 구분한다.")]
    [SerializeField] private string sourceText;

    private TMP_Text text;
    private string applied;
    private (string name, object value)[] args;

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
    }

    private void OnEnable()
    {
        Loc.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= Refresh;
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

        applied = Loc.Or(key, sourceText, args);
        text.text = applied;
    }
}
