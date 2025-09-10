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

    [SerializeField] private GameObject title;
    [SerializeField] private GameObject description;
    [SerializeField] private GameObject iconDescription;

    private int pageCount = 0; // 신문 우측으로 넘어가는 트리거. 8개 좌측에 들어갔으면, 이후 우측으로 전환
    private int maxPageCount = 9;


    // 설명들 UI들 까는 위치
    private float xPos = -145f;
    private float yPos = 95f;

    // 설명들 간의 간격
    private float yInterval = 30f;

    // 설명들 좌측 깔리는 첫위치
    private float xLeftPos = -145f;
    private float yLeftPos = 95f;

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
            MakeDescription(data.waveDescription); 
            yPos -= 30f;// 얘는 설명 많아서 카운트 하나 더
            pageCount++;
        }

        if(data.upgradeTitle !=null)
        {
            MakeTitle(data.upgradeTitle);
            for(int i = 0; i < data.upgradeDescription.Count; i++ )
            {
                MakeIconDescription(data.upgradeDescription[i], data.upgradeIcon[i]);
            }
        }

        if (data.bugTitle != null)
        {
            MakeTitle(data.bugTitle);
            for (int i = 0; i < data.bugDescription.Count; i++)
            {
                MakeIconDescription(data.bugDescription[i], data.bugIcon[i]);
            }
        }

        if (data.itemTitle!= null)
        {
            MakeTitle(data.itemTitle);
            for (int i = 0; i < data.itemDescription.Count; i++)
            {
                MakeIconDescription(data.itemDescription[i], data.itemIcon[i]);
            }
        }

        if (data.additionalTitle != null)
        {
            MakeTitle(data.additionalTitle);
            for (int i = 0; i < data.additionalDescription.Count; i++)
            {
                MakeDescription(data.additionalDescription[i]);
            }
        }

        pageCount = 0;
        return true;
    }

    private void MakeTitle(string text)
    {
        if (pageCount >= maxPageCount - 1) // 제목은 붙어있도록
        {
            pageCount = -9999;
            xPos = xRightPos;
            yPos = yRightPos;
        }
        GameObject tmp = Instantiate(title, this.transform);
        tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, yPos, 0f);
        tmp.GetComponent<TextMeshProUGUI>().text = text;
        yPos -= yInterval;
        pageCount++;
    }

    private void MakeDescription(string text)
    {
        if (pageCount >= maxPageCount)
        {
            pageCount = -9999;
            xPos = xRightPos;
            yPos = yRightPos;
        }
        GameObject tmp = Instantiate(description, this.transform);
        tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, yPos, 0f);
        tmp.GetComponent<TextMeshProUGUI>().text = text;
        yPos -= yInterval;
        pageCount++;
    }

    private void MakeIconDescription(string text, Sprite sprite)
    {
        if (pageCount >= maxPageCount)
        {
            pageCount = -9999;
            xPos = xRightPos;
            yPos = yRightPos;
        }
        GameObject tmp = Instantiate(iconDescription, this.transform);
        tmp.GetComponent<RectTransform>().anchoredPosition = new Vector3(xPos, yPos, 0f);
        tmp.GetComponentInChildren<TextMeshProUGUI>().text = text;
        tmp.GetComponentInChildren<Image>().sprite = sprite;
        yPos -= yInterval;
        pageCount++;
    }
}
