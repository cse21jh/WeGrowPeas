using UnityEngine;

/// <summary>마우스가 올라간 밭 칸의 강조 표시를 맡는다.</summary>
public class SoilHighlighter
{
    private readonly Grid grid;
    private readonly Material soilHighlightMaterial;
    private int hoveredSoilIndex = -1;

    public SoilHighlighter(Grid grid, Material soilHighlightMaterial)
    {
        this.grid = grid;
        this.soilHighlightMaterial = soilHighlightMaterial;
    }

    public void HighlightHoveredTile(int? targetIndex, Material customMaterial = null, UnityEngine.Color? customOutlineColor = null)
    {
        // 1. Un-highlight previous
        if (hoveredSoilIndex != -1 && (!targetIndex.HasValue || hoveredSoilIndex != targetIndex.Value))
        {
            Transform oldSoilT = grid.GetSoilTransform(hoveredSoilIndex);
            if (oldSoilT != null)
            {
                Soil oldSoil = oldSoilT.GetComponent<Soil>();
                if (oldSoil != null) oldSoil.SetHighlight(false);
            }
            hoveredSoilIndex = -1;
        }

        // 2. Highlight new
        if (targetIndex.HasValue)
        {
            int idx = targetIndex.Value;
            if (hoveredSoilIndex != idx)
            {
                Transform newSoilT = grid.GetSoilTransform(idx);
                if (newSoilT != null)
                {
                    Soil newSoil = newSoilT.GetComponent<Soil>();
                    if (newSoil != null)
                    {
                        Material matToUse = customMaterial != null ? customMaterial : soilHighlightMaterial;
                        newSoil.SetHighlight(true, matToUse, customOutlineColor);
                    }
                }
                hoveredSoilIndex = idx;
            }
        }
    }
    public void ClearHoverHighlight()
    {
        if (hoveredSoilIndex != -1)
        {
            Transform oldSoilT = grid.GetSoilTransform(hoveredSoilIndex);
            if (oldSoilT != null)
            {
                Soil oldSoil = oldSoilT.GetComponent<Soil>();
                if (oldSoil != null) oldSoil.SetHighlight(false);
            }
            hoveredSoilIndex = -1;
        }
    }
}
