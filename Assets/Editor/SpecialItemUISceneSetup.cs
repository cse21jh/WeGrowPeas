using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 특수 아이템 UI 골격(선물 버튼 + 전체화면 3택 패널)을 씬에 생성하고
/// <see cref="SpecialItemUIController"/>에 연결. 스타일은 이후 인스펙터에서 보완.
/// </summary>
public static class SpecialItemUISceneSetup
{
    [MenuItem("Tools/SpecialItem/Add Special Item UI To Scene")]
    public static void Build()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // 재생성 시 기존 것 제거 (씬 오브젝트만)
        foreach (var c in Resources.FindObjectsOfTypeAll<SpecialItemUIController>())
            if (c != null && c.gameObject.scene.IsValid())
                Object.DestroyImmediate(c.gameObject);

        // ── 항상 켜진 루트(컨트롤러 Update가 선물 버튼 토글) ──
        var root = NewUI("SpecialItemRoot", canvas.transform);
        Stretch(root);
        root.GetComponent<RectTransform>().SetAsLastSibling();
        var controller = root.AddComponent<SpecialItemUIController>();

        // ── 선물 버튼 (우하단, 미수령 선물 있을 때만 표시) ──
        var giftRoot = NewUI("GiftButton", root.transform);
        Anchor(giftRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-170, 90), new Vector2(-20, 170));
        var giftImg = AddImage(giftRoot, new Color(1f, 0.85f, 0.3f, 0.95f));
        var giftBtn = giftRoot.AddComponent<Button>();
        giftBtn.targetGraphic = giftImg;
        var giftLabel = NewText("Label", giftRoot.transform, "🎁 선물", 26, TextAlignmentOptions.Center);
        Stretch(giftLabel);
        var giftCount = NewText("Count", giftRoot.transform, "", 20, TextAlignmentOptions.TopRight);
        Anchor(giftCount, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -30), new Vector2(-4, -2));

        // ── 3택 패널 (전체화면) ──
        var panel = NewUI("ChoicePanel", root.transform);
        Stretch(panel);
        AddImage(panel, new Color(0.03f, 0.03f, 0.07f, 0.95f));

        var title = NewText("Title", panel.transform, "완두콩의 선물", 42, TextAlignmentOptions.Center);
        Anchor(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(0, -30));

        var cardRow = NewUI("CardRow", panel.transform);
        Anchor(cardRow, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.78f), Vector2.zero, Vector2.zero);
        var hl = cardRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 30; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandHeight = true;

        var cards = new SpecialItemCard[3];

        for (int i = 0; i < 3; i++)
        {
            var card = NewUI($"Card{i}", cardRow.transform);
            var cardImg = AddImage(card, new Color(1f, 1f, 1f, 0.08f));
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = cardImg;

            var iconGo = NewUI("Icon", card.transform);
            Anchor(iconGo, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-60, -160), new Vector2(60, -40));
            var iconImg = AddImage(iconGo, Color.white);

            var nameGo = NewText("Name", card.transform, "", 30, TextAlignmentOptions.Center);
            Anchor(nameGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -230), new Vector2(-10, -170));

            var descGo = NewText("Desc", card.transform, "", 22, TextAlignmentOptions.Top);
            Anchor(descGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(15, 75), new Vector2(-15, -245));

            // 카드 하단 리롤 버튼
            var rerollGo = NewUI("RerollButton", card.transform);
            Anchor(rerollGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(15, 15), new Vector2(-15, 65));
            var rerollImg = AddImage(rerollGo, new Color(1f, 1f, 1f, 0.15f));
            var rerollBtn = rerollGo.AddComponent<Button>();
            rerollBtn.targetGraphic = rerollImg;
            var rerollLabel = NewText("Label", rerollGo.transform, "다시 뽑기", 20, TextAlignmentOptions.Center);
            Stretch(rerollLabel);

            // 카드 스크립트 연결 (표시/버튼은 카드가 스스로 관리)
            var cardScript = card.AddComponent<SpecialItemCard>();
            var cso = new SerializedObject(cardScript);
            cso.FindProperty("selectButton").objectReferenceValue = btn;
            cso.FindProperty("icon").objectReferenceValue = iconImg;
            cso.FindProperty("nameText").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
            cso.FindProperty("descText").objectReferenceValue = descGo.GetComponent<TMP_Text>();
            cso.FindProperty("rerollButton").objectReferenceValue = rerollBtn;
            cso.ApplyModifiedPropertiesWithoutUndo();

            cards[i] = cardScript;
        }

        var closeBtn = NewUI("CloseButton", panel.transform);
        Anchor(closeBtn, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-100, 30), new Vector2(100, 90));
        var closeImg = AddImage(closeBtn, new Color(1, 1, 1, 0.15f));
        var closeButton = closeBtn.AddComponent<Button>();
        closeButton.targetGraphic = closeImg;
        var closeLabel = NewText("Label", closeBtn.transform, "나중에", 24, TextAlignmentOptions.Center);
        Stretch(closeLabel);

        // ── 컨트롤러 연결 ──
        var so = new SerializedObject(controller);
        so.FindProperty("giftButtonRoot").objectReferenceValue = giftRoot;
        so.FindProperty("giftCountText").objectReferenceValue = giftCount.GetComponent<TMP_Text>();
        so.FindProperty("choicePanel").objectReferenceValue = panel;
        FillArray(so, "cards", cards);
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(giftBtn.onClick, controller.OpenChoicePanel);
        UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseChoicePanel);

        giftRoot.SetActive(false);
        panel.SetActive(false);
        Debug.Log("[SpecialItem] UI 골격 생성 완료. 선물이 오면 우하단 버튼이 표시됩니다.");
        Selection.activeGameObject = root;
    }

    private static void FillArray(SerializedObject so, string prop, Object[] values)
    {
        var arr = so.FindProperty(prop);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white; t.enableWordWrapping = true;
        return go;
    }

    private static Image AddImage(GameObject go, Color c)
    {
        var img = go.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Anchor(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
    }
}
