using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 도감 카테고리에 "업적" 탭 버튼을 추가한다.
///
/// 도감(CodexRoot)은 시작 씬의 DontDestroyCanvas 아래에 하나만 있고 게임 씬까지 살아남으므로
/// 시작 씬만 고치면 된다. 버튼 모양은 마지막 탭(벌레)을 복제해 맞춘다.
/// 카테고리 버튼 배열의 순서가 <see cref="CodexProgress.Category"/> 순서라서 업적 탭은 5번째(인덱스 4)에 둔다.
///
/// StartScene 3에는 메인 화면 버튼 줄(SettingButtons)에 도감/업적 바로가기 버튼도 만든다.
/// 회상 버튼(HistoryBtn)을 복제해 모양을 맞추고, 아이콘은 일단 회상 버튼 것을 그대로 쓴다.
///
/// 이미 만든 버튼이 있으면 다시 만들지 않고 연결만 맞춘다. 여러 번 실행해도 안전하다.
/// </summary>
public static class CodexAchievementTabSetup
{
    private const string StartScenePath = "Assets/Scenes/StartScene.unity";
    private const string StartScene3Path = "Assets/Scenes/StartScene 3.unity";
    private const string ButtonName = "Btn_업적";
    private const string ButtonLabel = "업적";

    private const string MenuRowName = "SettingButtons";
    private const string MenuTemplateName = "HistoryBtn";
    private const string CodexMenuButtonName = "CodexBtn";
    private const string AchievementMenuButtonName = "AchievementBtn";

    [MenuItem("Tools/Codex/Add Achievement Tab")]
    public static void Build()
    {
        var scene = OpenScene(StartScenePath);
        if (!scene.IsValid()) return;

        var codex = FindCodex(scene);
        if (codex == null) return;

        var tab = InstallAchievementTab(codex);
        if (tab == null) return;

        Save(scene);
        Selection.activeObject = tab;
    }

    [MenuItem("Tools/Codex/Add Codex Buttons to StartScene 3")]
    public static void BuildStartScene3()
    {
        var scene = OpenScene(StartScene3Path);
        if (!scene.IsValid()) return;

        var codex = FindCodex(scene);
        if (codex == null) return;

        if (InstallAchievementTab(codex) == null) return;

        Transform row = FindInScene(scene, MenuRowName);
        Transform template = row != null ? row.Find(MenuTemplateName) : null;
        if (template == null)
        {
            Debug.LogError($"[Codex] {MenuRowName}/{MenuTemplateName}을 찾지 못했습니다.");
            return;
        }

        var codexBtn = InstallMenuButton(template, CodexMenuButtonName, "도감");
        UnityEventTools.AddVoidPersistentListener(codexBtn.onClick, codex.OpenCodex);

        var achievementBtn = InstallMenuButton(template, AchievementMenuButtonName, ButtonLabel);
        UnityEventTools.AddIntPersistentListener(achievementBtn.onClick, codex.OpenCategory,
            (int)CodexProgress.Category.Achievement);

        // 회상 버튼 바로 뒤에 도감, 업적 순으로 둔다.
        int at = template.GetSiblingIndex();
        codexBtn.transform.SetSiblingIndex(at + 1);
        achievementBtn.transform.SetSiblingIndex(at + 2);

        Save(scene);
        Selection.objects = new Object[] { codexBtn.gameObject, achievementBtn.gameObject };
        Debug.Log($"[Codex] StartScene 3에 도감/업적 버튼 설치 완료 ({MenuRowName})");
    }

    private static Scene OpenScene(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (!scene.IsValid()) Debug.LogError($"[Codex] 씬을 열지 못했습니다: {path}");
        return scene;
    }

    private static CodexUIController FindCodex(Scene scene)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var codex = go.GetComponentInChildren<CodexUIController>(true);
            if (codex != null) return codex;
        }
        Debug.LogError($"[Codex] {scene.name}에서 CodexUIController를 찾지 못했습니다.");
        return null;
    }

    private static GameObject InstallAchievementTab(CodexUIController codex)
    {
        var so = new SerializedObject(codex);
        var buttons = so.FindProperty("categoryButtons");
        int index = (int)CodexProgress.Category.Achievement;
        int templateIndex = index - 1;

        if (buttons.arraySize <= templateIndex ||
            !(buttons.GetArrayElementAtIndex(templateIndex).objectReferenceValue is Button template))
        {
            Debug.LogError($"[Codex] 복제할 카테고리 버튼(인덱스 {templateIndex})이 없습니다.");
            return null;
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

        Debug.Log($"[Codex] 업적 탭 설치 완료: {ButtonName} (categoryButtons[{index}])");
        return tab;
    }

    /// <summary>템플릿 버튼을 복제(또는 기존 것 재사용)하고 라벨을 바꾼 뒤 onClick을 비워 돌려준다.</summary>
    private static Button InstallMenuButton(Transform template, string name, string labelText)
    {
        Transform existing = template.parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : Object.Instantiate(template.gameObject, template.parent);
        go.name = name;
        go.SetActive(true);

        var label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = labelText;

        var button = go.GetComponent<Button>();
        ClearPersistentListeners(button.onClick);
        return button;
    }

    private static void ClearPersistentListeners(UnityEventBase evt)
    {
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(evt, i);
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        }
        return null;
    }

    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
