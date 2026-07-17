using UnityEngine;
using UnityEngine.UI;

public class ChiliPepper : Plant
{
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "고추";
        base.Init(gridIndex, grid);
        plantID = 3;
    }
    public override float GetResistanceValue(int order)
    {
        return 1f;
    }

    public override void ResistWave(WaveType waveType)
    {
        // 특수(채식주의자): 웨이브를 버틸 때마다 가치 증가
        if (SpecialItemSystem.Has("vegetarian")) resistWaveCount++;
        return;
    }
    public override int GetSellingPrice()
    {
        // 특수(채식주의자): 판매가 500 통일 + 다른 식물과 동일한 가격 상승 공식 적용
        if (SpecialItemSystem.Has("vegetarian") && grid != null)
            return CalculateSellingPrice(500, grid.GetAdditionalPlantGoldMultiplier());
        return 500;
    }

    protected void OnMouseEnter()
    {
        FindAnyObjectByType<PriceSignController>().HideTaste();
        FindAnyObjectByType<PriceSignController>().SetPrice(GetSellingPrice());
        priceSign.gameObject.SetActive(true);
        priceSign.SetPrice(GetSellingPrice());
    }

    protected void OnMouseExit()
    {
        FindAnyObjectByType<PriceSignController>().HidePrice();
        if (!grid.showingAllPrice)
            priceSign.gameObject.SetActive(false);
    }

}
