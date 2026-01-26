using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField]
    private GameObject[] slotItems;

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

        string path = SaveContext.Instance.CurrentSaveFilePath;

        if(path != null && File.Exists(path)) //continue
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
        else //new game
        {
            TransitionController.instance.Transition_Out();
            StartCoroutine(DelayAction(1.1f, () =>
            {
                GameStartContext.SetStartType(GameStartType.NewGame);
                SceneLoader.Instance?.LoadGardenScene();
            }));
        }
    }
}
