using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TutorialChapterSelectionPopup : MonoBehaviour
{
    [Serializable]
    private struct ChapterButton
    {
        public Button button;
        public CanvasGroup opacity;
        public TMP_Text label;
    }

    [SerializeField] private ChapterButton[] chapters;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text closeLabel;

    private GameObject previousSelection;

    private void OnEnable()
    {
        SaveManager.OnProfileChanged += RefreshAccess;
        Loc.LanguageChanged += RefreshLabels;
        RefreshAccess();
        RefreshLabels();
    }

    private void OnDisable()
    {
        SaveManager.OnProfileChanged -= RefreshAccess;
        Loc.LanguageChanged -= RefreshLabels;
    }

    public void Open()
    {
        if (gameObject.activeSelf) return;

        var events = EventSystem.current;
        if (events != null) previousSelection = events.currentSelectedGameObject;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        if (events != null) events.SetSelectedGameObject(chapters[0].button.gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    public void SelectChapter(int chapter)
    {
        if (SceneLoader.Instance?.TryLoadTutorialChapter(chapter) == true)
            SoundManager.Instance?.PlayEffect(SoundIds.Button);
    }

    private void RefreshAccess()
    {
        for (int i = 0; i < chapters.Length; i++)
        {
            bool unlocked = SceneLoader.CanStartTutorialChapter(i + 1);
            chapters[i].button.interactable = unlocked;
            chapters[i].opacity.alpha = unlocked ? 1f : 0.5f;
        }
    }

    private void RefreshLabels()
    {
        titleLabel.text = Loc.T("game.tutorial.select_title");
        closeLabel.text = Loc.T("game.common.close");
        for (int i = 0; i < chapters.Length; i++)
            chapters[i].label.text = Loc.T("game.tutorial.chapter_label", ("chapter", i + 1));
    }
}
