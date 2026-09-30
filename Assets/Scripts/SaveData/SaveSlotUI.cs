using TMPro;
using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject[] slotItems;
    [SerializeField] private GameObject savePopup;
    [SerializeField] private GameObject clickBlocker;

    [SerializeField] private AbilityUIController abilityUIController;

    private string path;

    private void Start()
    {
        clickBlocker.SetActive(false);
    }

    // 슬롯은 현재 프로필 폴더에 있으므로 프로필이 바뀌면 다시 그린다.
    private void OnEnable()
    {
        SaveManager.OnProfileChanged += SetSlots;
        SetSlots();
    }

    private void OnDisable()
    {
        SaveManager.OnProfileChanged -= SetSlots;
    }

    public void SetSlots()
    {
        for (int i = 0; i < slotItems.Length; i++)
        {
            var tmp = slotItems[i].GetComponentInChildren<TextMeshProUGUI>();

            int slotIndex = i;

            string path = SaveContext.GetSavePath(slotIndex);

            if (!RunSave.Exists(path)) tmp.text = $"저장소 {slotIndex}\n비어 있음";
            else if (RunSave.TryLoad(path, out SaveData saveData)) tmp.text = $"저장소 {slotIndex}\nDay {saveData.progress.stage}";
            else tmp.text = $"저장소 {slotIndex}\n읽을 수 없음";
        }

    }

    public void OnClickSlot(int slotIndex)
    {
        SaveContext.Instance.SelectSlot(slotIndex);

        path = SaveContext.Instance.CurrentSaveFilePath;

        if (RunSave.Exists(path)) //continue
        {
            ShowSavePopup();
        }
        else //new game
        {
            abilityUIController.OpenPlantAbilityPanel();
            //OnClickNewGame();
        }
    }

    public void OnClickNewGame()
    {
        RunSave.Delete(path);
        ActivateBlocker();
        // 화면 덮기/열기 연출은 SceneLoader가 담당한다.
        GameStartContext.SetStartType(GameStartType.NewGame);
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void OnClickContinueGame()
    {
        if (!RunSave.TryLoad(path, out SaveData saveData))
        {
            Debug.LogError($"[SaveSlotUI] 세이브를 읽지 못했습니다: {path}");
            return;
        }
        GameStartContext.SetStartType(RunSave.ResolveContinueStartType(saveData));

        ActivateBlocker();
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void ShowSavePopup()
    {
        savePopup.SetActive(true);
    }

    public void CloseSavePopup()
    {
        savePopup.SetActive(false);
    }

    public void ActivateBlocker()
    {
        clickBlocker.SetActive(true);
    }
}
