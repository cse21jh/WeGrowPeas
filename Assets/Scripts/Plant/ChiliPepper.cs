using UnityEngine;
using UnityEngine.UI;

public class ChiliPepper : Plant
{
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "∞Ì√ﬂ";
        base.Init(gridIndex, grid);
        plantID = 3;
    }
    public override float GetResistanceValue(int order)
    {
        return 1f;
    }
    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        return 1f;
    }

    public override void ResistWave(WaveType waveType)
    {
        return;
    }
    public override int GetSellingPrice()
    {
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
        priceSign.gameObject.SetActive(false);
    }

}
