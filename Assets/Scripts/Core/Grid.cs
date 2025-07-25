using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System.Drawing;

public class Grid : MonoBehaviour
{
    private EnemyController enemyController;

    List<Plant> plants = new List<Plant>();
    [HideInInspector] public int maxCol = 4;
    public Dictionary<int, Plant> plantGrid = new Dictionary<int, Plant>();
    private Dictionary<CompleteTraitType, float> additionalResistance = new Dictionary<CompleteTraitType, float>();
    private int additionalInheritance = 0;
    private float breedTimer = 30.0f;
    private int maxBreedCount = 4;
    private int breedCount = 0;
    public int BreedCount => breedCount;

    private bool isBreeding = false;

    private GameObject breedObj1 = null;
    private GameObject breedObj2 = null;
    private bool isBreedButtonPressed = false;

    private bool isBreedSkipButtonPressed = false;

    private float bugSpeed = 2.5f;
    private float bugSpawnTimeInterval = 5.0f;

    [SerializeField] private GameObject peaPrefab;
    //[SerializeField] private GameObject soilPrefab;
    [SerializeField] private GameObject[] disabledSoil; // 4개 이상의 열이 추가될 때 활성화되는 토양들
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
        enemyController = GameObject.Find("EnemyController").GetComponent<EnemyController>();
        //InitGrid();
        InitSoils();
        breedButton.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InitGrid()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = Instantiate(peaPrefab);
            Pea pea = obj.GetComponent<Pea>();
            List<GeneticTrait> basicTrait = new List<GeneticTrait>
            {
                new GeneticTrait(CompleteTraitType.NaturalDeath, 0.5f, 1)
            };
            pea.SetTrait(basicTrait);
            //plants.Add(pea);
            AddPlantToGrid(pea);
        }
    }

    public IEnumerator Breeding()
    {
        //breedTimer 만큼 동안 아래 과정 반복 진행 가능

        //교배할 부모 완두콩 두 개 선택
        isBreeding = true;
        breedObj1 = null;
        breedObj2 = null;

        //int breedCount = 0;

        breedTimerUI.StartBreedingTimer();

        Debug.Log(breedTimer + "초 시작. 최대 교배 횟수는 " + maxBreedCount + "입니다");
        UpdateBreedCountUI(maxBreedCount);
        float startTime = Time.time;
        float endTime = startTime + breedTimer;

        float spawnBugTime = startTime + bugSpawnTimeInterval;

        breedSkipButton.SetActive(true);
        enemyController.ShowWaveSkipButton();
        isBreedSkipButtonPressed = false;


        while (Time.time < endTime && !isBreedSkipButtonPressed)
        {
            if (Time.time > spawnBugTime)
            {
                List<int> targetIdx = new List<int>(plantGrid.Keys);
                if (targetIdx.Count > 0)
                {
                    SpawnBug();
                    spawnBugTime += bugSpawnTimeInterval;
                }
            }

            if (isBreedButtonPressed)
            {
                if (breedObj1 != null && breedObj2 != null) // 교배 버튼 등으로 추후 수정
                {
                    Plant parent1 = breedObj1.GetComponent<Plant>();
                    Plant parent2 = breedObj2.GetComponent<Plant>();
                    //자식 완두콩 형질 계산 후 Instantiate

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
                        List<GeneticTrait> childTrait = Breed(parent1.GetGeneticTrait(), parent2.GetGeneticTrait());
                        GameObject childObj = Instantiate(peaPrefab);
                        Plant child = childObj.GetComponent<Plant>();
                        if (child != null)
                        {
                            child.SetTrait(childTrait);
                            //plants.Add(child);
                            AddPlantToGrid(child);
                            breedCount++;
                            Debug.Log("자식 생성 성공. 남은 교배 횟수는 " + (maxBreedCount - breedCount) + "입니다");
                            UpdateBreedCountUI(maxBreedCount - breedCount);
                            Plant p1 = breedObj1.GetComponent<Plant>();
                            Plant p2 = breedObj2.GetComponent<Plant>();
                            p1.MakeDefaultSprite();
                            p2.MakeDefaultSprite();
                            breedObj1 = null;
                            breedObj2 = null;
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
                        SoundManager.Instance.PlayEffect("WrongSelect");
                        isBreedButtonPressed = false;
                    }
                    else
                    {
                        Debug.Log("키울 공간이 부족합니다");
                        SoundManager.Instance.PlayEffect("WrongSelect");
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

        if(breedObj1 != null) breedObj1.GetComponent<Plant>().MakeDefaultSprite();
        if(breedObj2 != null) breedObj2.GetComponent<Plant>().MakeDefaultSprite();

        breedTimerUI.StopTimer();
        breedCount = 0;
        Debug.Log("교배 페이즈 종료");
        breedButton.SetActive(false);
        enemyController.HideWaveSkipButton();
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
                plant.Init(idx, this);

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

    /*public void DestroyPlant(int gridNum)
    {
        Plant plant = plantGrid[gridNum];
        plant.Die();
        plantGrid.Remove(gridNum);
        return;
    }*/

    public void ClearGridIndex(int gridIndex)
    {
        if(plantGrid.ContainsKey(gridIndex)) plantGrid.Remove(gridIndex);

        if (CheckGameOver())
        {
            GameManager.Instance.GameOver();
        }
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
        pea.SetTrait(trait);
        AddPlantToGrid(pea);
    }

    public int GetMaxCol()
    {
        return maxCol;
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
        //GameObject obj = Instantiate(soilPrefab, this.transform);
        //obj.transform.localPosition = new Vector3(1.7f * (maxCol-1), 0f, 0f);

        disabledSoil[maxCol - 5].SetActive(true);

        for (int row = 0; row < 4; row++)
        {
            Transform soilT = disabledSoil[maxCol - 5].transform.GetChild(row);
            Soil soil = soilT.GetComponent<Soil>();

            if (soil != null)
            {
                int index = row * maxCol + (maxCol - 1);
                soil.Init(index);
            }
        }
    }
    private void InitSoils()
    {
        for (int col = 0; col < maxCol; col++)
        {
            Transform soilColT = transform.GetChild(col);
            for (int row = 0; row < 4; row++)
            {
                Transform soilT = soilColT.GetChild(row);
                Soil soil = soilT.GetComponent<Soil>();
                if (soil != null)
                {
                    int index = col * 4 + row;
                    soil.Init(index);
                }
            }
        }
    }

    public void RequestBreedSelect(GameObject clickedObject)
    {
        Plant clickedPea = clickedObject.GetComponent<Plant>();
        if (clickedPea == null) return;

        if (breedObj1 == clickedObject)
        {
            // 부모 1 선택 취소
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj1 = null;
        }
        else if (breedObj2 == clickedObject)
        {
            // 부모 2 선택 취소
            SoundManager.Instance.PlayEffect("SelectPlant");
            clickedPea.MakeDefaultSprite();
            breedObj2 = null;
        }
        else if (breedObj1 == null)
        {
            // 부모 1 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj1 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else if (breedObj2 == null)
        {
            // 부모 2 선택
            SoundManager.Instance.PlayEffect("SelectPlant");
            breedObj2 = clickedObject;
            clickedPea.MakeSelectedSprite();
        }
        else
        {
            // 이미 두 부모 선택됨
            SoundManager.Instance.PlayEffect("WrongSelect");
            Debug.Log("이미 두 부모가 모두 선택된 상태");
        }

        breedButton.SetActive(breedObj1 != null && breedObj2 != null);
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

    private void SpawnBug()
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

        obj.GetComponent<Bug>().InitBug(bugSpeed, this, new Vector3(x,y,peaPrefab.transform.position.z));
    }

    public bool GetIsBreeding()
    { 
        return isBreeding;
    }

    public bool TryPlacePlant(Plant plant, Vector3 screenPosition)
    {
        int? targetIndex = GetGridIndexFromPosition(screenPosition);

        // 토양 감지 실패 또는 해당 칸에 이미 식물이 있는 경우
        if (!targetIndex.HasValue || plantGrid.ContainsKey(targetIndex.Value))
        {
            // 원래 위치로 되돌리기
            Transform originalSoil = GetSoilTransform(plant.gridIndex);
            plant.transform.position = originalSoil.position;
            return false;
        }

        // 새로운 위치에 심기
        plantGrid.Remove(plant.gridIndex); // 원래 위치에서 제거
        plantGrid[targetIndex.Value] = plant;

        plant.SetGridIndex(targetIndex.Value);
        Transform soilT = GetSoilTransform(targetIndex.Value);
        plant.transform.position = soilT.position;

        return true;
    }

    public int? GetGridIndexFromPosition(Vector3 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);

        if (hit.collider != null)
        {
            Soil soil = hit.collider.GetComponent<Soil>();
            if (soil != null)
            {
                return soil.GridIndex;
            }
        }
        return null;
    }
}

