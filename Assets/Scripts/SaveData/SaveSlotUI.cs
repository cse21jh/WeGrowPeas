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

    private string path;

    private void Start()
    {
        SetSlots();
    }

    public void SetSlots()
    {
        for(int i = 0; i < slotItems.Length; i++)
        {
            var tmp = slotItems[i].GetComponentInChildren<TextMeshProUGUI>();

            int slotIndex = i;

            string path = SaveContext.Instance.GetSavePath(slotIndex);

            if (!File.Exists(path)) tmp.text = $"저장소 {slotIndex} - 비어 있음";
            else
            {
                string json = File.ReadAllText(path);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                tmp.text = $"저장소 {slotIndex} - Day {saveData.stage}";
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

        if(path != null && File.Exists(path)) //continue
        {
            ShowSavePopup();
        }
        else //new game
        {
            OnClickNewGame();
        }
    }

    public void OnClickNewGame()
    {
        TransitionController.instance.Transition_Out();
        StartCoroutine(DelayAction(1.1f, () =>
        {
            GameStartContext.SetStartType(GameStartType.NewGame);
            SceneLoader.Instance?.LoadGardenScene();
        }));
    }

    public void OnClickContinueGame()
    {
        string json = File.ReadAllText(path);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        GameStartContext.SetStartType(saveData.gst);

        TransitionController.instance.Transition_Out();
        StartCoroutine(DelayAction(1.1f, () =>
        {
            //GameStartContext.SetStartType(GameStartType.ContinueGame);
            SceneLoader.Instance?.LoadGardenScene();
        }));
    }

    public void ShowSavePopup()
    {
        savePopup.SetActive(true);
    }

    public void CloseSavePopup()
    {
        savePopup.SetActive(false);
    }
}
