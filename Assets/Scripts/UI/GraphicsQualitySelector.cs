using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정창의 그래픽 품질 행 ("그래픽  &lt; 보통 &gt;").
/// <see cref="LanguageSelector"/>와 같은 방식으로 볼륨 행의 라벨을 복제해 실행 중에 만든다.
///
/// 설정창은 씬마다 따로 있고 남는 자리가 없어서, 패널을 한 줄만큼 늘리고
/// 맨 아래 설정 행 밑에 새 행을 넣은 뒤 그 아래 버튼들을 한 줄 내린다.
/// </summary>
public class GraphicsQualitySelector : MonoBehaviour
{
    private const float RowSpacing = 30f;
    private const float ArrowSize = 30f;
    private const float ArrowInset = 45f;

    private TMP_Text label;
    private TMP_Text value;

    /// <summary>
    /// lastRow: 지금 맨 아래에 있는 설정 행. 새 행은 그 바로 밑에 놓인다.
    /// labelReference: 글자 모양을 따를 행(배경음 볼륨 행).
    /// </summary>
    public static void AddTo(Transform panel, RectTransform lastRow, RectTransform labelReference)
    {
        var template = labelReference != null ? labelReference.GetComponentInChildren<TMP_Text>(true) : null;
        // 토글 컴포넌트가 행 안쪽 자식에 붙어 있을 수 있으므로 패널 바로 아래의 행까지 올라간다.
        while (lastRow != null && lastRow.parent != panel) lastRow = lastRow.parent as RectTransform;
        if (template == null || lastRow == null)
        {
            Debug.LogWarning($"[GraphicsQualitySelector] 설정창 행을 찾지 못해 그래픽 행을 만들지 않습니다: {panel.name}");
            return;
        }

        MakeRoom(panel, lastRow.anchoredPosition.y);

        var row = new GameObject(nameof(GraphicsQualitySelector), typeof(RectTransform));
        var rect = (RectTransform)row.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = lastRow.anchorMin;
        rect.anchorMax = lastRow.anchorMax;
        rect.pivot = lastRow.pivot;
        rect.sizeDelta = labelReference.sizeDelta;
        rect.anchoredPosition = new Vector2(labelReference.anchoredPosition.x, lastRow.anchoredPosition.y - RowSpacing);

        row.SetActive(false);
        var selector = row.AddComponent<GraphicsQualitySelector>();
        selector.Build(template);
        row.SetActive(true);
    }

    /// <summary>
    /// 패널을 한 줄 키운다. 가운데 기준 요소는 반 줄 올리고, 맨 아래 행보다 밑에 있던 버튼은 반 줄 내린다.
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
            // 화면 전체를 덮는 요소(Blocker, 단축키 화면)는 늘어난 패널에 맞춰 저절로 따라온다.
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
        GraphicsQuality.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Loc.LanguageChanged -= Refresh;
        GraphicsQuality.Changed -= Refresh;
    }

    private void Step(int direction)
    {
        // 끝에서 다시 처음으로 돌지 않는다. 낮음 왼쪽으로 가면 높음이 되는 건 헷갈린다.
        int next = Mathf.Clamp((int)GraphicsQuality.Current + direction,
            (int)GraphicsQualityLevel.Low, (int)GraphicsQualityLevel.High);
        if (next == (int)GraphicsQuality.Current) return;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayEffect("Button");
        GraphicsQuality.Set((GraphicsQualityLevel)next);
    }

    private void Refresh()
    {
        label.text = Loc.T("settings.graphics");
        value.text = GraphicsQuality.Current switch
        {
            GraphicsQualityLevel.Low => Loc.T("settings.graphics.low"),
            GraphicsQualityLevel.Medium => Loc.T("settings.graphics.medium"),
            _ => Loc.T("settings.graphics.high"),
        };
    }
}
