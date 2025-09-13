using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Newspaper : MonoBehaviour
{
    [SerializeField]
    private List<NewspaperData> newspaperData = new List<NewspaperData>();

    [SerializeField]
    private NewspaperAdditionalData newspaperAdditionalData;

    [SerializeField] private GameObject title;
    [SerializeField] private GameObject description;
    [SerializeField] private GameObject iconDescription;
    [SerializeField] private GameObject bigIconDescription;
    [SerializeField] private GameObject TMI;


    // 설명들 UI들 까는 위치
    private float xPos = -140f;
    private float yPos = 70f;

    // 설명들 간의 기본 간격
    private float yInterval = 30f;

    // 설명들 좌측 깔리는 첫위치
    private float xLeftPos = -140f;
    private float yLeftPos = 70f;

    // 설명들 우측으로 넘어갔을 때의 첫 위치
    private float xRightPos = 130f;
    private float yRightPos = 150f;

    public bool UpdateNewspaper()
    {
        NewspaperData data = null; 
        for(int i = 0; i < newspaperData.Count; i++)
        {
            if (newspaperData[i].stage == GameManager.Instance.stage)
                data = newspaperData[i];
        }

        if (data == null) // 신문 데이터 없으면 아예 신문이 뜨지 않도록
            return false;

        xPos = xLeftPos;
        yPos = yLeftPos;

        // 웨이브 설명
        if (data.waveTitle != null)
        {
            MakeTitle(data.waveTitle);
            yPos -= 10f;
            MakeIconDescription(data.waveDescription, data.waveIcon, bigIconDescription); 
            yPos -= 50f;// 얘는 설명 많아서 카운트 하나 더
        }

        if (data.bugTitle != null)
        {
            MakeTitle(data.bugTitle);
            MakeDescription(data.additionalBugDescription, description);
            for (int i = 0; i < data.bugDescription.Count; i++)
            {
                MakeIconDescription(data.bugDescription[i], data.bugIcon[i],iconDescription);
            }
        }

        xPos = xRightPos;
        yPos = yRightPos;

        if (data.upgradeTitle !=null)
        {
            MakeTitle(data.upgradeTitle);
            yPos -= 10f;
            for(int i = 0; i < data.upgradeDescription.Count; i++ )
            {
                MakeIconDescription(data.upgradeDescription[i], data.upgradeIcon[i],iconDescription);
            }
        }

        xPos = 130f;
        yPos = -130f;

        if(newspaperAdditionalData != null)
        {
            //string text = newspaperAdditionalData.TMI[Random.Range(0, newspaperAdditionalData.TMI.Count)];
            string text = newspaperAdditionalData.TMI[6];
            MakeDescription(text, TMI);
        }

        return true;
    }

    private void MakeTitle(string text)
    {
        GameObject tmp = Instantiate(title, this.transform);
        tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, yPos, 0f);
        tmp.GetComponent<TextMeshProUGUI>().text = text;
        yPos -= yInterval;
    }

    private void MakeDescription(string text, GameObject prefab)
    {
        GameObject tmp = Instantiate(description, this.transform);
        tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, yPos, 0f);
        tmp.GetComponent<TextMeshProUGUI>().text = text;
        yPos -= yInterval;
    }

    private void MakeIconDescription(string text, Sprite sprite, GameObject prefab)
    {
        GameObject tmp = Instantiate(prefab, this.transform);
        tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, yPos, 0f);
        tmp.GetComponentInChildren<TextMeshProUGUI>().text = text;
        tmp.GetComponentInChildren<Image>().sprite = sprite;
        yPos -= yInterval;
    }
}
