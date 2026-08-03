using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.Collections;
using System.IO;
using TMPro;
using Unity.Collections;
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
        SetSlots();
        clickBlocker.SetActive(false);
    }

    public void SetSlots()
    {
        for (int i = 0; i < slotItems.Length; i++)
        {
            var tmp = slotItems[i].GetComponentInChildren<TextMeshProUGUI>();

            int slotIndex = i;

            string path = SaveContext.Instance.GetSavePath(slotIndex);

            if (!File.Exists(path)) tmp.text = $"저장소 {slotIndex}\n비어 있음";
            else
            {
                string json = File.ReadAllText(path);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                tmp.text = $"저장소 {slotIndex}\nDay {saveData.stage}";
            }
        }

    }

    private IEnumerator DelayAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(delay);
        action?.Invoke();
    }

    public void OnClickSlot(int slotIndex)
    {
        SaveContext.Instance.SelectSlot(slotIndex);

        path = SaveContext.Instance.CurrentSaveFilePath;

        if (path != null && File.Exists(path)) //continue
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
        File.Delete(path);
        ActivateBlocker();
        // 화면 덮기/열기 연출은 SceneLoader가 담당한다.
        GameStartContext.SetStartType(GameStartType.NewGame);
        SceneLoader.Instance?.LoadGardenScene();
    }

    public void OnClickContinueGame()
    {
        string json = File.ReadAllText(path);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        // 안전장치: 파일에 잘못 기록된 새 게임/게임오버 상태를 이어하기 상태로 보정
        GameStartType startType = saveData.gst;
        if (startType == GameStartType.NewGame || startType == GameStartType.GameOver || startType == GameStartType.None)
        {
            startType = GameStartType.ContinueGame;
        }
        GameStartContext.SetStartType(startType);

        ActivateBlocker();

        //GameStartContext.SetStartType(GameStartType.ContinueGame);
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
