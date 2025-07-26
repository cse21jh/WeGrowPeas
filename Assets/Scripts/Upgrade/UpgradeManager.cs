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


    };

    public GameObject upgradePanel;
    private UpgradeCardUI[] upgradeCards;

    private Dictionary<Type, int> remainUpgrade = new();
    private Type[] randomUpgrade = new Type[3];

    private float upgradeTimer = 30.0f;
    private int maxRerollCount = 0;
    public int MaxRerollCount => maxRerollCount;
    private int curRerollCount = 0;
    private bool select = false;

    [SerializeField] TextMeshProUGUI rerollNum;
    [SerializeField] Slider upgradeTimeSlider;

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
                remainUpgrade.Add(type, tmp.MaxAmount);
        }
        // stage ������ ���;� �ϴ� �ʼ� ���׷��̵�
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
        // randomUpgrade�� 3�� �����ϰ� �����ϱ� remainUpgrade 0�̸� �� ��������. reroll�ϸ� �ش� �Լ� ��ȣ��?
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
                Debug.Log($"���׷��̵� ���� {i+1}: {UpgradeInstance[randomUpgrade[i]]().Name}");
                Upgrade randUpgrade = UpgradeInstance[randomUpgrade[i]]();
                upgradeCards[i].Set(randUpgrade, i, this);
            }
            else
            {
                Debug.Log($"���׷��̵� ���� {i+1}: ������ ���׷��̵尡 �����ϴ�");
            }
        }

    }

    public void SelectUpgrade(int idx)
    {
        var tmp = randomUpgrade[idx];
        if (tmp == null)
        { 
            Debug.Log("���׷��̵� ���� X");
            return;
        }
        remainUpgrade[tmp]--;   
        if (remainUpgrade[tmp] == 0)
        {
            remainUpgrade.Remove(tmp);
        }
        UpgradeInstance[tmp]().OnSelectAction(); // ���� ���׷��̵� �۵�. �� upgrade���� �����ص�. 
        Debug.Log($"���׷��̵� : {UpgradeInstance[tmp]().Name}");
        select = true;

        for (int i = 0; i < randomUpgrade.Length; i++)
            randomUpgrade[i] = null;

    }

    public IEnumerator UpgradePhase()
    {
        Debug.Log("���׷��̵� ������ ����. ���� ���� Ƚ���� " + maxRerollCount + " �Դϴ�");
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
            // �ӽ÷� 1,2,3 ��ư ���� �� �ǵ��� ����
            // UI ��� ���� �����ϵ��� ���� �ʿ�
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

            // �ӽ� ���� ���.
            /*if(Input.GetKeyDown(KeyCode.R) && rerollCount < maxRerollCount)
            {
                SetRandomUpgrade();
                rerollCount++;
            }*/

            float timeRatio = Mathf.Clamp01((Time.time - startTime) / upgradeTimer);
            UpgradeTimerUI(timeRatio);

            yield return null;
        }

        Debug.Log("���׷��̵� ������ ����");
        upgradePanel.SetActive(false);
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
}
