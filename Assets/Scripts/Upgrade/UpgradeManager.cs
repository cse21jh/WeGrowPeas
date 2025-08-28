using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{    
    private static readonly Dictionary<Type, Func<Upgrade>> UpgradeInstance = new()
    {
        
        { typeof(AddNaturalDeathPlantUpgrade), () => new AddNaturalDeathPlantUpgrade()},
        { typeof(AddWindPlantUpgrade), () => new AddWindPlantUpgrade()},
        { typeof(AddFloodPlantUpgrade), () => new AddFloodPlantUpgrade()},
        { typeof(AddPestPlantUpgrade), () => new AddPestPlantUpgrade()},
        { typeof(AddColdPlantUpgrade), () => new AddColdPlantUpgrade()},
        { typeof(AddHeavyRainPlantUpgrade), () => new AddHeavyRainPlantUpgrade()},
        { typeof(NaturalDeathResistenceUpgrade), () => new NaturalDeathResistenceUpgrade()},
        { typeof(WindResistenceUpgrade), () => new WindResistenceUpgrade()},
        { typeof(FloodResistenceUpgrade), () => new FloodResistenceUpgrade()},
        //{ typeof(PestResistenceUpgrade), () => new PestResistenceUpgrade()},
        { typeof(ColdResistenceUpgrade), () => new ColdResistenceUpgrade()},
        { typeof(HeavyRainResistenceUpgrade), () => new HeavyRainResistenceUpgrade()},
        { typeof(AddSoilUpgrade), () => new AddSoilUpgrade()},
        { typeof(BreedTimerUpgrade), () => new BreedTimerUpgrade()},
        { typeof(MaxBreedCountUpgrade), () => new MaxBreedCountUpgrade()},
        { typeof(InheritanceUpgrade), () => new InheritanceUpgrade()},
        { typeof(MaxRerollCountUpgrade), () => new MaxRerollCountUpgrade()},
        { typeof(WaveSkipUpgrade), () => new WaveSkipUpgrade()},
        { typeof(LadybugUpgrade), () => new LadybugUpgrade()},
        { typeof(BugSpeedUpgrade), () => new BugSpeedUpgrade()},
        { typeof(BugGoldUpgrade), () => new BugGoldUpgrade()},
        { typeof(BugFrequencyUpgrade), () => new BugFrequencyUpgrade()},
        //{ typeof(AddNaturalDeathPeanutUpgrade), () => new AddNaturalDeathPeanutUpgrade()},
        //{ typeof(AddWindPeanutUpgrade), () => new AddWindPeanutUpgrade()},
        //{ typeof(AddFloodPeanutUpgrade), () => new AddFloodPeanutUpgrade()},
        //{ typeof(AddPestPeanutUpgrade), () => new AddPestPeanutUpgrade()},
        //{ typeof(AddColdPeanutUpgrade), () => new AddColdPeanutUpgrade()},
        //{ typeof(AddHeavyRainPeanutUpgrade), () => new AddHeavyRainPeanutUpgrade()},
        { typeof(PeanutCopyUpgrade), () => new PeanutCopyUpgrade()},
        { typeof(PeanutGoldUpgrade), () => new PeanutGoldUpgrade()},
        { typeof(AddBasicPeanutUpgrade), () => new AddBasicPeanutUpgrade()},
        // 아래는 디버깅용 
        { typeof(AddNepenthesUpgrade), () => new AddNepenthesUpgrade()},
        { typeof(AddChiliPepperUpgrade), () => new AddChiliPepperUpgrade()},
    };

    public GameObject upgradePanel;
    private UpgradeCardUI[] upgradeCards;
    
    private Type[] randomUpgrade = new Type[3];

    private float upgradeTimer = 50.0f;
    private int maxRerollCount = 0;
    public int MaxRerollCount => maxRerollCount;
    private int curRerollCount = 0;
    private bool select = false;

    [SerializeField] TextMeshProUGUI rerollNum;
    [SerializeField] Slider upgradeTimeSlider;
    
    public List<GeneticTrait> addPeaTrait;
    public List<GeneticTrait> addPeanutTrait;

    public GameObject selectAddPeaOrPeanutButton;


    //저장 필요
    private Dictionary<Type, int> remainUpgrade = new();

    private void Start()
    {
        upgradeCards = upgradePanel.GetComponentsInChildren<UpgradeCardUI>();
        upgradePanel.SetActive(false);
    }

    public void UnlockUpgrade(int stage)
    {
        foreach (var type in UpgradeInstance.Keys)
        {
            Upgrade tmp = UpgradeInstance[type]();
            if (tmp.UnlockStage == stage)
            {
                if (remainUpgrade.ContainsKey(type))
                    continue;
                remainUpgrade.Add(type, tmp.MaxAmount);
            }
        }
        // stage 끝나고 나와야 하는 필수 업그레이드
        switch(stage)
        {
            case 5:
                randomUpgrade[0] = typeof(AddWindPlantUpgrade);  break;
            case 10:
                randomUpgrade[0] = typeof(AddFloodPlantUpgrade); break;
            case 15:
                randomUpgrade[0] = typeof(AddPestPlantUpgrade); break;
            case 20:
                randomUpgrade[0] = typeof(AddColdPlantUpgrade); break;
            case 25:
                randomUpgrade[0] = typeof(AddHeavyRainPlantUpgrade); break;
        }
        return;
    }

    

    private void SetRandomUpgrade()
    {
        // randomUpgrade에 3개 랜덤하게 설정하기 remainUpgrade 0이면 안 나오도록. reroll하면 해당 함수 재호출?
        List<Type> availableUpgrades = remainUpgrade.Where(kvp => kvp.Value != 0).Select(kvp => kvp.Key).ToList();
        
        for(int i = 0; i<randomUpgrade.Length; i++)
        {
            if (availableUpgrades.Count == 0)
                break;

            int randomIndex = UnityEngine.Random.Range(0, availableUpgrades.Count);
            if (randomUpgrade[i] == null)
            {
                randomUpgrade[i] = availableUpgrades[randomIndex];
                availableUpgrades.RemoveAt(randomIndex);
            }
            else
            {
                int a = availableUpgrades.IndexOf(randomUpgrade[i]);
                if(a != -1)
                {
                    availableUpgrades.RemoveAt(a);
                }
            }
        }

        for (int i = 0; i < randomUpgrade.Length; i++)
        {
            if (randomUpgrade[i] != null)
            {
                Debug.Log($"업그레이드 슬롯 {i+1}: {UpgradeInstance[randomUpgrade[i]]().Name}");
                Upgrade randUpgrade = UpgradeInstance[randomUpgrade[i]]();
                upgradeCards[i].Set(randUpgrade, i, this);
            }
            else
            {
                Debug.Log($"업그레이드 슬롯 {i+1}: 가능한 업그레이드가 없습니다");
            }
        }

    }

    public void SelectUpgrade(int idx)
    {
        var tmp = randomUpgrade[idx];
        if (tmp == null)
        { 
            Debug.Log("업그레이드 존재 X");
            return;
        }
        remainUpgrade[tmp]--;   
        UpgradeInstance[tmp]().OnSelectAction(); // 실제 업그레이드 작동. 각 upgrade에서 선언해둠. 
        Debug.Log($"업그레이드 : {UpgradeInstance[tmp]().Name}");
        if (UpgradeInstance[randomUpgrade[idx]]().UpgradeId <= 6 && UpgradeInstance[randomUpgrade[idx]]().UpgradeId >= 1)
            selectAddPeaOrPeanutButton.SetActive(true);
        else
            select = true;

        for (int i = 0; i < randomUpgrade.Length; i++)
            randomUpgrade[i] = null;

    }

    public IEnumerator UpgradePhase()
    {
        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Upgrade);

        Debug.Log("업그레이드 페이즈 시작. 리롤 가능 횟수는 " + maxRerollCount + " 입니다");
        ClickRouter.Instance.IsBlockedByUI = true;
        curRerollCount = maxRerollCount;
        SetRerollCountUI(curRerollCount);

        SetRandomUpgrade();
        upgradePanel.SetActive(true);
        select = false;
        

        float startTime = Time.time;
        float endTime = startTime + upgradeTimer;
        //int rerollCount = 0;

        while (!select && (Time.time < endTime))
        {
            // 임시로 1,2,3 버튼 누를 시 되도록 설정
            // UI 띄워 선택 가능하도록 수정 필요
            /*if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectUpgrade(0);
                select = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectUpgrade(1);
                select = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectUpgrade(2);
                select = true;
            }*/

            // 임시 리롤 기능.
            /*if(Input.GetKeyDown(KeyCode.R) && rerollCount < maxRerollCount)
            {
                SetRandomUpgrade();
                rerollCount++;
            }*/

            float timeRatio = Mathf.Clamp01((Time.time - startTime) / upgradeTimer);
            UpgradeTimerUI(timeRatio);

            yield return null;
        }

        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Normal);
        Debug.Log("업그레이드 페이즈 종료");
        upgradePanel.SetActive(false);
        selectAddPeaOrPeanutButton.SetActive(false);
        ClickRouter.Instance.IsBlockedByUI = false;
        yield return null;
    }

    public void AddMaxRerollCount(int count)
    {
        maxRerollCount += count;
        return;
    }

    public void Reroll()
    {
        if (curRerollCount > 0)
        {
            for (int i = 0; i < randomUpgrade.Length; i++)
                randomUpgrade[i] = null;
            SetRandomUpgrade();
            curRerollCount--;
            SetRerollCountUI(curRerollCount);
        }
    }

    private void SetRerollCountUI(int count)
    {
        rerollNum.text = count.ToString();
    }

    private void UpgradeTimerUI(float timeRatio)
    {
        upgradeTimeSlider.value = timeRatio;
    }

    public Dictionary<Type, int> GetRemainUpgrade()
    {
        return remainUpgrade;
    }

    public Dictionary<Type, Func<Upgrade>> GetUpgradeInstance()
    {
        return UpgradeInstance;
    }

    public void LoadUpgradeManager(SaveData saveData)
    {
        maxRerollCount = saveData.remainUpgradeRerollCount;
        int idx;
        foreach (var type in UpgradeInstance.Keys)
        {
            Upgrade tmp = UpgradeInstance[type]();

            if (tmp.UnlockStage <= saveData.stage)
            {
                idx = saveData.remainUpgradeId.IndexOf(tmp.UpgradeId); // 해당 인덱스가 없다면 저장된 값에 없는 것. 오류
                if (idx == -1)
                {
                    Debug.Log("이거슨 버그입니다");
                    continue;
                }

                if (remainUpgrade.ContainsKey(type)) // 모종의 이유로 (unlockUpgrade를 먼저 했거나...) 이미 remainUpgrade에 값이 있는 경우 남은 업그레이드 개수만 갱신
                {
                    remainUpgrade[type] = saveData.remainUpgradeCount[idx];
                    continue;
                }
                remainUpgrade.Add(type, saveData.remainUpgradeCount[idx]);
            }
        }

        // 필수 업그레이드가 있는 경우
        switch (saveData.stage)
        {
            case 5:
                randomUpgrade[0] = typeof(AddWindPlantUpgrade); break;
            case 10:
                randomUpgrade[0] = typeof(AddFloodPlantUpgrade); break;
            case 15:
                randomUpgrade[0] = typeof(AddPestPlantUpgrade); break;
            case 20:
                randomUpgrade[0] = typeof(AddColdPlantUpgrade); break;
            case 25:
                randomUpgrade[0] = typeof(AddHeavyRainPlantUpgrade); break;
        }
        return;
    }


    public void AddPeaUgrade()
    {
        if(addPeaTrait != null)
            GameManager.Instance.grid.AddPea(addPeaTrait);
        addPeaTrait = null;
        select = true;
    }

    public void AddPeanutUpgrade()
    {
        if(addPeanutTrait != null)
            GameManager.Instance.grid.AddPeanut(addPeanutTrait);
        addPeanutTrait = null;
        select = true;
    }

    public void TutorialUpgrade()
    {
        StartCoroutine(TUpgradePhase());
    }

    public IEnumerator TUpgradePhase()
    {
        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Upgrade);

        Debug.Log("업그레이드 페이즈 시작. 리롤 가능 횟수는 " + maxRerollCount + " 입니다");
        ClickRouter.Instance.IsBlockedByUI = true;
        curRerollCount = maxRerollCount;
        SetRerollCountUI(curRerollCount);

        randomUpgrade[0] = typeof(AddFloodPlantUpgrade);
        randomUpgrade[1] = typeof(AddSoilUpgrade);
        randomUpgrade[2] = typeof(MaxBreedCountUpgrade);
        SetRandomUpgrade();
        upgradePanel.SetActive(true);
        select = false;


        /*float startTime = Time.time;
        float endTime = startTime + upgradeTimer;
        //int rerollCount = 0;

        while (!select && (Time.time < endTime))
        {
            float timeRatio = Mathf.Clamp01((Time.time - startTime) / upgradeTimer);
            UpgradeTimerUI(timeRatio);

            yield return null;
        }*/

        FindAnyObjectByType<UIAnimationManager>().SwitchCameras(CameraManager.CameraType.Normal);
        Debug.Log("업그레이드 페이즈 종료");
        upgradePanel.SetActive(false);
        selectAddPeaOrPeanutButton.SetActive(false);
        ClickRouter.Instance.IsBlockedByUI = false;
        yield return null;
    }
}
