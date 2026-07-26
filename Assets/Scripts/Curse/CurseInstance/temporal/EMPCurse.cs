using UnityEngine;

public class EMPCurse : CurseInstance
{
    public EMPCurse(CurseScriptable data, int lvIndex) : base(data, lvIndex)
    {
    }

    public override void Activate()
    {
        CurseState.EmpBlockRatio = (Lv != null ? Lv.valueA : 0f) / 100f;
        CurseEffectManager.Instance?.SetEMPCurse(true);
    }

    public override void Deactivate()
    {
        CurseState.EmpBlockRatio = 0f;
        CurseEffectManager.Instance?.SetEMPCurse(false);
    }
}
