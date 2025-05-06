using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grid : MonoBehaviour
{
    List<Plant> plants = new List<Plant>();
    private int maxPlantNum = 16;

    [SerializeField] private GameObject peaPrefab;

    // Start is called before the first frame update
    void Start()
    {
        InitGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitGrid()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = Instantiate(peaPrefab);
            Pea pea = obj.GetComponent<Pea>();
            List<GeneticTrait> basicTrait = new List<GeneticTrait>
            {
                new GeneticTrait(CompleteTraitType.NaturalDeath, 0.7f, 1)
            };
            pea.Init(basicTrait);
            plants.Add(pea);
        }
    }

    public IEnumerator Breeding()
    {
        //40초 동안 아래 과정 반복 진행 가능

        //교배할 부모 완두콩 두 개 선택

        //자식 완두콩 형질 계산 후 Instantiate

        //Grid 리로드

        yield return null;
    }    

    private void ShowPlantsOnGrid()
    {

    }
}
