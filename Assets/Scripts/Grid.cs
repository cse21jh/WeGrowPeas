using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Grid : MonoBehaviour
{
    List<Plant> plants = new List<Plant>();
    public int maxCol = 4;
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();

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
            if (i == 0)
                basicTrait.Add(new GeneticTrait(CompleteTraitType.WindResistance, 0.7f, 1));
            pea.Init(basicTrait);
            //plants.Add(pea);
            AddPlantToGrid(pea);
        }
    }

    public IEnumerator Breeding()
    {
        //40초 동안 아래 과정 반복 진행 가능

        //교배할 부모 완두콩 두 개 선택
        GameObject obj1 = null;
        GameObject obj2 = null;

        Debug.Log("40초 시작.");
        float startTime = Time.time;
        float endTime = startTime + 40.0f;

        while (Time.time < endTime)
        {
            if (Input.GetMouseButtonDown(0)) // 완두콩 선택 과정. 취소는 이미 눌렀던 완두콩 클릭하면 취소. 
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // 현재는 콜라이더 component 있어야 확인 가능. 추후 grid 좌표 계산 되면 수정 예정.
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    GameObject clickedObject = hit.collider.gameObject;
                    Plant clickedPea = clickedObject.GetComponent<Plant>();

                    if (clickedPea != null)
                    {
                        if(obj1 == clickedObject)
                        {
                            Debug.Log("부모 1 선택 취소");
                            obj1 = null;
                        }
                        else if(obj2 == clickedObject)
                        {
                            Debug.Log("부모 2 선택 취소");
                            obj2 = null;
                        }
                        else if(obj1 == null)
                        {
                            Debug.Log("부모 1 선택");
                            obj1 = clickedObject;
                        }
                        else if(obj2 == null)
                        {
                            Debug.Log("부모 2 선택");
                            obj2 = clickedObject;
                        }
                        else
                        {
                            Debug.Log("이미 두 부모가 모두 선택된 상태");
                        }
                    }
                    else
                    {
                        Debug.Log("콩을 선택해주세요");
                    }
                }
            }


            if (Input.GetKeyDown(KeyCode.B))
            {
                if (obj1 != null && obj2 != null) // 교배 버튼 등으로 추후 수정
                {
                    Plant parent1 = obj1.GetComponent<Plant>();
                    Plant parent2 = obj2.GetComponent<Plant>();
                    //자식 완두콩 형질 계산 후 Instantiate
                    List<GeneticTrait> childTrait = Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait());
                    
                    bool canBreed = false;
                    for (int idx = 0; idx < maxCol * 4; idx++)
                    {
                        if (!plantGrid.ContainsKey(idx))
                        {
                            canBreed = true;
                        }
                    }
                    if (canBreed)
                    {
                        GameObject childObj = Instantiate(peaPrefab);
                        Plant child = childObj.GetComponent<Plant>();
                        if (child != null) 
                        {
                            child.Init(childTrait);
                            //plants.Add(child);
                            AddPlantToGrid(child);
                            Debug.Log("자식 생성 성공");
                            obj1 = null;
                            obj2 = null;
                            Debug.Log("부모 선택 초기화");
                        }
                        else
                        {
                            Debug.Log("자식 생성에 오류 발생");
                            Destroy(childObj);
                        }
                            
                    }
                    else
                    {
                        Debug.Log("키울 공간이 부족합니다");
                    }
                    
                }
                else
                {
                    Debug.Log("아직 두 콩을 모두 선택하지 않았습니다");
                }
            }

            yield return null;
        }


        Debug.Log("교배 페이즈 종료");
        //Grid 리로드

        yield return null;
    }    

    private List<GeneticTrait> Breed(List<GeneticTrait> parent1, List<GeneticTrait> parent2)
    {
        List<GeneticTrait> childTrait = new List<GeneticTrait>();

        foreach (CompleteTraitType trait in System.Enum.GetValues(typeof(CompleteTraitType)))
        {
            if (trait == CompleteTraitType.None)
                break;

            int p1Trait;
            int p2Trait;
            
            int childGenetic = 0;

            int traitNotInParent = 0;

            if(parent1.Any(t=>t.traitType == trait))
            {
                p1Trait = parent1.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p1Trait = 2;
                traitNotInParent += 1;
            }

            if (parent2.Any(t => t.traitType == trait))
            {
                p2Trait = parent2.First(t => t.traitType == trait).genetics;
            }
            else
            {
                p2Trait = 2;
                traitNotInParent += 1;
            }

            if (traitNotInParent == 2)
                continue;

            switch (p1Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (Random.Range(0, 2)); break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (Random.Range(0, 2)); break;
                default: break;
            }

            float resistance = 0f;
            switch (childGenetic)
            {
                case 0: resistance = 0.9f; break;
                default: resistance = 0.7f; break;
            }
            childTrait.Add(new GeneticTrait(trait, resistance, childGenetic));
        }


        return childTrait;  
    }

    private void AddPlantToGrid(Plant plant)
    {
        for(int idx = 0; idx < maxCol*4; idx++)
        {
            if(!plantGrid.ContainsKey(idx))
            {
                plantGrid[idx] = plant;

                Transform soilTransform = GetSoilTransform(idx);
                plant.transform.position = soilTransform.position;

                return;
            }
        }
    }

    private Transform GetSoilTransform(int idx)
    {
        int row = idx / maxCol;
        int col = idx % maxCol;

        Transform rowT = transform.GetChild(row);
        Transform colT = rowT.GetChild(col);

        return colT;
    }

    public void DestroyPlant(int gridNum)
    {
        Plant plant = plantGrid[gridNum];
        plant.Die();
        plantGrid.Remove(gridNum);
        return;
    }
}

