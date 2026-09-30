using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 카테고리에 "업적" 탭 버튼을 추가한다.
///
/// 도감(CodexRoot)은 StartScene의 DontDestroyCanvas 아래에 하나만 있고 게임 씬까지 살아남으므로
/// StartScene만 고치면 된다. 버튼 모양은 마지막 탭(벌레)을 복제해 맞춘다.
/// 카테고리 버튼 배열의 순서가 <see cref="CodexProgress.Category"/> 순서라서 업적 탭은 5번째(인덱스 4)에 둔다.
///
/// 이미 만든 탭이 있으면 다시 만들지 않고 연결만 맞춘다. 여러 번 실행해도 안전하다.
/// </summary>
public static class CodexAchievementTabSetup
{
    private const string StartScenePath = "Assets/Scenes/StartScene.unity";
    private const string ButtonName = "Btn_업적";
    private const string ButtonLabel = "업적";

    [MenuItem("Tools/Codex/Add Achievement Tab")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Codex] 씬을 열지 못했습니다: {StartScenePath}");
            return;
        }

        CodexUIController codex = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            codex = go.GetComponentInChildren<CodexUIController>(true);
            if (codex != null) break;
        }
        if (codex == null) { Debug.LogError("[Codex] StartScene에서 CodexUIController를 찾지 못했습니다."); return; }

        var so = new SerializedObject(codex);
        var buttons = so.FindProperty("categoryButtons");
        int index = (int)CodexProgress.Category.Achievement;
        int templateIndex = index - 1;

        if (buttons.arraySize <= templateIndex ||
            !(buttons.GetArrayElementAtIndex(templateIndex).objectReferenceValue is Button template))
        {
            Debug.LogError($"[Codex] 복제할 카테고리 버튼(인덱스 {templateIndex})이 없습니다.");
            return;
        }

        Transform parent = template.transform.parent;
        Transform existing = parent.Find(ButtonName);
        GameObject tab;
        if (existing != null)
        {
            tab = existing.gameObject;
        }
        else
        {
            tab = Object.Instantiate(template.gameObject, parent);
            tab.name = ButtonName;
            tab.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            // 레이아웃 그룹이 없는 경우를 대비해 템플릿 옆으로 한 칸 옮긴다.
            if (parent.GetComponent<LayoutGroup>() == null)
            {
                var rt = (RectTransform)tab.transform;
                rt.anchoredPosition += new Vector2(rt.rect.width + 10f, 0f);
            }
        }
        tab.SetActive(true);

        var label = tab.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = ButtonLabel;

        if (buttons.arraySize <= index) buttons.arraySize = index + 1;
        buttons.GetArrayElementAtIndex(index).objectReferenceValue = tab.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = tab;

        Debug.Log($"[Codex] 업적 탭 설치 완료: {ButtonName} (categoryButtons[{index}])");
    }
}
