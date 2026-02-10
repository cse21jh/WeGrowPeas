using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class EditorSceneStartConfig
{
    private const string MENU_PATH = "Tools/Always Start From StartScene";
    private const string START_SCENE_PATH = "Assets/Scenes/StartScene.unity";

    [MenuItem(MENU_PATH)]
    private static void ToggleStartScene()
    {
        // 현재 설정된 시작 씬이 있는지 확인 (있으면 끄고, 없으면 켬)
        bool isEnabled = EditorSceneManager.playModeStartScene != null;

        if (isEnabled)
        {
            // 기능 끄기
            EditorSceneManager.playModeStartScene = null;
            Debug.Log("[Editor] Start Scene Disabled. Play mode will start from the open scene.");
        }
        else
        {
            // 기능 켜기
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(START_SCENE_PATH);
            if (sceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
                Debug.Log($"[Editor] Start Scene Enabled. Play mode will always start from: {START_SCENE_PATH}");
            }
            else
            {
                Debug.LogError($"[Editor] Could not find StartScene.unity at {START_SCENE_PATH}");
            }
        }
    }

    // 메뉴 아이템의 체크 상태를 업데이트하는 함수 (메뉴가 열릴 때 호출됨)
    [MenuItem(MENU_PATH, true)]
    private static bool ValidateToggleStartScene()
    {
        Menu.SetChecked(MENU_PATH, EditorSceneManager.playModeStartScene != null);
        return true;
    }
}
