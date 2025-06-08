using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    List<Plant> plants = new List<Plant>();
    private int maxCol = 4;
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    private Dictionary<CompleteTraitType, float> additionalResistance = new Dictionary<CompleteTraitType, float>();
    private int additionalInheritance = 0;
    private float breedTimer = 40.0f;
    private int maxBreedCount = 4;
    private int waveSkipCount = 0;

    private bool isBreedButtonPressed = false;


    [SerializeField] private GameObject peaPrefab;
    [SerializeField] private GameObject soilPrefab;

    [SerializeField] private TimerUI breedTimerUI;
    [SerializeField] private Button breedButton;


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
                new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1)
            };
            pea.Init(basicTrait);
            //plants.Add(pea);
            AddPlantToGrid(pea);
        }
    }

    public IEnumerator Breeding()
    {
        //breedTimer 만큼 동안 아래 과정 반복 진행 가능

        //교배할 부모 완두콩 두 개 선택
        GameObject obj1 = null;
        GameObject obj2 = null;

        int breedCount = 0;

        breedTimerUI.StartBreedingTimer();

        Debug.Log(breedTimer + "초 시작. 최대 교배 횟수는 " + maxBreedCount + "입니다");
        float startTime = Time.time;
        float endTime = startTime + breedTimer;

        while (Time.time < endTime && breedCount < maxBreedCount)
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
                        if (obj1 == clickedObject)
                        {
                            //Debug.Log("부모 1 선택 취소");
                            Plant p = obj1.GetComponent<Plant>();
                            p.MakeDefaultSprite();
                            obj1 = null;
                        }
                        else if (obj2 == clickedObject)
                        {
                            //Debug.Log("부모 2 선택 취소");
                            Plant p = obj2.GetComponent<Plant>();
                            p.MakeDefaultSprite();
                            obj2 = null;
                        }
                        else if (obj1 == null)
                        {
                            //Debug.Log("부모 1 선택");
                            obj1 = clickedObject;
                            Plant p = obj1.GetComponent<Plant>();
                            p.MakeSelectedSprite();
                        }
                        else if (obj2 == null)
                        {
                            //Debug.Log("부모 2 선택");
                            obj2 = clickedObject;
                            Plant p = obj2.GetComponent<Plant>();
                            p.MakeSelectedSprite();
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

            if (Input.GetKeyDown(KeyCode.S))
            {
                if (waveSkipCount > 0)
                {
                    GameManager.Instance.enemyController.SetNextWave();
                    waveSkipCount--;
                    Debug.Log("다음 웨이브를 스킵했습니다");
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
                    if (canBreed && breedCount < maxBreedCount)
                    {
                        GameObject childObj = Instantiate(peaPrefab);
                        Plant child = childObj.GetComponent<Plant>();
                        if (child != null)
                        {
                            child.Init(childTrait);
                            //plants.Add(child);
                            AddPlantToGrid(child);
                            breedCount++;
                            Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (maxBreedCount - breedCount) + "입니다");
                            Plant p1 = obj1.GetComponent<Plant>();
                            Plant p2 = obj2.GetComponent<Plant>();
                            p1.MakeDefaultSprite();
                            p2.MakeDefaultSprite();
                            obj1 = null;
                            obj2 = null;
                        }
                        else
                        {
                            Debug.Log("자식 생성에 오류 발생");
                            Destroy(childObj);
                        }

                    }
                    else if (breedCount >= maxBreedCount)
                    {
                        Debug.Log("최대 교배 횟수 초과");
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

        breedTimerUI.StopTimer();
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

            if (parent1.Any(t => t.traitType == trait))
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
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 0 : 1); break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 0 : 1); break;
                default: break;
            }

            float resistance = 0f;
            float additional = GetAdditionalResistance(trait);


            switch (childGenetic)
            {
                case 0: resistance = 0.8f + additional; break;
                default: resistance = 0.5f + additional; break;
            }
            childTrait.Add(new GeneticTrait(trait, resistance, childGenetic));
        }


        return childTrait;
    }

    private void AddPlantToGrid(Plant plant)
    {
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (!plantGrid.ContainsKey(idx))
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
        int row = idx / 4;
        int col = idx % 4;

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

    public bool CheckGameOver()
    {
        bool gameOver = true;
        for (int idx = 0; idx < maxCol * 4; idx++)
        {
            if (plantGrid.ContainsKey(idx))
            {
                gameOver = false;
            }
        }
        return gameOver;
    }

    public void AddBreedTimer(int time)
    {
        breedTimer += time;
        return;
    }

    public void AddMaxBreedCount(int count)
    {
        maxBreedCount += count;
        return;
    }

    public void AddPlant(List<GeneticTrait> trait)
    {
        GameObject obj = Instantiate(peaPrefab);
        Pea pea = obj.GetComponent<Pea>();
        pea.Init(trait);
        AddPlantToGrid(pea);
    }

    public int GetMaxCol()
    {
        return maxCol;
    }

    public void AddWaveSkipCount(int count)
    {
        waveSkipCount += count;
        return;
    }

    public float GetAdditionalResistance(CompleteTraitType traitType)
    {
        if (additionalResistance.TryGetValue(traitType, out float value))
            return value;
        else
            return 0f;
    }

    public void AddAdditionalResistance(CompleteTraitType traitType, float value)
    {
        if(additionalResistance.TryGetValue(traitType, out float var))
            additionalResistance[traitType] += value;
        else
            additionalResistance[traitType] = value;
        for (int idx = 0; idx < GetMaxCol() * 4; idx++) // 기존에 존재하던 식물의 resistance도 증가
        {
            if (plantGrid.ContainsKey(idx))
            {
                Plant plant = plantGrid[idx];
                plant.UpdateResistance(traitType, value);
            }
        }

        return;
    }

    public void AddAdditionalInheritance(int value)
    {
        additionalInheritance += value;
        return;
    }

    public void AddSoil()
    {
        maxCol += 1;
        GameObject obj = Instantiate(soilPrefab, this.transform);
        obj.transform.localPosition = new Vector3(1.7f * (maxCol-1), 0f, 0f);
        return;
    }

    private void ActivateBreed()
    {
        isBreedButtonPressed = true;
    }

    private void DeactivateBreed()
    {
        isBreedButtonPressed = false;
    }

}

