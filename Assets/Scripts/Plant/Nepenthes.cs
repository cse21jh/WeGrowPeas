using DG.DemiLib;
using UnityEngine;
using UnityEngine.UI;


public class Nepenthes : Plant
{
    [SerializeField] private GameObject NepenthesPheromone;
    [SerializeField] private float pheromoneSize = 4f;
    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "³×Ææµ¥½º";
        base.Init(gridIndex, grid);
        NepenthesPheromone.transform.localScale = new Vector3(pheromoneSize, pheromoneSize, 1f);
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
        return 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, pheromoneSize/2);
    }

}
