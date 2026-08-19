using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 태그 툴팁에 필요한 오브젝트를 프리팹에 만들어 붙인다.
///
/// 상점 UI는 씬이 아니라 프리팹에 들어 있어서 프리팹을 직접 열어 고친다.
///   ShopCanvas.prefab  — 툴팁 + 상세 패널 태그 칸
///   ItemPrefab.prefab  — 슬롯 태그 칸의 레이캐스트 보정
///
/// 이미 만들어 둔 것이 있으면 다시 만들지 않고 연결만 맞춘다. 여러 번 실행해도 안전하다.
/// </summary>
public static class ShopTagTooltipSetup
{
    private const string ShopCanvasPath =
        "Assets/Resource/Sprites/UI/phoneUI/Renewal/MenuIcons/Canvases/ShopCanvas.prefab";
    private const string ItemPrefabPath =
        "Assets/Resource/Sprites/UI/phoneUI/Renewal/MenuIcons/Sprites/shop/ItemPrefab.prefab";

    private const string TooltipRootName = "TagTooltip";
    private const string TagChipPrefix = "Detail_ItemTag_";

    /// <summary>상세 패널에 만들 태그 칸 수. 지금 한 아이템의 최대 태그는 2개다.</summary>
    private const int DetailTagSlots = 3;

    // ── 툴팁 크기 ─────────────────────────────────────────────────────────────
    // 폰 화면 캔버스는 단위가 작다. 이 프리팹의 본문 텍스트가 5, 헤더가 8이고
    // 요소 폭이 50~200 정도라 거기에 맞춘다. 크기를 바꾸려면 여기만 고치면 된다.

    private const float TooltipFontSize = 5f;
    private const float TooltipWrapWidth = 90f;
    private static readonly Vector2 TooltipOffset = new Vector2(6f, -6f);

    /// <summary>
    /// 화면 아래에서 이만큼은 비워 둔다. 하단 메뉴바를 가리지 않게 하려는 것.
    /// 폰 화면이 348 단위 높이라 그 1/6쯤을 시작값으로 잡았다 — 실제로 보고 조절하면 된다.
    /// (인스펙터의 HoverTooltip > 가장자리 여백 > Inset Bottom)
    /// </summary>
    private const float TooltipBottomInset = 60f;

    [MenuItem("Tools/Shop/Add Tag Tooltip")]
    public static void Build()
    {
        BuildShopCanvas();
        FixItemPrefabTagRaycast();
        AssetDatabase.SaveAssets();
    }

    // ── ShopCanvas.prefab ─────────────────────────────────────────────────────

    private static void BuildShopCanvas()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ShopCanvasPath);
        if (root == null)
        {
            Debug.LogError($"[Shop] 프리팹을 열지 못했습니다: {ShopCanvasPath}");
            return;
        }

        try
        {
            var shop = root.GetComponentInChildren<ShopCanvasController>(true);
            if (shop == null)
            {
                Debug.LogError("[Shop] ShopCanvasController를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(shop);

            HoverTooltip tooltip = EnsureTooltip(root.transform);
            List<GameObject> chips = EnsureDetailTagChips(so);

            WireArray(so, "detail_ItemTags", chips.ToArray());
            WireArray(so, "detail_ItemTagTexts",
                chips.ConvertAll(c => (Object)c.GetComponentInChildren<TMP_Text>(true)).ToArray());

            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ShopCanvasPath);
            Debug.Log($"[Shop] 태그 툴팁 + 상세 태그 칸 {chips.Count}개를 ShopCanvas에 넣었습니다. " +
                      $"(툴팁: {tooltip.name})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>커서를 따라다니는 툴팁. 맨 마지막 자식이라 다른 UI 위에 그려진다.</summary>
    private static HoverTooltip EnsureTooltip(Transform parent)
    {
        Transform existing = parent.Find(TooltipRootName);
        GameObject rootGo = existing != null ? existing.gameObject : NewUI(TooltipRootName, parent);

        Stretch(rootGo);
        rootGo.transform.SetAsLastSibling();

        var tooltip = rootGo.GetComponent<HoverTooltip>();
        if (tooltip == null) tooltip = rootGo.AddComponent<HoverTooltip>();

        // 패널: pivot 좌상단 + 중앙 앵커여야 커서 오른쪽 아래로 펼쳐진다.
        Transform panelT = rootGo.transform.Find("Panel");
        GameObject panel = panelT != null ? panelT.gameObject : NewUI("Panel", rootGo.transform);

        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0f, 1f);

        var bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.04f, 0.92f);
        bg.raycastTarget = false; // 툴팁이 커서를 가로채면 깜빡인다

        // 폰 화면 캔버스는 단위가 작다(본문 5, 헤더 8, 요소 폭 50~200).
        // 아래 숫자들은 거기에 맞춘 값이다.
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(3, 3, 2, 2);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;

        var fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform textT = panel.transform.Find("Text");
        GameObject textGo = textT != null ? textT.gameObject : NewUI("Text", panel.transform);

        var text = textGo.GetComponent<TextMeshProUGUI>();
        if (text == null) text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = TooltipFontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = true;

        var le = textGo.GetComponent<LayoutElement>();
        if (le == null) le = textGo.AddComponent<LayoutElement>();
        le.preferredWidth = TooltipWrapWidth; // 이 폭을 넘으면 줄바꿈

        var tso = new SerializedObject(tooltip);
        tso.FindProperty("panel").objectReferenceValue = panelRt;
        tso.FindProperty("text").objectReferenceValue = text;
        tso.FindProperty("offset").vector2Value = TooltipOffset;
        tso.FindProperty("insetBottom").floatValue = TooltipBottomInset;
        tso.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return tooltip;
    }

    /// <summary>
    /// 상세 패널의 태그 칸. 이미 연결된 등급 배지를 본떠 만들어 모양을 맞춘다.
    /// </summary>
    private static List<GameObject> EnsureDetailTagChips(SerializedObject shopSo)
    {
        var chips = new List<GameObject>();

        var gradeObj = shopSo.FindProperty("detail_ItemGrade").objectReferenceValue as GameObject;
        if (gradeObj == null)
        {
            Debug.LogWarning("[Shop] detail_ItemGrade가 비어 있어 태그 칸을 만들지 못했습니다. " +
                             "등급 배지를 먼저 연결하세요.");
            return chips;
        }

        Transform parent = gradeObj.transform.parent;
        var gradeRt = gradeObj.GetComponent<RectTransform>();

        // 부모에 레이아웃 그룹이 있으면 자리는 알아서 잡힌다. 없으면 등급 배지 아래로 흘려 놓는다.
        bool autoLayout = parent.GetComponent<LayoutGroup>() != null;

        for (int i = 0; i < DetailTagSlots; i++)
        {
            string chipName = TagChipPrefix + i;

            Transform found = parent.Find(chipName);
            GameObject chip;

            if (found != null)
            {
                chip = found.gameObject;
            }
            else
            {
                chip = Object.Instantiate(gradeObj, parent);
                chip.name = chipName;

                if (!autoLayout)
                {
                    var rt = chip.GetComponent<RectTransform>();
                    rt.anchoredPosition = gradeRt.anchoredPosition
                                          + new Vector2(0f, -(gradeRt.rect.height + 4f) * (i + 1));
                }
            }

            // 호버를 받으려면 레이캐스트 대상이 필요하다.
            EnsureRaycastTarget(chip);

            chip.SetActive(false); // 실제 표시는 ShopBadge.ApplyTags가 결정한다
            chips.Add(chip);
        }

        return chips;
    }

    // ── ItemPrefab.prefab ─────────────────────────────────────────────────────

    /// <summary>슬롯의 태그 칸이 호버를 받을 수 있게 레이캐스트 대상을 보장한다.</summary>
    private static void FixItemPrefabTagRaycast()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ItemPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[Shop] 프리팹을 열지 못했습니다: {ItemPrefabPath}");
            return;
        }

        try
        {
            var item = root.GetComponentInChildren<ItemController>(true);
            if (item == null)
            {
                Debug.LogWarning("[Shop] ItemController를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(item);
            var tagsProp = so.FindProperty("itemTags");
            if (tagsProp == null || tagsProp.arraySize == 0)
            {
                Debug.LogWarning("[Shop] ItemPrefab의 itemTags가 비어 있습니다. 태그 칸을 먼저 연결하세요.");
                return;
            }

            int fixedCount = 0;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var go = tagsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (go != null && EnsureRaycastTarget(go)) fixedCount++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Debug.Log($"[Shop] 슬롯 태그 칸 {tagsProp.arraySize}개 확인 (레이캐스트 보정 {fixedCount}개).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    /// <summary>호버를 받으려면 레이캐스트를 받는 Graphic이 있어야 한다.</summary>
    /// <returns>무언가 고쳤으면 true.</returns>
    private static bool EnsureRaycastTarget(GameObject go)
    {
        var graphic = go.GetComponent<Graphic>();
        if (graphic == null)
        {
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f); // 보이지 않지만 호버는 잡히는 판
            img.raycastTarget = true;
            return true;
        }

        if (!graphic.raycastTarget)
        {
            graphic.raycastTarget = true;
            return true;
        }

        return false;
    }

    private static void WireArray(SerializedObject so, string propertyName, Object[] values)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[Shop] {propertyName} 필드를 찾지 못했습니다.");
            return;
        }

        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
