using TMPro;
using UnityEngine;

/// <summary>
/// 상점 배지(고정 상품 / 등급 / 품목 제한) 표시 규칙.
/// 아이템 슬롯(ItemController)과 상세 패널(ShopCanvasController)이 같은 문구를 쓰도록 한 곳에 모았다.
/// </summary>
public static class ShopBadge
{
    /// <summary>배지 3종을 한 번에 채운다. 오브젝트가 null이면 건너뛴다.</summary>
    public static void Apply(
        ItemData item, bool isFixed,
        GameObject typeObj, TMP_Text typeText,
        GameObject gradeObj, TMP_Text gradeText,
        GameObject limitObj, TMP_Text limitText)
    {
        if (item == null) return;

        // 고정 상품 / 로테이션 상품
        if (typeObj != null) typeObj.SetActive(true);
        if (typeText != null) typeText.text = isFixed ? "고정" : "로테이션";

        // 등급 (GradeTagText가 있으면 우선, 없으면 Rarity로 S/A/B/C)
        if (gradeObj != null) gradeObj.SetActive(true);
        if (gradeText != null) gradeText.text = $"{GetGrade(item)}등급";

        // 품목 제한: 전체 구매 제한이 있는 아이템만 표시
        bool hasLimit = item.MaxPurchaseCount >= 0;
        if (limitObj != null) limitObj.SetActive(hasLimit);
        if (limitText != null && hasLimit) limitText.text = "품목 제한";
    }

    /// <summary>남은 구매 횟수 문구. 무제한이면 "재고 제한 없음".</summary>
    public static string GetStockText(ItemData item)
    {
        if (item == null) return "";
        if (item.MaxPurchaseCount < 0) return "재고 제한 없음";

        int remain = Mathf.Max(0, item.MaxPurchaseCount - item.GetTotalPurchaseCount());
        return $"남은 수량 {remain}";
    }

    /// <summary>슬롯 하단에 표시할 태그 목록. (등급 배지와 별개)</summary>
    public static string[] GetTags(ItemData item)
    {
        if (item == null || item.Tags == null || item.Tags.Count == 0) return null;

        var names = new string[item.Tags.Count];
        for (int i = 0; i < item.Tags.Count; i++)
            names[i] = item.Tags[i].ToDisplayName();

        return names;
    }

    /// <summary>
    /// 태그 칸을 있는 만큼만 켠다. 칸 수보다 태그가 많으면 앞에서부터 채운다.
    /// 슬롯(ItemController)과 상세 패널(ShopCanvasController)이 같은 규칙을 쓰도록 한 곳에 둔다.
    ///
    /// 마우스를 올리면 그 태그의 설명이 <see cref="HoverTooltip"/>으로 뜬다.
    /// 호버 처리는 여기서 붙이므로 씬에는 태그 칸만 연결해 두면 된다.
    /// </summary>
    public static void ApplyTags(string[] tags, GameObject[] tagObjects, TMP_Text[] tagTexts)
    {
        if (tagObjects == null) return;

        for (int i = 0; i < tagObjects.Length; i++)
        {
            bool show = tags != null && i < tags.Length && !string.IsNullOrEmpty(tags[i]);

            if (tagObjects[i] != null) tagObjects[i].SetActive(show);

            if (!show) continue;

            if (tagTexts != null && i < tagTexts.Length && tagTexts[i] != null)
                tagTexts[i].text = tags[i];

            if (tagObjects[i] != null) SetupTagHover(tagObjects[i], tags[i]);
        }
    }

    /// <summary>태그 칸에 호버 설명을 붙인다. 이미 붙어 있으면 내용만 갈아끼운다.</summary>
    private static void SetupTagHover(GameObject tagObject, string displayName)
    {
        var hover = tagObject.GetComponent<UIHoverHandler>();
        if (hover == null) hover = tagObject.AddComponent<UIHoverHandler>();

        string description = ItemTagExtensions.TryParseDisplayName(displayName, out var tag)
            ? tag.ToDescription()
            : "";

        // 설명이 없으면 굳이 띄우지 않는다.
        if (string.IsNullOrEmpty(description))
        {
            hover.Setup(null, null);
            return;
        }

        string content = $"{displayName}\n{description}";
        hover.Setup(() => HoverTooltip.ShowFor(content), HoverTooltip.HideCurrent);
    }

    public static string GetGrade(ItemData item)
    {
        if (item == null) return "";
        if (!string.IsNullOrEmpty(item.GradeTagText)) return item.GradeTagText;

        return item.Rarity switch
        {
            ItemRarity.Legendary => "S",
            ItemRarity.Special => "A",
            ItemRarity.Rare => "B",
            ItemRarity.Common => "C",
            _ => ""
        };
    }
}
