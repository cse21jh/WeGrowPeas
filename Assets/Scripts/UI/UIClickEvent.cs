using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
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

    private IEnumerator DelayAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

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
        /*string path = Application.dataPath + "/UserData.json";

        GetGameStartTypeFromSave();

        if (File.Exists(path) && GameStartContext.StartType != GameStartType.GameOver)
        {
            TransitionController.instance.Transition_Out();
            StartCoroutine(DelayAction(1.1f, () =>
            {
                //GameStartContext.SetStartType(GameStartType.ContinueGame);
                SceneLoader.Instance?.LoadGardenScene();
            }));
        }

        return;*/

        OnClickShowSaveSlotPanel();
    }

    public void OnClick_ContinueGameAfterEnding()
    {
        string path = SaveContext.Instance.CurrentSaveFilePath;

        string json = File.ReadAllText(path);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        // 안전장치: 파일에 잘못 기록된 새 게임/게임오버 상태를 이어하기 상태로 보정
        GameStartType startType = saveData.progress.gst;
        if (startType == GameStartType.NewGame || startType == GameStartType.GameOver || startType == GameStartType.None)
        {
            startType = GameStartType.ContinueGame;
        }
        GameStartContext.SetStartType(startType);

        //GameStartContext.SetStartType(GameStartType.ContinueGame);
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

    public void OnClick_GoToTutorial()
    {
        SceneLoader.Instance?.LoadTutorialScene();
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
        if (GameStartContext.StartType == GameStartType.GameOver)
        {
            continueEndlessButton.gameObject.SetActive(false);
        }

        restartPopup.SetActive(true);
    }

    public void OnClick_CloseGameOverPopup()
    {
        restartPopup.SetActive(false);
    }

    private void GetGameStartTypeFromSave()
    {
        string json = File.ReadAllText(Application.dataPath + "/UserData.json");
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        GameStartContext.SetStartType(saveData.progress.gst);
    }
    public void PlayButtonClickSound()
    {
        SoundManager.Instance.PlayEffect("Button");
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
}
