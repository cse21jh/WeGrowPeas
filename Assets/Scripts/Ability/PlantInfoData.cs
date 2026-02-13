using UnityEngine;

[CreateAssetMenu(fileName = "PlantInfoData", menuName = "Data/PlantInfoData")]
public class PlantInfoData : ScriptableObject
{
    public PlayablePlantType type;
    public string plantName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;
}
