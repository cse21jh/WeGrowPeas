using UnityEngine;
using UnityEngine.UI;

public class ChiliPepper : Plant
{
    public override PlantKind Kind => PlantKind.ChiliPepper;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = PlantIds.ChiliPepper;
        base.Init(gridIndex, grid);
        plantID = 3;
    }
    public override void ResistWave(WaveType waveType)
    {
        int previousSellingPrice = GetSellingPrice();
        State.ResistWave(waveType); // 특수(채식주의자)일 때만 가치가 오른다
        RefreshSellingPriceAndNotify(previousSellingPrice, PlantValueChangeReason.WaveSurvived);
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
        if (!grid.Upgrades.showingAllPrice)
            priceSign.gameObject.SetActive(false);
    }

}
