using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Pea : Plant
{
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] selectedSprite;

    public override void Init(int gridIndex, Grid grid)
    {
        speciesname = "완두콩";
        base.Init(gridIndex, grid);
    }

    public override void SetTrait(List<GeneticTrait> newTraits)
    {
        traits = newTraits;


        StemController stem = GetComponentInChildren<StemController>();
        if (stem != null)
        {
            stem.SetTraits(newTraits);
        }
        else
        {
            Debug.LogWarning("StemController not found in Plant");
        }
    }

    public override List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }
    /*public override void Initialize(int gridNumber, Plant parent1, Plant parent2)
    {
        base.Initialize(gridNumber, parent1, parent2);
    }

    public override void InitializeCompleteTrait(Dictionary<CompleteTraitType, int> parent1, Dictionary<CompleteTraitType, int> parent2)
    {
        base.InitializeCompleteTrait(parent1, parent2);

        foreach (CompleteTraitType trait in Enum.GetValues(typeof(CompleteTraitType)))
        {
            if (trait == CompleteTraitType.None)
                break;
            if (completeGenetics[trait] == 0)
                completeResistances[trait] = 0.9f;
            else
                completeResistances[trait] = 0.5f;
        }

        // 저항력 계산 및 삽입 필요. 지금은 러프하게 열성 만족하면 0.9 저항력 가지도록
    }

    public override void InitializeIncompleteTrait(Dictionary<IncompleteTraitType, float> parent1, Dictionary<IncompleteTraitType, float> parent2)
    {
        base.InitializeIncompleteTrait(parent1, parent2);
        // 저항력 계산 및 삽입 필요
    }*/

    public override void Die()
    {
        base.Die();
    }

    public override void DieWithAnimation()
    {
        StartCoroutine(PeaDeathAnimation());
    }

    IEnumerator PeaDeathAnimation()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        for(int i = 0; i < deathFrames.Length; i++)
        {
            sr.sprite = deathFrames[i];
            yield return new WaitForSeconds(0.3f);
        }

        Die();
        //Destroy(gameObject);
    }

    public override void MakeSelectedSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[1];
    }

    public override void MakeDefaultSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = selectedSprite[0];
    }
}
