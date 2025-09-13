using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewspaperAdditionalData", menuName = "Scriptable Objects/NewspaperAdditionalData")]
public class NewspaperAdditionalData : ScriptableObject
{
    [Header("추가 설명")]
    [TextArea(3, 5)]
    public List<string> TMI;
}
