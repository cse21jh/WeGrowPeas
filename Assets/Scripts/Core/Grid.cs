using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    List<Plant> plants = new List<Plant>();
    [HideInInspector] public int maxCol = 4;
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    private Dictionary<CompleteTraitType, float> additionalResistance = new Dictionary<CompleteTraitType, float>();
    private int additionalInheritance = 0;
    private float breedTimer = 30.0f;
    private int maxBreedCount = 4;
    private int waveSkipCount = 0;

    private bool isBreeding = false;

    private bool isBreedButtonPressed = false;

    private bool isBreedSkipButtonPressed = false;

    private float bugSpeed = 2.5f;
    private float bugSpawnTimeInterval = 5.0f;

    [SerializeField] private GameObject peaPrefab;
    [SerializeField] private GameObject soilPrefab;
    [SerializeField] private GameObject bugPrefab;

    [SerializeField] private TimerUI breedTimerUI;
    [SerializeField] private GameObject breedButton;
    [SerializeField] private GameObject breedSkipButton;
    [SerializeField] private TextMeshProUGUI breedCountUI;
    
    public int killBugCount = 0;
    public int totalBreedCount = 0;
        

    // Start is called before the first frame update
    void Start()
    {
        InitGrid();
        breedButton.SetActive(false);
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
        isBreeding = true;

        GameObject obj1 = null;
        GameObject obj2 = null;

        int breedCount = 0;

        breedTimerUI.StartBreedingTimer();

        Debug.Log(breedTimer + "초 시작. 최대 교배 횟수는 " + maxBreedCount + "입니다");
        UpdateBreedCountUI(maxBreedCount);
        float startTime = Time.time;
        float endTime = startTime + breedTimer;

        float spawnBugTime = startTime + bugSpawnTimeInterval;

        breedSkipButton.SetActive(true);
        isBreedSkipButtonPressed = false;


        while (Time.time < endTime && !isBreedSkipButtonPressed)
        {
            if (Time.time > spawnBugTime)
            {
                List<int> targetIdx = new List<int>(plantGrid.Keys);
                if (targetIdx.Count > 0)
                {
                    SpawnBug(targetIdx[Random.Range(0, targetIdx.Count)]);
                    spawnBugTime += bugSpawnTimeInterval;
                }
            }

            if (Input.GetMouseButtonDown(0)) // 완두콩 선택 과정. 취소는 이미 눌렀던 완두콩 클릭하면 취소. 
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // 현재는 콜라이더 component 있어야 확인 가능. 추후 grid 좌표 계산 되면 수정 예정.
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    GameObject clickedObject = hit.collider.gameObject;
                    Plant clickedPea = clickedObject.GetComponent<Plant>();
                    Bug clickedBug = clickedObject.GetComponent<Bug>();

                    if (clickedPea != null)
                    {
                        if (obj1 == clickedObject)
                        {
                            //Debug.Log("부모 1 선택 취소");
                            SoundManager.Instance.PlayEffect("SelectPlant");
                            Plant p = obj1.GetComponent<Plant>();
                            p.MakeDefaultSprite();
                            obj1 = null;
                            breedButton.SetActive(false);
                        }
                        else if (obj2 == clickedObject)
                        {
                            //Debug.Log("부모 2 선택 취소");
                            SoundManager.Instance.PlayEffect("SelectPlant");
                            Plant p = obj2.GetComponent<Plant>();
                            p.MakeDefaultSprite();
                            obj2 = null;
                            breedButton.SetActive(false);
                        }
                        else if (obj1 == null)
                        {
                            //Debug.Log("부모 1 선택");
                            SoundManager.Instance.PlayEffect("SelectPlant");
                            obj1 = clickedObject;
                            Plant p = obj1.GetComponent<Plant>();
                            p.MakeSelectedSprite();
                            if (obj1 != null && obj2 != null)
                            {
                                breedButton.SetActive(true);
                            }
                        }
                        else if (obj2 == null)
                        {
                            //Debug.Log("부모 2 선택");
                            SoundManager.Instance.PlayEffect("SelectPlant");
                            obj2 = clickedObject;
                            Plant p = obj2.GetComponent<Plant>();
                            p.MakeSelectedSprite();
                            if (obj1 != null && obj2 != null)
                            {
                                breedButton.SetActive(true);
                            }
                        }
                        else
                        {
                            SoundManager.Instance.PlayEffect("WrongSelect");
                            Debug.Log("이미 두 부모가 모두 선택된 상태");
                        }
                    }
                    else if (clickedBug != null)
                    {
                        StartCoroutine(clickedBug.KillBug());
                    }
                    else
                    {
                        Debug.Log("올바른 오브젝트 선택이 아닙니다.");
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

            if (isBreedButtonPressed)
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
                            UpdateBreedCountUI(maxBreedCount - breedCount);
                            Plant p1 = obj1.GetComponent<Plant>();
                            Plant p2 = obj2.GetComponent<Plant>();
                            p1.MakeDefaultSprite();
                            p2.MakeDefaultSprite();
                            obj1 = null;
                            obj2 = null;
                            DeactivateBreed();
                        }
                        else
                        {
                            Debug.Log("자식 생성에 오류 발생");
                            Destroy(childObj);
                            isBreedButtonPressed = false;
                        }

                    }
                    else if (breedCount >= maxBreedCount)
                    {
                        Debug.Log("최대 교배 횟수 초과");
                        isBreedButtonPressed = false;
                    }
                    else
                    {
                        Debug.Log("키울 공간이 부족합니다");
                        isBreedButtonPressed = false;
                    }


                }
                else
                {
                    Debug.Log("아직 두 콩을 모두 선택하지 않았습니다");
                }
            }

            yield return null;
        }

        if(obj1 != null) obj1.GetComponent<Plant>().MakeDefaultSprite();
        if(obj2 != null) obj2.GetComponent<Plant>().MakeDefaultSprite();


        breedTimerUI.StopTimer();
        Debug.Log("교배 페이즈 종료");
        breedButton.SetActive(false);
        isBreeding = false;
        breedSkipButton.SetActive(false);
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
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 1 : 0); break;
                default: break;
            }

            switch (p2Trait)
            {
                case 2: childGenetic += 1; break;
                case 1: childGenetic += (additionalInheritance + 50 <= Random.Range(1, 101) ? 1 : 0); break;
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
        SoundManager.Instance.PlayEffect("Breed");
        totalBreedCount++;
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

        Destroy(plant.gameObject);
        return;
    }

    public Transform GetSoilTransform(int idx)
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

    public void DestroyPlantByShovel(Plant plant)
    {
        int keyToRemove = -1;

        foreach (var pair in plantGrid)
        {
            if (pair.Value == plant)
            {
                keyToRemove = pair.Key;
                SoundManager.Instance.PlayEffect("Shovel");
                break;
            }
        }

        GameObject.Destroy(plant.gameObject);
        plantGrid.Remove(keyToRemove);

        if (CheckGameOver())
        {
            GameManager.Instance.GameOver();
        }

        return;
    }

    public void DestroyPlantByBug(Plant plant)
    {
        int keyToRemove = -1;

        foreach (var pair in plantGrid)
        {
            if (pair.Value == plant)
            {
                keyToRemove = pair.Key;
                break;
            }
        }

        GameObject.Destroy(plant.gameObject);
        plantGrid.Remove(keyToRemove);

        if (CheckGameOver())
        {
            GameManager.Instance.GameOver();
        }

        return;
    }

    public bool CheckGameOver()
    { 
        if (plantGrid.Count == 0)
            return true;
        return false;
    }

    public void AddBreedTimer(int time)
    {
        breedTimer += time;
        return;
    }

    public float GetBreedTimer()
    {
        return breedTimer;
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

    public void ActivateBreed()
    {
        isBreedButtonPressed = true;
    }

    private void DeactivateBreed()
    {
        breedButton.SetActive(false);
        isBreedButtonPressed = false;
    }

    public void SkipBreed()
    {
        isBreedSkipButtonPressed = true;
    }

    private void UpdateBreedCountUI(int count)
    {
        breedCountUI.text = $"<sprite=8> {count}";
    }

    private void SpawnBug(int targetObjIdx)
    {
        GameObject obj = Instantiate(bugPrefab);

        int edge = Random.Range(0, 4);

        float x=0f;
        float y=0f;

        switch(edge)
        {
            case 0:
                x= Random.Range(-9f, 9f);
                y = 5.0f;
                break;
            case 1:
                x = 9.0f;
                y = Random.Range(-5f, 5f);
                break;
            case 2:
                x = Random.Range(-9f, 9f);
                y = -5.0f;
                break;
            case 3:
                x = -9.0f;
                y = Random.Range(-5f, 5f);
                break;
        }

        obj.GetComponent<Bug>().InitBug(targetObjIdx, bugSpeed, this, new Vector3(x,y,peaPrefab.transform.position.z));
    }

    public bool GetIsBreeding()
    { 
        return isBreeding;
    }

}

