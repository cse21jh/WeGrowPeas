using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewspaperData", menuName = "Scriptable Objects/NewspaperData")]
public class NewspaperData : ScriptableObject
{
    [Header("스테이지")]
    public int stage;

    [Header("웨이브")]
    public string waveTitle;
    public string waveDescription;

    [Header("업그레이드")]
    public string upgradeTitle;
    public List<string> upgradeDescription = new List<string>();
    public List<Sprite> upgradeIcon = new List<Sprite>();

    [Header("벌레")]
    public string bugTitle;
    public List<string> bugDescription = new List<string>();
    public List<Sprite> bugIcon = new List<Sprite>();

    [Header("상점 품목")]
    public string itemTitle;
    public List<string> itemDescription = new List<string>();
    public List<Sprite> itemIcon = new List<Sprite>();

    [Header("기타 설명")]
    public string additionalTitle;
    public List<string> additionalDescription = new List<string>();
    
}
