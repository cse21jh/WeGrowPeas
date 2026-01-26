using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
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
