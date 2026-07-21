using UnityEngine;

[DisallowMultipleComponent]
public class PhoneHomeGridGeneratedSlot : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string entryId;

    [SerializeField, HideInInspector]
    private GameObject sourcePrefab;

    public string EntryId
    {
        get => entryId;
        set => entryId = value;
    }

    public GameObject SourcePrefab
    {
        get => sourcePrefab;
        set => sourcePrefab = value;
    }
}
