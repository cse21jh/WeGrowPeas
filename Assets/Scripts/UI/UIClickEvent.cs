using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public enum GameStartType
{
    NewGame,
    ContinueGame,
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

    private IEnumerator DelayAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    public void OnClick_StartNewGame()
    {
        TransitionController.instance.Transition_Out();
        StartCoroutine(DelayAction(1.1f, () =>
        {
            GameStartContext.SetStartType(GameStartType.NewGame);
            SceneLoader.Instance?.LoadGardenScene();
        }));
    }

    public void OnClick_ContinueGame()
    {
        string path = Application.dataPath + "/UserData.json";

        if(File.Exists(path) && GameStartContext.StartType != GameStartType.GameOver)
        {
            TransitionController.instance.Transition_Out();
            StartCoroutine(DelayAction(1.1f, () =>
            {
                GameStartContext.SetStartType(GameStartType.ContinueGame);
                SceneLoader.Instance?.LoadGardenScene();
            }));
        }

        return;
    }

    public void OnClick_GoToTutorial()
    {
        TransitionController.instance.Transition_Out();
        StartCoroutine(DelayAction(1.1f, () =>
        {
            SceneLoader.Instance?.LoadTutorialScene();
        }));
    }

    public void OnClick_PlayAgain()
    {
        TransitionController.instance.Transition_Out();
        StartCoroutine(DelayAction(1.1f, () =>
        {
            SceneLoader.Instance?.LoadStartScene();
        }));
    }

    public void OnClick_SaveAndReturnMain()
    {
        //GameEvents.RequestSaveGame();
        TransitionController.instance.Transition_Out();
        //SceneLoader.Instance?.LoadStartScene();
        StartCoroutine(DelayAction(1.1f, () =>
        {
            Debug.Log("Save And Return Main");
            SceneLoader.Instance?.LoadStartScene();
        }));
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
}
