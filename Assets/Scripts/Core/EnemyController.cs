using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private static readonly Dictionary<WaveType, string> waveDescriptions = new()
    {
        { WaveType.Aging, "곧 하루가 지나갑니다......" },
        { WaveType.Wind, "거센 바람이 몰아칩니다......" },
        { WaveType.Flood, "홍수가 덮쳐옵니다......" },
        { WaveType.Pest, "불길한 날개소리가 들립니다......" },
        { WaveType.Cold, "기온이 떨어지고 있습니다......" },
        { WaveType.HeavyRain, "폭우가 내리기 시작합니다......" },
        { WaveType.None, "오늘은 아무 일도 일어나지 않을 것 같습니다." }
    };

    private static readonly Dictionary<WaveType, string> waveSoundString= new()
    {
        { WaveType.Aging, "Aging" },
        { WaveType.Wind, "Wind" },
        { WaveType.Flood, "Flood" },
        { WaveType.Pest, "Pest" },
        { WaveType.Cold, "Cold" },
        { WaveType.HeavyRain, "HeavyRain" },
        { WaveType.None, "Aging" }
    };

    public Grid grid;

    private WaveType currentWave;
    private WaveType nextWave;
    private int waveUnlocked = 2;

    [SerializeField] TextMeshProUGUI nextWaveText;

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
        SoundManager.Instance.PlayEffect(waveSoundString[currentWave]);

        for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
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
        FlushNextWaveText();
        return;
    }

    public void SetNextWave()
    {
        currentWave = nextWave;
        int next = Random.Range(0, waveUnlocked);
        nextWave = (WaveType)next;
        return;
    }

    public void UnlockWave(int stage)
    {
        // wave 설정은 현 stage의 wave가 끝나고 다다음 wave를 설정할 때 waveUnlocked를 사용. unlock자체는 stage 입장 직전. 즉, 6 스테이지부터 바람이 뜨려면 4스테이지 스테이지 진입 전 언락 필요. 
        switch (stage + 1)
        {
            case 5:
                waveUnlocked++;
                break;
            case 10:
                waveUnlocked++;
                break;
            case 15:
                waveUnlocked++;
                break;
            case 20:
                waveUnlocked++;
                break;
            case 25:
                waveUnlocked++;
                break;
        }
        return;
    }

    public void ShowNextWaveText()
    {
        nextWaveText.text = waveDescriptions[currentWave];
    }

    private void FlushNextWaveText()
    {
        nextWaveText.text = "";
    }
}
