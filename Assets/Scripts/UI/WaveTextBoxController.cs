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
        anim.SetBool("isShow", true);
    }


    public void HideWaveTextBox()
    {
        anim.SetBool("isShow", false);
    }



}
