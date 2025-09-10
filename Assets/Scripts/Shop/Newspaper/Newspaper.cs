using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Newspaper : MonoBehaviour
{
    [SerializeField]
    private List<NewspaperData> newspaperData = new List<NewspaperData>();

    [SerializeField]
    private GameObject waveArticle;

    [SerializeField]
    private GameObject upgradeArticle;

    [SerializeField]
    private GameObject bugArticle;

    [SerializeField]
    private GameObject itemArticle;

    [SerializeField]
    private GameObject additionalArticle;
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


        return true;
    }
}
