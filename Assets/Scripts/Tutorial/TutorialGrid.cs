using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialGrid : Grid
{
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitTGrid()
    {
        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 0, 0.0f)
    });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0.0f)
    });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 0, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 1, 0.0f)
    });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 0, 0.0f),
        new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 2, 0.0f)
    });
    }

    private void SpawnTPea(List<GeneticTrait> traits)
    {
        var p = Instantiate(peaPrefab);

        var pea = p.GetComponent<Pea>();
        pea.SetTrait(traits);

        AddPlantToGrid(pea);
    }
}
