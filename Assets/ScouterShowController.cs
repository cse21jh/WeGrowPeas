using System.Net.NetworkInformation;
using UnityEngine;

public class ScouterShowController : MonoBehaviour
{
    [SerializeField] private GameObject GoldScouter;
    [SerializeField] private GameObject ResistanceScouter;

    [SerializeField] private GameObject textBox_1;
    [SerializeField] private GameObject textBox_2;

    private void Awake()
    {
        SetScouter(false, false);
    }

    public void SetScouter(bool isGoldActive, bool isResistActive)
    {
        if(!isGoldActive && !isResistActive)
        {
            textBox_1.SetActive(false);
            textBox_2.SetActive(false);
            GoldScouter.SetActive(false);
            ResistanceScouter.SetActive(false);
            return;
        }

        textBox_1.SetActive(true);
        textBox_2.SetActive(false);
        transform.localPosition = new Vector3(-0.25f, transform.localPosition.y, transform.localPosition.z);

        GoldScouter.SetActive(isGoldActive);
        GoldScouter.transform.localPosition = new Vector3(-0.25f, 0f, 0f);

        ResistanceScouter.SetActive(isResistActive);
        ResistanceScouter.transform.localPosition = new Vector3(-0.25f, 0f, 0f);

        if (isGoldActive && isResistActive)
        {
            textBox_1.SetActive(false);
            textBox_2.SetActive(true);

            transform.localPosition = new Vector3(0f, transform.localPosition.y, transform.localPosition.z);
            GoldScouter.transform.localPosition = new Vector3(-0.4f, 0f, 0f);
            ResistanceScouter.transform.localPosition = new Vector3(0.4f, 0f, 0f);
        }
    }


}
