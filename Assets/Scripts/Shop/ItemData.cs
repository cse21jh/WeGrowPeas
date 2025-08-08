using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int price;

    [TextArea] public string description;

    // ������ �ִ�(���� �� �� �� �ִ�) ��ǰ����
    public bool isStackable = false;

    // �������� �ǹ� ���� (��: 5������ ���)
    public int initialStock = 1;
}