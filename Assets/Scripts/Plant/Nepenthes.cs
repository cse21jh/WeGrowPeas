using DG.DemiLib;
using UnityEngine;
using UnityEngine.UI;


public class Nepenthes : Plant
{
    [SerializeField] private GameObject NepenthesPheromone;
    [SerializeField] private float pheromoneSize = 4f;
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "네펜데스";
        base.Init(gridIndex, grid);
        plantID = 2;
        UpdatePheromone();
        UpdatePheromoneSize();
    }
    public override float GetResistanceValue(int order)
    {
        return 1f;
    }


    public override void ResistWave(WaveType waveType)
    {
        return;
    }
    public override int GetSellingPrice()
    {
        return 0;
    }

    public void UpdatePheromone()
    {
        if (NepenthesPheromone != null && grid != null)
        {
            NepenthesPheromone.SetActive(grid.HasNepenthesPheromone);
        }
    }

    public void UpdatePheromoneSize()
    {
        if (NepenthesPheromone != null && grid != null)
        {
            float multiplier = grid.GetEffectiveNepenthesPheromoneSizeMultiplier();
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
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, pheromoneSize/2);
    }

}
