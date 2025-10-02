using TMPro;
using UnityEngine;

public class SelectionBoxController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selectionBox1_Text;
    [SerializeField] private TextMeshProUGUI selectionBox2_Text;

    [SerializeField] private GameObject selectionBox1;
    [SerializeField] private GameObject selectionBox2;



    public void SetText(string sel1, string sel2)
    {
        selectionBox1_Text.text = sel1;
        selectionBox2_Text.text = sel2;

        if(sel2 == "")
        {
            selectionBox2.SetActive(false);
        }
        else
        {
            selectionBox2.SetActive(true);
        }
    }
}
