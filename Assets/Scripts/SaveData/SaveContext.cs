using UnityEngine;
using System.IO;

public class SaveContext : MonoBehaviour
{
    public static SaveContext Instance { get; private set; }

    public int CurrentSlotIndex { get; private set; } = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public string GetSavePath(int slotIndex)
    {
        return Application.dataPath + $"/UserData_{slotIndex}.json";
    }

    public string CurrentSaveFilePath => CurrentSlotIndex < 0 ? null : GetSavePath(CurrentSlotIndex);
    
    public void SelectSlot(int slotIndex)
    {
        CurrentSlotIndex = slotIndex;
    }
}
