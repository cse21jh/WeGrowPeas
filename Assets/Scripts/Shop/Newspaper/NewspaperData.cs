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
    [TextArea(3, 5)]
    public string waveDescription;
    public Sprite waveIcon;

    [Header("벌레")]
    public string bugTitle;
    [TextArea(3, 5)]
    public string additionalBugDescription;
    [TextArea(3, 5)]
    public List<string> bugDescription = new List<string>();
    public List<Sprite> bugIcon = new List<Sprite>();

    [Header("업그레이드")]
    public string upgradeTitle;
    [TextArea(3, 5)]
    public List<string> upgradeDescription = new List<string>();
    public List<Sprite> upgradeIcon = new List<Sprite>();    
}
