using UnityEngine;

public class WaveTextBoxController : MonoBehaviour
{
    Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void ShowWaveTextBox()
    {
        anim.SetTrigger("Show");
    }


    public void HideWaveTextBox()
    {
        anim.SetTrigger("Hide");
    }



}
