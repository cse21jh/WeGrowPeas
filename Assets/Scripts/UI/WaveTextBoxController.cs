using UnityEngine;

public class WaveTextBoxController : MonoBehaviour
{
    Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void ShowWaveTextBox()
    {
        if (isActiveAndEnabled) anim.SetBool(AnimParams.IsShow, true);
    }


    public void HideWaveTextBox()
    {
        if (isActiveAndEnabled) anim.SetBool(AnimParams.IsShow, false);
    }



}
