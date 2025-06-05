using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{    
    public static readonly Dictionary<Type, Func<Upgrade>> UpgradeInstance = new()
    {
        { typeof(BreedTimerUpgrade), () => new BreedTimerUpgrade()},
    };

    private Dictionary<Type, int> remainUpgrade = new();
    public Type[] randomUpgrade = new Type[3];

    //public UpgradeManager()
    //{
    //    foreach (var type in UpgradeInstance.Keys)
    //    {
    //        Upgrade tmp = UpgradeInstance[type]();
    //        remainUpgrade.Add(type, tmp.MaxAmount);
    //    }
    //}

    void Start() // 임시로 start에서 되도록
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
    }

    private void SelectUpgrade(int idx)
    {
        //3개중 idx 해당하는 upgrade 선택
        var tmp = randomUpgrade[idx];
        if (--remainUpgrade[tmp] == 0)
            // 해당 업그레이드 남은 횟수 없음. 더이상 등장 안 하도록 수정
        UpgradeInstance[tmp]().OnSelectAction(); // 실제 업그레이드 작동. 각 upgrade에서 선언해둠. 
    }

    public IEnumerator Upgrade()
    {
        // SetRandomUpgrade 통해 업그레이드 3개 무작위 선정
        // UI 띄우거나 임시 방안을 통해 업그레이드 선택
        // 선택 시 SelectUpgrade 호출하여 실제 작동
        // 리롤 가능하도록 구현 필요
        yield return null;
    }
}
