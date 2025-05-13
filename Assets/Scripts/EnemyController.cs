using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Grid grid;

    public WaveType currentWave;
    public WaveType nextWave;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnemyWave()
    {
        WaveType wave = currentWave;
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
        return;
        // grid의 plants 순회하며 plant 있는 칸에 plant 받아오기
        // 현 wave의 resistance value 가져와서 해당 확률 따라 죽일지 살릴지 결정. getresistance value 받아와야 하고, 추가로 해당 값 따라 랜덤으로 죽는지 사는지 결정하도록 알고리즘
        // 죽었으면 plant에서 die 띄우기 
    }
}
