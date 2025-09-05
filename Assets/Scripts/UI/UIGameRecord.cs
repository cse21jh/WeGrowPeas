using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIGameRecord : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private TextMeshProUGUI textPea;
    [SerializeField] private TextMeshProUGUI textBug;

    private string[] endingText = new string[4]
    {
        "살아남는 데는 성공했지만, 식량 확보가 보장되지 않는다고 판단되어 다른 대안을 탐색하러 떠났다…",
        "안정적인 환경에서 생산이 빠른 좋은 식량으로 평가받아 환경적인 변화가 크게 없는 일부 지역에서 쓰이게 되었다.",
        "다양한 환경에서 괜찮은 생산량을 보여 주었기에 비상시에 사용될 대체식품으로 각광받으며 좋은 먹거리가 되었다.",
        "뛰어난 적응성과 번식 속도를 입증해 전세계에 확산되었고, 이후 인류의 핵심적인 식량이 되었다!"
    };

    // Start is called before the first frame update
    void Start()
    {
        textStage.text = $"<sprite=0> 총 \"{GameRecordHolder.maxStageReached}\"라운드를 버텨 냈습니다.";
        textPea.text = $"<sprite=8> 총 \"{GameRecordHolder.TotalPeas}\"마리의 완두콩을 키웠습니다.";
        textBug.text = $"<sprite=10> 총 \"{GameRecordHolder.TotalBugsKilled}\"마리의 벌레를 잡았습니다.";
        
        //Debug.Log($"{GameRecordHolder.maxStageReached}, {GameRecordHolder.TotalPeas}, {GameRecordHolder.TotalBugsKilled}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
