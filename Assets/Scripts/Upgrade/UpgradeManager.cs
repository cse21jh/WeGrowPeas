using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{    
    public static readonly Dictionary<Type, Func<Upgrade>> UpgradeInstance = new()
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
        { typeof(PestResistenceUpgrade), () => new PestResistenceUpgrade()},
        { typeof(ColdResistenceUpgrade), () => new ColdResistenceUpgrade()},
        { typeof(HeavyRainResistenceUpgrade), () => new HeavyRainResistenceUpgrade()},
        { typeof(AddSoilUpgrade), () => new AddSoilUpgrade()},
        { typeof(BreedTimerUpgrade), () => new BreedTimerUpgrade()},
        { typeof(MaxBreedCountUpgrade), () => new MaxBreedCountUpgrade()},
        { typeof(InheritanceUpgrade), () => new InheritanceUpgrade()},
        { typeof(MaxRerollCountUpgrade), () => new MaxRerollCountUpgrade()},
        { typeof(WaveSkipUpgrade), () => new WaveSkipUpgrade()},


    };

    private Dictionary<Type, int> remainUpgrade = new();
    private Type[] randomUpgrade = new Type[3];

    private float upgradeTimer = 30.0f;
    private int maxRerollCount = 0;

    void Start() 
    {
        InitUpgrade();
    }

    private void InitUpgrade()
    {
        foreach (var type in UpgradeInstance.Keys)
        {
            Upgrade tmp = UpgradeInstance[type]();
            remainUpgrade.Add(type, tmp.MaxAmount);
        }
    }

    private void SetRandomUpgrade()
    {
        // randomUpgrade에 3개 랜덤하게 설정하기 remainUpgrade 0이면 안 나오도록. reroll하면 해당 함수 재호출?
        List<Type> availableUpgrades = remainUpgrade.Where(kvp => kvp.Value != 0).Select(kvp => kvp.Key).ToList();
        for (int i = 0; i < randomUpgrade.Length; i++)
            randomUpgrade[i] = null; 

        
        for(int i = 0; i<randomUpgrade.Length; i++)
        {
            if (availableUpgrades.Count == 0)
                break;

            int randomIndex = UnityEngine.Random.Range(0, availableUpgrades.Count);
            randomUpgrade[i] = availableUpgrades[randomIndex];
            availableUpgrades.RemoveAt(randomIndex);
        }

        for (int i = 0; i < randomUpgrade.Length; i++)
        {
            if (randomUpgrade[i] != null)
            {
                Debug.Log($"Random Upgrade Slot {i+1}: {randomUpgrade[i].Name}");
            }
            else
            {
                Debug.Log($"Random Upgrade Slot {i+1}: Empty");
            }
        }

    }

    private void SelectUpgrade(int idx)
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
    }

    public IEnumerator UpgradePhase()
    {
        Debug.Log("업그레이드 페이즈 시작. 리롤 가능 횟수는 " + maxRerollCount + " 입니다");
        
        SetRandomUpgrade();
        bool select = false;
        

        float startTime = Time.time;
        float endTime = startTime + upgradeTimer;
        int rerollCount = 0;

        while (!select && (Time.time < endTime))
        {
            // 임시로 1,2,3 버튼 누를 시 되도록 설정
            // UI 띄워 선택 가능하도록 수정 필요
            if (Input.GetKeyDown(KeyCode.Alpha1))
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
            }
            
            // 임시 리롤 기능.
            if(Input.GetKeyDown(KeyCode.R) && rerollCount < maxRerollCount)
            {
                SetRandomUpgrade();
                rerollCount--;
            }
            yield return null;
        }

        Debug.Log("업그레이드 페이즈 종료");
        yield return null;
    }

    public void AddMaxRerollCount(int count)
    {
        maxRerollCount += count;
        return;
    }

}
