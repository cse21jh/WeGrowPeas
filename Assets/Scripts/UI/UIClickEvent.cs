using UnityEngine;
using UnityEngine.UI;


public enum GameStartType
{
    None,
    NewGame,
    ContinueGame,
    ContinueAfterEnding,
    GameOver
}

public static class GameStartContext
{
    public static GameStartType StartType { get; private set; } = GameStartType.NewGame;

    public static void SetStartType(GameStartType type)
    {
        StartType = type;
    }
}

public class UIClickEvent : MonoBehaviour
{
    [SerializeField] private Button continueEndlessButton;
    [SerializeField] private GameObject restartPopup;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject saveSlotPanel;
    [SerializeField] private TutorialChapterSelectionPopup tutorialChapterSelectionPopup;

    [Header("치장 씬")]
    [Tooltip("치장 씬의 확인 창. 치장 씬에서만 연결한다.")]
    [SerializeField] private CustomizePopup customizePopup;
    [Tooltip("치장 씬의 계절 미리보기. 치장 씬에서만 연결한다.")]
    [SerializeField] private CustomizeSeasonPreview customizeSeasonPreview;

    private bool isLeavingCustomize;

    // 씬 전환 연출(덮기/열기)은 SceneLoader가 담당한다. 여기서 Transition_Out을 직접 부르지 않는다.
    public void OnClick_StartNewGame(int slotIndex)
    {
        SaveContext.Instance.SelectSlot(slotIndex);

        GameStartContext.SetStartType(GameStartType.NewGame);
        SceneLoader.Instance?.LoadGardenScene();

        //OnClickShowSaveSlotPanel();
    }

    public void OnClick_ContinueGame()
    {
        OnClickShowSaveSlotPanel();
    }

    public void OnClick_ContinueGameAfterEnding()
    {
        string path = SaveContext.Instance.CurrentSaveFilePath;
        if (!RunSave.TryLoad(path, out SaveData saveData))
        {
            Debug.LogError($"[UIClickEvent] 세이브를 읽지 못했습니다: {path}");
            return;
        }
        GameStartContext.SetStartType(RunSave.ResolveContinueStartType(saveData));

        SceneLoader.Instance?.LoadGardenScene();
    }

    /// <summary>회상 화면 열기. 회상 UI는 씬에 항상 켜진 루트로 들어 있다.</summary>
    public void OnClick_OpenRecall()
    {
        var recall = FindAnyObjectByType<RecallUIController>();
        if (recall == null)
        {
            Debug.LogWarning("[Recall] 씬에 회상 UI(RecallUIController)가 없습니다.");
            return;
        }

        recall.OpenRecall();
    }

    public void OnClick_GoToCustomizeScene()
    {
        SceneLoader.Instance?.LoadCustomizeScene();
    }

    /// <summary>현재 씬의 아카이브를 연다. 씬을 떠나기 전 책 인스턴스를 참조하지 않는다.</summary>
    public void OnClick_OpenArchive()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var archive in FindObjectsByType<WeGrowPeas.UI.Book.ArchiveBookController>(FindObjectsSortMode.None))
        {
            if (archive.gameObject.scene != scene || !archive.isActiveAndEnabled) continue;
            archive.Open();
            return;
        }

        Debug.LogWarning("[Archive] 현재 씬에 아카이브 UI(ArchiveBookController)가 없습니다.", this);
    }

    public void OnClick_GoToTutorial()
    {
        SceneLoader.Instance?.LoadTutorialScene();
    }

    public void OnClick_OpenTutorialSelection()
    {
        tutorialChapterSelectionPopup?.Open();
    }

    public void OnClick_GoToIntro()
    {
        SceneLoader.Instance?.LoadIntroScene();
    }

    public void OnClick_PlayAgain()
    {
        SceneLoader.Instance?.LoadStartScene();
    }

    public void OnClick_SaveAndReturnMain()
    {
        //GameEvents.RequestSaveGame();
        Debug.Log("Save And Return Main");
        SceneLoader.Instance?.LoadStartScene();
    }

    public void OnClick_QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClick_OpenGameOverPopup()
    {
        // 게임오버였거나 무한 모드(테스트)가 꺼져 있으면 엔딩 뒤 이어하기를 숨긴다.
        if (GameStartContext.StartType == GameStartType.GameOver || !EndlessMode.Enabled)
        {
            continueEndlessButton.gameObject.SetActive(false);
        }

        restartPopup.SetActive(true);
    }

    public void OnClick_CloseGameOverPopup()
    {
        restartPopup.SetActive(false);
    }

    public void PlayButtonClickSound()
    {
        SoundManager.Instance.PlayEffect(SoundIds.Button);
    }

    public void OnClickShowSaveSlotPanel()
    {
        buttonPanel.SetActive(false);
        saveSlotPanel.SetActive(true);
    }

    public void OnClickHideSaveSlotPanel()
    {
        buttonPanel.SetActive(true);
        saveSlotPanel.SetActive(false);
    }

    // ── 치장 씬 ───────────────────────────────────────────────────────────────

    /// <summary>치장 씬 되돌리기: 마지막으로 저장한 치장 상태로 되돌린다.</summary>
    public void OnClick_CustomizeUndo()
    {
        if (isLeavingCustomize) return;
        if (!CustomizeState.HasUnsavedChanges)
        {
            customizePopup.Notice("되돌릴 변경 사항이 없어요.");
            return;
        }
        CustomizeState.RevertToSaved();
    }

    /// <summary>치장 씬 저장하기: 지금 치장 상태를 저장한다.</summary>
    public void OnClick_CustomizeSave()
    {
        if (isLeavingCustomize) return;
        CustomizeState.Save();
        customizePopup.Notice("현재 치장 상태를 저장했어요.");
    }

    /// <summary>치장 씬 메인으로: 시작 화면으로 돌아간다. 저장하지 않은 변경이 있으면 먼저 묻는다.</summary>
    public void OnClick_CustomizeGoToMain()
    {
        if (isLeavingCustomize) return;
        if (!CustomizeState.HasUnsavedChanges)
        {
            CustomizeLeaveToMain();
            return;
        }

        customizePopup.Ask("현재 상태가 저장되지 않았습니다.\n정말 나가시겠습니까?", "저장하지 않고 나가기", () =>
        {
            // 저장하지 않은 장착이 메모리에 남아 다른 화면에 보이지 않도록 되돌린 뒤 나간다.
            CustomizeState.RevertToSaved();
            CustomizeLeaveToMain();
        });
    }

    /// <summary>치장 씬 계절 버튼: 미리보기 계절을 다음 계절로 넘긴다(여름 → 가을 → 겨울 → 봄).</summary>
    public void OnClick_CustomizeNextSeason()
    {
        if (isLeavingCustomize || customizeSeasonPreview == null) return;
        customizeSeasonPreview.NextSeason();
    }

    private void CustomizeLeaveToMain()
    {
        isLeavingCustomize = true;
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadStartScene();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.StartSceneName); // 치장 씬을 바로 Play한 경우
    }
}
