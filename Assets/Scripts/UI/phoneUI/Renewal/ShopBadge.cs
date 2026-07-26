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
        if (typeText != null) typeText.text = isFixed ? "고정 상품" : "로테이션 상품";

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
        if (item == null) return null;
        // 현재 ItemData에 별도 태그 필드가 없어 등급 태그만 노출한다.
        // 태그 데이터가 생기면 여기서 반환 목록을 확장하면 슬롯 UI가 자동 반영된다.
        string grade = item.GradeTagText;
        return string.IsNullOrEmpty(grade) ? null : new[] { grade };
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
