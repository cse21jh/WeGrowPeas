using UnityEngine;

// 205 독점시장: 상점 품목 가격이 무작위(하한~상한 배율)로 결정. ShopUI/ItemData가 CurseState를 읽어 반영.
public class ShopMonopolyCurse : CurseInstance
{
    public ShopMonopolyCurse(CurseScriptable data, int level) : base(data, level)
    {

    }

    public override void Activate()
    {
        CurseState.ShopMonopoly = true;
        CurseState.ShopPriceMinMul = (Lv != null ? Lv.valueA : 100f) / 100f;
        CurseState.ShopPriceMaxMul = (Lv != null ? Lv.valueB : 100f) / 100f;
    }

    public override void Deactivate()
    {
        CurseState.ShopMonopoly = false;
        CurseState.ShopPriceMinMul = 1f;
        CurseState.ShopPriceMaxMul = 1f;
    }
}
