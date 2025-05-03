using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pea : Plant
{
    
    public override void Initialize(int gridNumber, Plant parent1, Plant parent2)
    {
        base.Initialize(gridNumber, parent1, parent2);
    }

    public override void InitializeCompleteTrait(Dictionary<CompleteTraitType, float> parent1, Dictionary<CompleteTraitType, float> parent2)
    {
        base.InitializeCompleteTrait(parent1, parent2);
    }

    public override void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        base.InitializeIncompleteTrait(parent1, parent2);
    }




    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
