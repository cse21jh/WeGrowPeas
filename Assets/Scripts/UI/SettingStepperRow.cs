using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정창의 "라벨  &lt; 값 &gt;" 한 줄. 볼륨 행의 라벨을 복제해 실행 중에 만든다.
///
/// 설정창은 씬마다 따로 있고 남는 자리가 없어서, 패널을 한 줄만큼 늘리고
/// 맨 아래 설정 행 밑에 새 행을 넣은 뒤 그 아래 버튼들을 한 줄 내린다.
/// </summary>
public class SettingStepperRow : MonoBehaviour
{
    private const float RowSpacing = 30f;
    private const float ArrowSize = 30f;
    private const float ArrowInset = 45f;

    private TMP_Text label;
    private TMP_Text value;
    private Func<string> labelText;
    private Func<string> valueText;
    private Action<int> step;

    /// <summary>
    /// lastRow: 지금 맨 아래에 있는 설정 행. 새 행은 그 바로 밑에 놓인다.
    /// labelReference: 글자 모양을 따를 행(배경음 볼륨 행).
    /// step: 화살표를 눌렀을 때 호출된다(왼쪽 -1, 오른쪽 +1). 값이 바뀌면 행이 다시 그려진다.
    /// 만들지 못하면 null.
    /// </summary>
    public static SettingStepperRow AddTo(Transform panel, RectTransform lastRow, RectTransform labelReference,
        string name, Func<string> labelText, Func<string> valueText, Action<int> step)
    {
        var template = labelReference != null ? labelReference.GetComponentInChildren<TMP_Text>(true) : null;
        // 토글 컴포넌트가 행 안쪽 자식에 붙어 있을 수 있으므로 패널 바로 아래의 행까지 올라간다.
        while (lastRow != null && lastRow.parent != panel) lastRow = lastRow.parent as RectTransform;
        if (template == null || lastRow == null)
        {
            Debug.LogWarning($"[SettingStepperRow] 설정창 행을 찾지 못해 {name} 행을 만들지 않습니다.");
            return null;
        }

        MakeRoom(panel, lastRow.anchoredPosition.y);

        var row = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)row.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = lastRow.anchorMin;
        rect.anchorMax = lastRow.anchorMax;
        rect.pivot = lastRow.pivot;
        rect.sizeDelta = labelReference.sizeDelta;
        rect.anchoredPosition = new Vector2(labelReference.anchoredPosition.x, lastRow.anchoredPosition.y - RowSpacing);

        row.SetActive(false);
        var stepper = row.AddComponent<SettingStepperRow>();
        stepper.labelText = labelText;
        stepper.valueText = valueText;
        stepper.step = step;
        stepper.Build(template);
        row.SetActive(true);
        MoveSideButtonsOffRow(panel, rect);
        return stepper;
    }

    /// <summary>
    /// 새 행과 같은 높이에 걸친 양옆 버튼(단축키, 도감)을 맨 아래 버튼 줄로 내린다.
    /// 긴 라벨이 버튼에 가려지지 않게 하기 위함이며, 맨 아래 버튼과 가로로 겹치는 버튼은 옮기지 않는다.
    /// </summary>
    private static void MoveSideButtonsOffRow(Transform panel, RectTransform row)
    {
        RectTransform bottom = null;
        foreach (RectTransform child in panel)
        {
            if (child == row || child.anchorMin != child.anchorMax || child.GetComponent<Button>() == null) continue;
            if (!Mathf.Approximately(child.anchorMin.y, 0.5f)) continue;
            if (bottom == null || child.anchoredPosition.y < bottom.anchoredPosition.y) bottom = child;
        }
        if (bottom == null) return;

        float rowHalf = row.rect.height * 0.5f;
        float rowY = row.anchoredPosition.y;
        foreach (RectTransform child in panel)
        {
            if (child == row || child == bottom || child.anchorMin != child.anchorMax || child.GetComponent<Button>() == null) continue;
            if (!Mathf.Approximately(child.anchorMin.y, 0.5f)) continue;

            float halfHeight = child.rect.height * 0.5f;
            if (Mathf.Abs(child.anchoredPosition.y - rowY) >= rowHalf + halfHeight) continue;

            float gap = Mathf.Abs(child.anchoredPosition.x - bottom.anchoredPosition.x);
            if (gap < (child.rect.width + bottom.rect.width) * 0.5f) continue;

            child.anchoredPosition = new Vector2(child.anchoredPosition.x, bottom.anchoredPosition.y);
        }
    }

    /// <summary>
    /// 패널을 한 줄 키운다. 가운데 기준 요소는 반 줄 올리고, 맨 아래 행보다 밑에 있던 요소는 반 줄 내린다.
    /// 위·아래 여백은 그대로 유지된다.
    /// </summary>
    private static void MakeRoom(Transform panel, float lastRowY)
    {
        var panelRect = (RectTransform)panel;
        float panelHeight = panelRect.rect.height;
        panelRect.sizeDelta += new Vector2(0f, RowSpacing);

        float half = RowSpacing * 0.5f;
        foreach (RectTransform child in panel)
        {
            // 화면 전체를 덮는 요소(Blocker, 단축키 화면)는 늘어난 패널에 맞춰 저절로 커진다.
            if (child.anchorMin != child.anchorMax) continue;
            // 위·아래 모서리 기준 요소(제목, 닫기 버튼)는 모서리가 움직이므로 그대로 둔다.
            if (!Mathf.Approximately(child.anchorMin.y, 0.5f)) continue;

            // 패널과 같은 높이의 배경 이미지는 함께 키운다.
            if (child.rect.height >= panelHeight - 1f)
            {
                child.sizeDelta += new Vector2(0f, RowSpacing);
                continue;
            }

            bool below = child.anchoredPosition.y < lastRowY - 1f;
            child.anchoredPosition += new Vector2(0f, below ? -half : half);
        }
    }

    private void Build(TMP_Text template)
    {
        label = Instantiate(template, transform, false);
        // 복제한 라벨에 번역 컴포넌트가 있으면 볼륨 문구로 되돌리므로 지운다.
        var localized = label.GetComponent<LocalizedText>();
        if (localized != null) Destroy(localized);
        // 긴 라벨("무한 모드 (테스트)")이 두 줄로 꺾이지 않게 한 줄에서 글자를 줄인다.
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMax = template.fontSize;
        label.fontSizeMin = 10f;

        value = CreateText(template, Vector2.zero, Vector2.one, Vector2.zero);

        CreateArrow(template, "<", 0f, -1);
        CreateArrow(template, ">", 1f, 1);
    }

    private TMP_Text CreateText(TMP_Text template, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var text = Instantiate(template, transform, false);
        var localized = text.GetComponent<LocalizedText>();
        if (localized != null) Destroy(localized);
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
        button.onClick.AddListener(() => OnArrow(direction));
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

    private void OnArrow(int direction)
    {
        string before = valueText();
        step(direction);
        if (valueText() == before) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect(SoundIds.Button);
        Refresh();
    }

    public void Refresh()
    {
        if (label == null || labelText == null || valueText == null) return;
        label.text = labelText();
        value.text = valueText();
    }
}
