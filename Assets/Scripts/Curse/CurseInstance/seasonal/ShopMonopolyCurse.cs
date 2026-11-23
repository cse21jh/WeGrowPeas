using UnityEngine;

// 205 독점시장: 상점 품목 가격이 무작위(하한~상한 배율)로 결정. ItemData가 CurseState를 읽어 반영.
public class ShopMonopolyCurse : CurseInstance
{
    public ShopMonopolyCurse(CurseScriptable data, int level, CurseBoard board) : base(data, level, board)
    {

    }

    public override void Activate()
    {
        GameRun.Curse.ShopMonopoly = true;
        GameRun.Curse.ShopPriceMinMul = (Lv != null ? Lv.valueA : 100f) / 100f;
        GameRun.Curse.ShopPriceMaxMul = (Lv != null ? Lv.valueB : 100f) / 100f;
    }

    public override void Deactivate()
    {
        GameRun.Curse.ShopMonopoly = false;
        GameRun.Curse.ShopPriceMinMul = 1f;
        GameRun.Curse.ShopPriceMaxMul = 1f;
    }
}
