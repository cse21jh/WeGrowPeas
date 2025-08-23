using UnityEngine;

public enum DialogueTriggerType { NarrationClick, KeyPress, ObjectClick }

[CreateAssetMenu(menuName = "Tutorial/Dialogue Step")]
public class DialogueStep : ScriptableObject
{
    [TextArea] public string text;

    public DialogueTriggerType triggerType = DialogueTriggerType.NarrationClick;

    // KeyPress 일 때만 사용
    //public KeyCode triggerKey = KeyCode.None;

    // ObjectClick 일 때만 사용 (이 오브젝트를 클릭해야 진행)
    public GameObject targetObject;

    // 이 스텝을 마치면 "튜토리얼 스킵?" 팝업을 띄울지
    public bool showSkipPopupOnComplete = false;
}
