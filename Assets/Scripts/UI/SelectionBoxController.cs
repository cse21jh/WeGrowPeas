using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionBoxController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selectionBox1_Text;
    [SerializeField] private TextMeshProUGUI selectionBox2_Text;

    [SerializeField] private GameObject selectionBox1;
    [SerializeField] private GameObject selectionBox2;

    private Action onClickAction1;
    private Action onClickAction2;


    public void SetText(string sel1, string sel2 = "", Action act1 = null, Action act2 = null)
    {


        selectionBox1_Text.text = sel1;
        selectionBox2_Text.text = sel2;

        onClickAction1 = act1;
        onClickAction2 = act2;

        selectionBox1.GetComponent<Button>().onClick.AddListener(() =>
        {
            onClickAction1?.Invoke();
            SoundManager.Instance.PlayEffect("Button");
        });
        selectionBox2.GetComponent<Button>().onClick.AddListener(() =>
        {
            onClickAction2?.Invoke();
            SoundManager.Instance.PlayEffect("Button");
        });

        if (sel2 == "")
        {
            selectionBox2.SetActive(false);
        }
        else
        {
            selectionBox2.SetActive(true);
        }
    }

    public void DeactivateBtn()
    {
        selectionBox1.GetComponent<Button>().onClick.RemoveAllListeners();
        selectionBox2.GetComponent<Button>().onClick.RemoveAllListeners();


        selectionBox1.GetComponent<Button>().colors = ColorBlock.defaultColorBlock;
        selectionBox2.GetComponent<Button>().colors = ColorBlock.defaultColorBlock;
    }
}
