using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;


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
    public void OnClick_StartNewGame()
    {
        GameStartContext.SetStartType(GameStartType.NewGame);
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void OnClick_ContinueGame()
    {
        string path = Application.dataPath + "/UserData.json";

        if(File.Exists(path) && GameStartContext.StartType != GameStartType.GameOver)
        {
            GameStartContext.SetStartType(GameStartType.ContinueGame);
            SceneLoader.Instance?.LoadGardenScene();
        }

        return;
    }

    public void OnClick_PlayAgain()
    {
        SceneLoader.Instance?.LoadStartScene();
    }

    public void OnClick_SaveAndReturnMain()
    {
        //GameEvents.RequestSaveGame();
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
}
