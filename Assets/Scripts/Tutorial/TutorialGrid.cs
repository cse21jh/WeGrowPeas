using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class TutorialGrid : Grid
{
    //public Dictionary<int, Plant> Board.Plants = new Dictionary<int, Plant>();

    [SerializeField] private GameObject tutorialBug;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
    }

    public void InitTGrid()
    {
        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(TraitType.NaturalDeath, 0.45f, 1, 0.0f),
        new GeneticTrait(TraitType.Wind, 0.43f, 0, 0.0f)
        });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(TraitType.NaturalDeath, 0.37f, 1, 0.0f),
        new GeneticTrait(TraitType.Wind, 0.47f, 1, 0.0f)
        });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(TraitType.NaturalDeath, 0.5f, 0, 0.0f),
        new GeneticTrait(TraitType.Wind, 0.38f, 1, 0.0f)
        });

        SpawnTPea(new List<GeneticTrait> {
        new GeneticTrait(TraitType.NaturalDeath, 0.5f, 0, 0.0f),
        new GeneticTrait(TraitType.Wind, 0.75f, 2, 0.0f)
        });
    }

    private void SpawnTPea(List<GeneticTrait> traits)
    {
        var p = Instantiate(peaPrefab);

        var pea = p.GetComponent<Pea>();
        pea.SetTrait(traits);
        pea.SetMovable(false);

        AddPlantToGrid(pea);
    }

    public void SpawnTutorialBug()
    {
        StartCoroutine(TutorialBugSpawnRoutine());
    }

    private IEnumerator TutorialBugSpawnRoutine()
    {
        TutorialBug b = Instantiate(tutorialBug).GetComponent<TutorialBug>();

        //isBreeding = true;

        yield return new WaitForSeconds(1.75f);

        b.StopMoving();
    }

    public override bool CheckGameOver()
    {
        return false;
    }
    public void MakeMovable()
    {
        Pea movablePea = Board.Plants[0].GetComponent<Pea>();        

        bool targetValue = !movablePea.IsMovable;

        movablePea.SetMovable(targetValue);
    }

    public void MakeAllUnmovable()
    {
        foreach(var peas in Board.Plants)
        {
            if(peas.Value is Pea p)
            {
                p.SetMovable(false);
            }
        }
    }

}
