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

    /// <summary>현재 프로필의 런 세이브 슬롯 경로.</summary>
    public static string GetSavePath(int slotIndex)
    {
        return ProfileStore.PathOf(SlotFileName(slotIndex));
    }

    public static string SlotFileName(int slotIndex) => $"UserData_{slotIndex}.json";

    public const int SlotCount = 3;

    public string CurrentSaveFilePath => CurrentSlotIndex < 0 ? null : GetSavePath(CurrentSlotIndex);
    
    public void SelectSlot(int slotIndex)
    {
        CurrentSlotIndex = slotIndex;
    }
}
