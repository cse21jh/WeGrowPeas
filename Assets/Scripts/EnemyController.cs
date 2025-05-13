using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType
{
    Aging,
    Wind,
    Flood,
    Pest,
    Cold,
    HeavyRain,
    None
}

public class EnemyController : MonoBehaviour
{
    public Grid grid;

    public WaveType currentWave;
    public WaveType nextWave;

    // Start is called before the first frame update
    void Start()
    {
        currentWave = WaveType.Aging;
        nextWave = WaveType.Aging;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnemyWave()
    {
        WaveType wave = currentWave;
        Debug.Log("currentWave : " + currentWave);
        for (int idx = 0; idx < grid.maxCol * 4; idx++)
        {
            if (grid.plantGrid.ContainsKey(idx))
            {
                Plant plant = grid.plantGrid[idx];

                if(plant.CanResist(wave))
                {
                    Debug.Log(idx + "번째 식물이 웨이브를 버텼습니다");
                }
                else
                {
                    Debug.Log(idx + "번째 식물이 죽었습니다");
                    grid.DestroyPlant(idx);
                }

            }
        }
        SetNextWave();
        return;
    }

    private void SetNextWave()
    {
        currentWave = nextWave;
        int next = Random.Range(0, 2);
        nextWave = (WaveType)next;
        return;
    }

}
