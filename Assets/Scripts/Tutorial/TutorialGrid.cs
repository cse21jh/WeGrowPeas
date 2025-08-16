using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialGrid : Grid
{
    [HideInInspector] public int maxCol = 4;
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    private float breedTimer = 30.0f;

    private bool isBreeding = false;

    private GameObject breedObj1 = null;
    private GameObject breedObj2 = null;
    private bool isBreedButtonPressed = false;

    [SerializeField] private GameObject peaPrefab;
    [SerializeField] private GameObject[] disabledSoil; // 4개 이상의 열이 추가될 때 활성화되는 토양들
    [SerializeField] private List<GameObject> bugPrefabs;

    [SerializeField] private TimerUI breedTimerUI;
    [SerializeField] private GameObject breedButton;
    [SerializeField] private GameObject breedSkipButton;
    [SerializeField] private TextMeshProUGUI breedCountUI;

    [SerializeField] private Sprite[] gardenSprites; // 정원 배경 스프라이트들
    [SerializeField] private SpriteRenderer gardenRenderer; // 정원 배경 스프라이트 렌더러

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitGrid()
    {
        GameObject obj = Instantiate(peaPrefab);
        Pea pea = obj.GetComponent<Pea>();
        List<GeneticTrait> basicTrait = new List<GeneticTrait>
        {
            new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1, 0.0f),
            new GeneticTrait(CompleteTraitType.WindResistance, 0.5f, 0, 0.0f)
        };
        pea.SetTrait(basicTrait);
        AddPlantToGrid(pea);
    }

    private void AddPlantToGrid(Plant plant, int grididx = -1) // 이미 오브젝트로 만들어진 식물 그리드에 추가. grididx에 숫자 삽입 시 해당 위치에 식물 심어줌
    {
        if (grididx != -1)
        {
            if (!plantGrid.ContainsKey(grididx))
            {
                plant.Init(grididx, this);
                Plantplant(plant);

                return;
            }
        }

        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (!plantGrid.ContainsKey(idx))
            {
                plant.Init(idx, this);
                Plantplant(plant);

                return;
            }
        }

        Destroy(plant.gameObject);
        return;
    }

    private void Plantplant(Plant plant)
    {
        plantGrid[plant.gridIndex] = plant;

        Transform soilT = GetSoilTransform(plant.gridIndex);
        plant.transform.position = soilT.position;
    }
}
