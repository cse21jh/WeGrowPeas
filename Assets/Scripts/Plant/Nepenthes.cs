using UnityEngine;

public class Nepenthes : Plant
{
    protected override void OnMouseEnter()
    {
        return;
    }

    protected override void OnMouseExit()
    {
        return;
    }

    protected virtual float GetResistanceValue(WaveType wave)
    {
        return 1f;
    }

    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        switch (genetics)
        {
            case 0: return 0.5f;
            case 1: return 0.5f;
            case 2: return 0.8f;
        }
        return 0.0f;
    }

    public override void ContactBug(Bug bug)
    {
        StartCoroutine(bug.KillBug());
    }
}
