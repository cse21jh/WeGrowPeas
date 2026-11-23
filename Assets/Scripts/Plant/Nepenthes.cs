using DG.DemiLib;
using UnityEngine;
using UnityEngine.UI;


public class Nepenthes : Plant
{
    [SerializeField] private GameObject NepenthesPheromone;
    [SerializeField] private float pheromoneSize = 4f;
    public override PlantKind Kind => PlantKind.Nepenthes;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = PlantIds.Nepenthes;
        base.Init(gridIndex, grid);
        plantID = 2;
        UpdatePheromone();
        UpdatePheromoneSize();
    }
    public override void ResistWave(WaveType waveType)
    {
        int previousSellingPrice = GetSellingPrice();
        State.ResistWave(waveType); // 특수(채식주의자)일 때만 가치가 오른다
        RefreshSellingPriceAndNotify(previousSellingPrice, PlantValueChangeReason.WaveSurvived);
    }

    public void UpdatePheromone()
    {
        if (NepenthesPheromone != null && grid != null)
        {
            NepenthesPheromone.SetActive(grid.Modifiers.HasNepenthesPheromone);
        }
    }

    public void UpdatePheromoneSize()
    {
        if (NepenthesPheromone != null && grid != null)
        {
            float multiplier = grid.Modifiers.GetEffectiveNepenthesPheromoneSizeMultiplier();
            float finalSize = pheromoneSize * multiplier;
            NepenthesPheromone.transform.localScale = new Vector3(finalSize, finalSize, 1f);
        }
        else if (NepenthesPheromone != null)
        {
            NepenthesPheromone.transform.localScale = new Vector3(pheromoneSize, pheromoneSize, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, pheromoneSize/2);
    }

}
