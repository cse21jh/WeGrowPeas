using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int price;

    [TextArea] public string description;

    // 개수가 있는(여러 번 살 수 있는) 물품인지
    public bool isStackable = false;

    // 스택형만 의미 있음 (예: 5개까지 재고)
    public int initialStock = 1;
}