using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum GameStartType
{
    NewGame,
    ContinueGame
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
    // Start is called before the first frame update
    public void OnClick_StartNewGame()
    {
        GameStartContext.SetStartType(GameStartType.NewGame);
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void OnClick_ContinueGame()
    {
        //StartType이 Continue가 아니거나 JSON이 존재하지 않으면 return;
        GameStartContext.SetStartType(GameStartType.ContinueGame);
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void OnClick_PlayAgain()
    {
        SceneLoader.Instance?.LoadStartScene();
    }

    public void OnClick_SaveAndReturnMain()
    {
        //SaveGame() 호출
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
