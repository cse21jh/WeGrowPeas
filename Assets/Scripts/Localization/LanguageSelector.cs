using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정창의 언어 선택 행 ("언어  &lt; 한국어 &gt;").
/// 설정창이 씬마다 따로 있어서, 공통으로 있는 배경음 볼륨 행의 라벨을 복제해 실행 중에 만든다.
/// 폰트와 글자 스타일이 설정창과 자동으로 같아진다.
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    private const float RowSpacing = 30f;
    private const float ArrowSize = 30f;
    private const float ArrowInset = 45f; // 슬라이더 구간 안쪽에 맞춰 왼쪽 라벨과 떨어뜨린다
    private const float LabelInset = 12f; // 같은 줄의 설정창 제목과 겹치지 않게 민다

    private TMP_Text label;
    private TMP_Text value;

    /// <summary>패널에 언어 행을 추가한다. reference는 위치와 글자 모양을 따를 행이다. 고를 언어가 하나뿐이면 만들지 않는다.</summary>
    public static void AddTo(Transform panel, RectTransform reference)
    {
        if (Loc.AvailableLanguages.Count < 2) return;

        var template = reference != null ? reference.GetComponentInChildren<TMP_Text>(true) : null;
        if (template == null)
        {
            Debug.LogWarning($"[LanguageSelector] 볼륨 행의 라벨을 찾지 못해 언어 행을 만들지 않습니다: {panel.name}");
            return;
        }

        var row = new GameObject(nameof(LanguageSelector), typeof(RectTransform));
        var rect = (RectTransform)row.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = reference.anchorMin;
        rect.anchorMax = reference.anchorMax;
        rect.pivot = reference.pivot;
        rect.sizeDelta = reference.sizeDelta;
        rect.anchoredPosition = reference.anchoredPosition + Vector2.up * RowSpacing;

        // 컴포넌트 활성화 시점에 그려지므로 내용을 모두 만든 뒤에 붙인다.
        row.SetActive(false);
        var selector = row.AddComponent<LanguageSelector>();
        selector.Build(template);
        row.SetActive(true);
    }

    private void Build(TMP_Text template)
    {
        label = Instantiate(template, transform, false);
        label.rectTransform.anchoredPosition += Vector2.right * LabelInset;
        value = CreateText(template, Vector2.zero, Vector2.one, Vector2.zero);

        CreateArrow(template, "<", 0f, -1);
        CreateArrow(template, ">", 1f, 1);
    }

    private TMP_Text CreateText(TMP_Text template, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var text = Instantiate(template, transform, false);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return text;
    }

    private void CreateArrow(TMP_Text template, string glyph, float anchorX, int direction)
    {
        var anchor = new Vector2(anchorX, 0.5f);
        var text = CreateText(template, anchor, anchor, Vector2.one * ArrowSize);
        text.text = glyph;
        text.rectTransform.anchoredPosition = Vector2.left * direction * ArrowInset;
        text.raycastTarget = true;

        var button = text.gameObject.AddComponent<Button>();
        button.targetGraphic = text;
        button.onClick.AddListener(() => Step(direction));
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

    private void Step(int direction)
    {
        var languages = Loc.AvailableLanguages.ToList();
        int next = (languages.IndexOf(Loc.Language) + direction + languages.Count) % languages.Count;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
        Loc.SetLanguage(languages[next]);
    }

    private void Refresh()
    {
        label.text = Loc.T("settings.language");
        value.text = Loc.GetLanguageName(Loc.Language);
    }
}
