using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정창에 도감 버튼을 만들어 붙인다.
///
/// 도감(CodexRoot)은 DontDestroyCanvas 아래에 있고 TransitionController가 그 캔버스를
/// DontDestroyOnLoad로 넘기므로, StartScene에서 한 번 만들어지면 게임 씬까지 살아남는다.
/// 그래서 버튼은 씬마다 따로 두되 여는 대상은 런타임에 CodexUIController.Instance로 찾는다.
/// (프리팹의 onClick은 다른 씬의 오브젝트를 직접 참조할 수 없기 때문이다.)
///
/// 대상:
///   Canvas.prefab   — 게임 씬(Garden)의 설정창
///   StartScene      — 시작 화면의 설정창
///
/// 이미 만들어 둔 버튼이 있으면 다시 만들지 않고 연결만 맞춘다. 여러 번 실행해도 안전하다.
/// </summary>
public static class CodexButtonSetup
{
    private const string CanvasPrefabPath = "Assets/Resource/Prefabs/Canvas.prefab";
    private const string StartScenePath = "Assets/Scenes/StartScene.unity";

    private const string ButtonName = "CodexButton";
    private const string ButtonLabel = "도감";

    /// <summary>모양을 맞추려고 복제할 기존 버튼. 설정창 안에 있어야 한다.</summary>
    private const string TemplateButtonName = "MainMenuButton";

    /// <summary>
    /// 설정창(450x250) 안에서 비어 있는 왼쪽 가운데.
    /// 볼륨바·토글은 x=42.5의 오른쪽 열에, MainMenuButton은 아래(y=-80)에 있어 겹치지 않는다.
    /// 위치가 마음에 안 들면 이 값만 고치고 다시 실행하면 된다.
    /// </summary>
    private static readonly Vector2 ButtonPosition = new Vector2(-140f, -5f);

    [MenuItem("Tools/Codex/Add Codex Button To Settings")]
    public static void Build()
    {
        int done = 0;
        if (BuildInPrefab()) done++;
        if (BuildInStartScene()) done++;

        if (done > 0) AssetDatabase.SaveAssets();
        Debug.Log($"[Codex] 도감 버튼 설치 완료: {done}/2곳. " +
                  "위치가 어색하면 인스펙터에서 CodexButton의 Anchored Position만 옮기면 된다.");
    }

    // ── 게임 씬(Canvas.prefab) ────────────────────────────────────────────────

    private static bool BuildInPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[Codex] 프리팹을 열지 못했습니다: {CanvasPrefabPath}");
            return false;
        }

        try
        {
            if (!AddButton(root, CanvasPrefabPath)) return false;
            PrefabUtility.SaveAsPrefabAsset(root, CanvasPrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── 시작 화면(StartScene) ─────────────────────────────────────────────────

    private static bool BuildInStartScene()
    {
        // 씬을 열면 저장 안 한 변경은 사라진다. 먼저 물어본다.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[Codex] 씬 저장을 취소해서 StartScene 작업을 건너뜁니다.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[Codex] 씬을 열지 못했습니다: {StartScenePath}");
            return false;
        }

        bool ok = false;
        foreach (var go in scene.GetRootGameObjects())
        {
            if (AddButton(go, StartScenePath)) { ok = true; break; }
        }

        if (!ok) return false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    // ── 공통 ──────────────────────────────────────────────────────────────────

    /// <summary>주어진 루트 아래에서 설정창을 찾아 도감 버튼을 만들거나 연결을 갱신한다.</summary>
    private static bool AddButton(GameObject root, string where)
    {
        var setting = root.GetComponentInChildren<SettingUI>(true);
        if (setting == null) return false; // 이 루트엔 설정창이 없다

        var so = new SerializedObject(setting);
        var panel = so.FindProperty("SettingPanel").objectReferenceValue as GameObject;
        if (panel == null)
        {
            Debug.LogError($"[Codex] {where}: SettingUI의 SettingPanel이 비어 있습니다.");
            return false;
        }

        Transform template = panel.transform.Find(TemplateButtonName);
        if (template == null)
        {
            Debug.LogError($"[Codex] {where}: 복제할 '{TemplateButtonName}'을 설정창에서 찾지 못했습니다.");
            return false;
        }

        Transform existing = panel.transform.Find(ButtonName);
        GameObject button;

        if (existing != null)
        {
            button = existing.gameObject;
        }
        else
        {
            button = Object.Instantiate(template.gameObject, panel.transform);
            button.name = ButtonName;

            var rt = button.GetComponent<RectTransform>();
            rt.anchoredPosition = ButtonPosition;
        }

        button.SetActive(true);

        // 라벨
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = ButtonLabel;
        else Debug.LogWarning($"[Codex] {where}: 버튼에 TMP 텍스트가 없어 라벨을 못 넣었습니다.");

        // onClick — 복제본이 원본(메인으로 나가기 등)의 호출을 물고 오므로 전부 비우고 다시 건다.
        var btn = button.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogError($"[Codex] {where}: 복제한 오브젝트에 Button이 없습니다.");
            return false;
        }

        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(btn.onClick, i);

        // 다른 버튼들과 같은 조합: 동작 + 클릭음
        UnityEventTools.AddPersistentListener(btn.onClick, setting.OpenCodex);
        UnityEventTools.AddPersistentListener(btn.onClick, setting.PlayButtonClickSound);

        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(setting);

        Debug.Log($"[Codex] {where}: '{ButtonName}'을 설정창에 넣고 OpenCodex에 연결했습니다.");
        return true;
    }
}
