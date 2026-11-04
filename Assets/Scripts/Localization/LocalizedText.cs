using TMPro;
using UnityEngine;

/// <summary>
/// TMP 텍스트를 번역 표의 키와 연결한다. 언어가 바뀌면 자동으로 다시 그린다.
/// 키가 표에 없으면 인스펙터에 적힌 문구를 그대로 둔다.
///
/// 코드가 .text를 직접 넣는 오브젝트에는 붙이지 않는다. 언어를 바꿀 때 덮어쓰게 된다.
/// 그런 곳은 <see cref="SetKey"/>를 쓰거나 코드에서 <see cref="Loc"/>를 호출한다.
/// </summary>
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    private TMP_Text text;
    private string originalText;
    private (string name, object value)[] args;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        if (text != null) originalText = text.text;
        else Debug.LogWarning($"[Loc] TMP 텍스트가 없는 오브젝트입니다: {name}", this);
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
        Refresh();
    }

    private void Refresh()
    {
        if (text == null || string.IsNullOrEmpty(key)) return;
        text.text = Loc.Or(key, originalText, args);
    }
}
