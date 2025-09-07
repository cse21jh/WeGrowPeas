using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIGameRecord : MonoBehaviour
{
    /*[SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private TextMeshProUGUI textPea;
    [SerializeField] private TextMeshProUGUI textBug;*/

    private TextMeshProUGUI endingText, pg1, pg2, pg3, pg4;

    private string[] endingTextDisc = new string[4]
    {
        "살아남는 데는 성공했지만, 식량 확보가 보장되지 않는다고 판단되어 다른 대안을 탐색하러 떠났다…",
        "안정적인 환경에서 생산이 빠른 좋은 식량으로 평가받아 환경적인 변화가 크게 없는 일부 지역에서 쓰이게 되었다.",
        "다양한 환경에서 괜찮은 생산량을 보여 주었기에 비상시에 사용될 대체식품으로 각광받으며 좋은 먹거리가 되었다.",
        "뛰어난 적응성과 번식 속도를 입증해 전세계에 확산되었고, 이후 인류의 핵심적인 식량이 되었다!"
    };

    // Start is called before the first frame update
    void Start()
    {
        /*textStage.text = $"<sprite=0> 총 \"{GameRecordHolder.maxStageReached}\"라운드를 버텨 냈습니다.";
        textPea.text = $"<sprite=8> 총 \"{GameRecordHolder.TotalPeas}\"마리의 완두콩을 키웠습니다.";
        textBug.text = $"<sprite=10> 총 \"{GameRecordHolder.TotalBugsKilled}\"마리의 벌레를 잡았습니다.";
        
        //Debug.Log($"{GameRecordHolder.maxStageReached}, {GameRecordHolder.TotalPeas}, {GameRecordHolder.TotalBugsKilled}");*/

        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        endingText = texts.FirstOrDefault(t => t.name == "EndingText");
        pg1 = texts.FirstOrDefault(t => t.name == "PG1");
        pg2 = texts.FirstOrDefault(t => t.name == "PG2");
        pg3 = texts.FirstOrDefault(t => t.name == "PG3");
        pg4 = texts.FirstOrDefault(t => t.name == "PG4");

        SetEndingMailContent();
    }
        

    private void SetEndingMailContent()
    {
        endingText.text = $"우리는 {GameRecordHolder.maxStageReached}일간 {endingTextDisc[GameRecordHolder.PlayerRank]}";

        pg1.text = $"총 \"{GameRecordHolder.maxStageReached}\"일을 버텼다!";

        pg2.text = $"\"{GameRecordHolder.TotalPeas}\"개의 완두콩 중 \"{GameRecordHolder.soldPeas}\"개를 판매했다.\n" +
            $"\"{GameRecordHolder.TotalPeanuts}\"개의 땅콩 중 \"{GameRecordHolder.soldPeanuts}\"개를 판매했다.\n" +
            $"벌레는 \"{GameRecordHolder.TotalBugsKilled}\"마리 잡았다.\n" +
            $"총 \"{GameRecordHolder.totalGoldEarned}\"골드를 벌었다!\n" +
            $"상점에서 \"{GameRecordHolder.totalGoldSpend}\"골드를 소모했다.";

        if(GameRecordHolder.PopularItemName == null)
        {
            pg3.text = $"우리 농장은 {GameRecordHolder.MostKilledWave}에 취약했다…\n" +
            $"사람들은 {GameRecordHolder.MostSellPlantName}을 가장 좋아하는 듯하다.";
        }
        else
        {
            pg3.text = $"우리 농장은 {GameRecordHolder.MostKilledWave}에 취약했다…\n" +
            $"상점에서 {GameRecordHolder.PopularItemName}를 애용했다.\n" +
            $"사람들은 {GameRecordHolder.MostSellPlantName}을 가장 좋아하는 듯하다.";
        }

        pg4.text = "";
    }
}

