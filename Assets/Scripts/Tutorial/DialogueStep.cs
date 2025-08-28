using UnityEngine;
using UnityEngine.UIElements;

public enum DialogueTriggerType { NarrationClick, KeyPress, ObjectClick }

public enum NextTutorialSequence { None, Step0, Step1, Step2, Step3, Step4, Step5 }

public enum TutorialActions
{
    None,
    ShowSkipPopUp,
    ShowWhiteCircle,
    FlushCircle,
    SpawnBug,
    Breed,
    EnemyWave,
    Upgrade,
    ShowTutorialEndPopUp,
}

[CreateAssetMenu(menuName = "Tutorial/Dialogue Step")]
public class DialogueStep : ScriptableObject
{
    [TextArea] public string text;

    public DialogueTriggerType triggerType = DialogueTriggerType.NarrationClick;

    // KeyPress 일 때만 사용
    //public KeyCode triggerKey = KeyCode.None;

    // ObjectClick 일 때만 사용 (이 오브젝트를 클릭해야 진행)
    public GameObject targetObject;

    public TutorialActions[] actions;

    public Vector3 whiteCirclePos = Vector3.zero;

    public NextTutorialSequence chainTo = NextTutorialSequence.None;
}
