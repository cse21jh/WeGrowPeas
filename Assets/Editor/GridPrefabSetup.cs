using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 밭 정보 팝업이 쓰는 행 프리팹에 컴포넌트를 붙이고 필드를 연결한다.
///
///   GridPrefab.prefab       → <see cref="GridCellSlot"/>
///   GridDetailPrefab.prefab → <see cref="GridDetailRow"/>
///
/// 연결은 지금 쓰이는 자식 이름을 기준으로 한 번만 찾아 넣는다.
/// 그 뒤로는 인스펙터 참조로 동작하므로 이름을 바꿔도 안전하다.
/// 이미 채워진 필드는 건드리지 않으니 여러 번 실행해도 된다.
/// </summary>
public static class GridPrefabSetup
{
    private const string CellPrefabPath = "Assets/Resource/Prefabs/UI/PhonePopup/GridPrefab.prefab";
    private const string DetailPrefabPath = "Assets/Resource/Prefabs/UI/PhonePopup/GridDetailPrefab.prefab";

    // 아이콘은 겹쳐 놓은 순서대로. 앞쪽이 위에 보이는 것.
    private static readonly string[] IconNames = { "Icon_1", "Icon_2", "Icon_2 (1)" };

    [MenuItem("Tools/Grid/Setup Grid Prefabs")]
    public static void Setup()
    {
        SetupCellPrefab();
        SetupDetailPrefab();
        AssetDatabase.SaveAssets();
    }

    // ── GridPrefab ────────────────────────────────────────────────────────────

    private static void SetupCellPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CellPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[Grid] 프리팹을 열지 못했습니다: {CellPrefabPath}");
            return;
        }

        try
        {
            var slot = root.GetComponent<GridCellSlot>();
            if (slot == null) slot = root.AddComponent<GridCellSlot>();

            var so = new SerializedObject(slot);

            // 아이콘 배열: 비어 있을 때만 이름으로 찾아 채운다.
            var iconsProp = so.FindProperty("icons");
            if (iconsProp.arraySize == 0)
            {
                var found = new List<Image>();
                foreach (string name in IconNames)
                {
                    var image = FindComponent<Image>(root.transform, name);
                    if (image != null) found.Add(image);
                }

                iconsProp.arraySize = found.Count;
                for (int i = 0; i < found.Count; i++)
                    iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = found[i];

                Debug.Log($"[Grid] 아이콘 {found.Count}칸 연결");
            }

            AssignIfEmpty(so, "amountText", FindComponent<TMP_Text>(root.transform, "AmountText"));
            AssignIfEmpty(so, "amountUnderlayText", FindComponent<TMP_Text>(root.transform, "AmountText_Underlay"));
            AssignIfEmpty(so, "selectedFrame", FindObject(root.transform, "SelectedFrame"));
            AssignIfEmpty(so, "button", root.GetComponent<Button>());

            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
            Debug.Log($"[Grid] GridCellSlot 설정 완료: {CellPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── GridDetailPrefab ──────────────────────────────────────────────────────

    private static void SetupDetailPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(DetailPrefabPath);
        if (root == null)
        {
            Debug.LogError($"[Grid] 프리팹을 열지 못했습니다: {DetailPrefabPath}");
            return;
        }

        try
        {
            var row = root.GetComponent<GridDetailRow>();
            if (row == null) row = root.AddComponent<GridDetailRow>();

            var so = new SerializedObject(row);

            AssignIfEmpty(so, "icon", FindComponent<Image>(root.transform, "GridDetailImage"));
            AssignIfEmpty(so, "label", FindComponent<TMP_Text>(root.transform, "GridDetailText"));

            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, DetailPrefabPath);
            Debug.Log($"[Grid] GridDetailRow 설정 완료: {DetailPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>이미 연결돼 있으면 손대지 않는다(직접 바꿔 둔 것을 덮어쓰지 않도록).</summary>
    private static void AssignIfEmpty(SerializedObject so, string propertyName, Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[Grid] {propertyName} 필드를 찾지 못했습니다.");
            return;
        }

        if (prop.objectReferenceValue != null) return;

        if (value == null)
        {
            Debug.LogWarning($"[Grid] {propertyName}에 넣을 오브젝트를 찾지 못했습니다. 직접 연결하세요.");
            return;
        }

        prop.objectReferenceValue = value;
    }

    private static T FindComponent<T>(Transform root, string childName) where T : Component
    {
        Transform t = FindDeep(root, childName);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static GameObject FindObject(Transform root, string childName)
    {
        Transform t = FindDeep(root, childName);
        return t != null ? t.gameObject : null;
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }
}
