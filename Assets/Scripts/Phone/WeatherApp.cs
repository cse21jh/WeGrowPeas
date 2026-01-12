using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeatherApp : MonoBehaviour
{
    [SerializeField] TMP_Text stageText;
    [SerializeField] TMP_Text waveText;
    [SerializeField] TMP_Text dangerousPlantText;
    [SerializeField] Image waveImage;

    [SerializeField] private GameObject pastWeatherPrefab;
    [SerializeField] private GameObject pastWeatherKillPrefab;

    [SerializeField] private Transform scrollViewContent;    

    public void LoadNextDay(int stage, Wave lastWave, Wave currentWave, int lastDangerousPlantCount, int currentDangerousPlantCount, int dieCount)
    {
        UpdateCurrentWave(stage + 1, currentWave, currentDangerousPlantCount);
        AddPastWeatherKillPrefab(stage, lastWave, dieCount);
        AddPastWeatherPrefab(stage, lastWave, lastDangerousPlantCount);        
    }

    public void UpdateCurrentWave(int stage, Wave wave, int dangerousPlantCount)
    {
        if(wave.WaveType == WaveType.None)
        {
            stageText.text = stage.ToString() + "일차";
            waveText.text = "아무 일도 일어나지 않을 예정입니다!";
            dangerousPlantText.text = "";
            // 웨이브 이미지 삽입 필요
        }
        else
        {
            stageText.text = stage.ToString() + "일차";
            waveText.text = wave.WaveName + "웨이브가 지나갈 예정입니다!";
            dangerousPlantText.text = wave.WaveName + "저항이 없는 식물 " + dangerousPlantCount.ToString() + "개";
            // 웨이브 이미지 삽입 필요
        }
    }

    public void AddPastWeatherPrefab(int stage, Wave wave, int dangerousPlantCount)
    {

        GameObject newPastWeather = Instantiate(pastWeatherPrefab, scrollViewContent);

        
        newPastWeather.transform.SetAsFirstSibling();

        TMP_Text stageText = newPastWeather.transform.Find("Stage").GetComponent<TMP_Text>();
        TMP_Text waveText = newPastWeather.transform.Find("Wave").GetComponent<TMP_Text>();
        TMP_Text dangerousPlantText = newPastWeather.transform.Find("DangerousPlant").GetComponent<TMP_Text>();
        Image waveImage = newPastWeather.transform.Find("WaveImage").GetComponent<Image>();

        if (wave.WaveType == WaveType.None)
        {
            stageText.text = "안전재난경보 - " + stage.ToString() + "일차";
            waveText.text = "아무 일도 일어나지 않았습니다!";
            dangerousPlantText.text = "";
            // 웨이브 이미지 삽입 필요
        }
        else
        {
            stageText.text = "안전재난경보 - " + stage.ToString() + "일차";
            waveText.text = wave.WaveName + " 웨이브가 지나갔습니다!";
            dangerousPlantText.text = wave.WaveName + " 저항이 없던 식물 " + dangerousPlantCount.ToString() + "개";
            // 웨이브 이미지 삽입 필요
        }
    }

    public void AddPastWeatherKillPrefab(int stage, Wave wave, int dieCount)
    {
        if (wave.WaveType == WaveType.None)
        {
            return;
        }
        GameObject newPastWeather = Instantiate(pastWeatherKillPrefab, scrollViewContent);


        newPastWeather.transform.SetAsFirstSibling();

        TMP_Text stageText = newPastWeather.transform.Find("Stage").GetComponent<TMP_Text>();
        TMP_Text waveText = newPastWeather.transform.Find("Wave").GetComponent<TMP_Text>();
        Image waveImage = newPastWeather.transform.Find("WaveImage").GetComponent<Image>();

        stageText.text = "안전재난경보 - " + stage.ToString() + "일차";
        waveText.text = wave.WaveName + " 웨이브로 " + dieCount.ToString() + "개의 식물이 시들었습니다";
        // 웨이브 이미지 삽입 필요
    }
}
