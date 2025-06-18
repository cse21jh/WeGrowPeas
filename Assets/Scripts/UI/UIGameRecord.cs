using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIGameRecord : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private TextMeshProUGUI textPea;
    [SerializeField] private TextMeshProUGUI textBug;

    // Start is called before the first frame update
    void Start()
    {
        textStage.text = $"<sprite=0> ÃÑ \"{GameRecordHolder.maxStageReached}\"¶ó¿îµå¸¦ ¹öÅß ³Â½À´Ï´Ù.";
        textPea.text = $"<sprite=8> ÃÑ \"{GameRecordHolder.TotalPeas}\"¸¶¸®ÀÇ ¿ÏµÎÄáÀ» Å°¿ü½À´Ï´Ù.";
        textBug.text = $"<sprite=10> ÃÑ \"{GameRecordHolder.TotalBugsKilled}\"¸¶¸®ÀÇ ¹ú·¹¸¦ Àâ¾Ò½À´Ï´Ù.";
        
        //Debug.Log($"{GameRecordHolder.maxStageReached}, {GameRecordHolder.TotalPeas}, {GameRecordHolder.TotalBugsKilled}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
