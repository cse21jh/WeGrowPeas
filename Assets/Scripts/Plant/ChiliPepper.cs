using UnityEngine;
using UnityEngine.UI;

public class ChiliPepper : Plant
{
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "∞Ì√ﬂ";
        base.Init(gridIndex, grid);
    }
    public override float GetResistanceValue(int order)
    {
        return 1f;
    }
    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        return 1f;
    }

    public override int GetSellingPrice()
    {
        return 500;
    }

}
