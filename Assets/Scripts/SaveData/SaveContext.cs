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

    /// <summary>슬롯을 고르지 않고 정원 씬을 바로 실행했을 때(에디터 테스트) 쓰는 슬롯.</summary>
    public const int EditorFallbackSlot = 2;

    public static string GetSavePath(int slotIndex)
    {
        return Application.dataPath + $"/UserData_{slotIndex}.json";
    }

    public string CurrentSaveFilePath => CurrentSlotIndex < 0 ? null : GetSavePath(CurrentSlotIndex);
    
    public void SelectSlot(int slotIndex)
    {
        CurrentSlotIndex = slotIndex;
    }
}
