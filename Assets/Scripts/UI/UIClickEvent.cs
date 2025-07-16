using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIClickEvent : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnClick_StartGame()
    {
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void OnClick_PlayAgain()
    {
        SceneLoader.Instance?.LoadStartScene();
    }

    public void OnClick_SaveAndReturnMain()
    {
        //SaveGame() »£√‚
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
