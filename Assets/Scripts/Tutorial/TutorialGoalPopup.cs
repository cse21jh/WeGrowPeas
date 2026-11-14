using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class TutorialGoalPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text expandedTitle;
    [SerializeField] private TMP_Text collapsedTitle;
    [SerializeField] private TextMeshProUGUI labelTemplate;

    private readonly List<(string key, TMP_Text label, GameObject check)> rows = new();
    private Tween fade;

    private void OnEnable() => Loc.LanguageChanged += RefreshLabels;

    private void OnDisable()
    {
        Loc.LanguageChanged -= RefreshLabels;
        fade?.Kill();
    }

    internal void Show(IReadOnlyList<string> keys)
    {
        const float rowHeight = 28f;
        var panel = (RectTransform)labelTemplate.transform.parent;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 48f + keys.Count * rowHeight);
        for (int i = 0; i < keys.Count; i++)
        {
            float y = -45f - i * rowHeight;
            var label = Instantiate(labelTemplate, panel, false);
            label.name = "Goal " + (i + 1);
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(-58f, rowHeight);
            rect.anchoredPosition = new Vector2(-12f, y);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 15f;
            label.color = new Color32(40, 38, 36, 255);
            label.raycastTarget = false;
            label.gameObject.SetActive(true);

            var edge = CreateBox("Checkbox", panel, new Vector2(1f, 1f),
                new Vector2(-18f, y), new Vector2(16f, 16f), new Color32(77, 72, 65, 255));
            var fill = CreateBox("Background", edge, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(12f, 12f), new Color32(255, 249, 235, 255));
            var tick = new GameObject("Checkmark", typeof(RectTransform));
            tick.transform.SetParent(fill, false);
            Color green = new Color32(46, 105, 59, 255);
            CreateBox("Short stroke", tick.transform, Vector2.one * 0.5f,
                new Vector2(-2.8f, -0.8f), new Vector2(5f, 2f), green).localRotation = Quaternion.Euler(0, 0, -45);
            CreateBox("Long stroke", tick.transform, Vector2.one * 0.5f,
                new Vector2(1.3f, 0.4f), new Vector2(9f, 2f), green).localRotation = Quaternion.Euler(0, 0, 50);
            tick.SetActive(false);
            rows.Add((keys[i], label, tick));
        }
        RefreshLabels();
    }

    internal void SetChecked(int row, bool value)
    {
        GameObject check = rows[row].check;
        if (check.activeSelf != value) check.SetActive(value);
    }

    internal void FadeOut()
    {
        var group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        fade = group.DOFade(0f, 0.4f).SetUpdate(true).SetLink(gameObject);
    }

    internal IEnumerator WaitForFade()
    {
        if (fade != null) yield return fade.WaitForCompletion();
    }

    private void RefreshLabels()
    {
        expandedTitle.text = collapsedTitle.text = Loc.T("tutorial.goals.title");
        foreach (var row in rows) row.label.text = Loc.T(row.key);
    }

    private static RectTransform CreateBox(string label, Transform parent, Vector2 anchor,
        Vector2 position, Vector2 size, Color color)
    {
        var image = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
