using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class EnemyController : MonoBehaviour
{

    public Grid grid;

    private static readonly List<Wave> unlockedWave = new List<Wave>();

    private Wave lastWave;
    private Wave currentWave;
    private Wave nextWave;

    private Wave noneWave;

    [SerializeField] TextMeshProUGUI nextWaveText;

    // Start is called before the first frame update
    void Start()
    {
        unlockedWave.Add(new AgingWave());
        noneWave = new NoneWave();
        lastWave = unlockedWave[0];
        currentWave = unlockedWave[0];
        nextWave = unlockedWave[0];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnemyWave()
    {
        Wave wave = currentWave;
        Debug.Log("currentWave : " + currentWave);
        SoundManager.Instance.PlayEffect(wave.WaveSoundString);

        if (currentWave != noneWave)
        {
            for (int idx = 0; idx < grid.GetMaxCol() * 4; idx++)
            {
                if (grid.plantGrid.ContainsKey(idx))
                {
                    Plant plant = grid.plantGrid[idx];

                    if (plant.CanResist(wave.WaveType))
                    {
                        Debug.Log(idx + "번째 식물이 웨이브를 버텼습니다");
                    }
                    else
                    {
                        Debug.Log(idx + "번째 식물이 죽었습니다");
                        plant.DieWithAnimation();
                    }

                }
            }
        }
        else
            Debug.Log("오늘은 아무일도 일어나지 않았습니다");
        SetNextWave();
        FlushNextWaveText();
        return;
    }

    public void SetNextWave()
    {
        lastWave = currentWave;
        currentWave = nextWave;
        int next = Random.Range(0, unlockedWave.Count);
        nextWave = unlockedWave[next];
        return;
    }

    public void WaveSkip()
    {
        currentWave = noneWave;
        ShowNextWaveText();
        return;
    }

    public bool IsLastWaveNone()
    {
        if (lastWave == noneWave)
            return true;
        else
            return false;
    }

    public void UnlockWave(int stage)
    {
        // wave 설정은 현 stage의 wave가 끝나고 다다음 wave를 설정할 때 waveUnlocked를 사용. unlock자체는 stage 입장 직전. 즉, 6 스테이지부터 바람이 뜨려면 4스테이지 스테이지 진입 전 언락 필요. 
        switch (stage + 1)
        {
            case 5:
                unlockedWave.Add(new WindWave());
                break;
            case 10:
                unlockedWave.Add(new FloodWave());
                break;
            case 15:
                unlockedWave.Add(new PestWave());
                break;
            case 20:
                unlockedWave.Add(new ColdWave());
                break;
            case 25:
                unlockedWave.Add(new HeavyRainWave());
                break;
        }
        return;
    }

    public void ShowNextWaveText()
    {
        nextWaveText.text = currentWave.WaveDescription;
    }

    private void FlushNextWaveText()
    {
        nextWaveText.text = "";
    }
}
