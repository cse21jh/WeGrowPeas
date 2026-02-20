using UnityEngine;

public class ShopMonopolyCurse : CurseInstance
{
    public ShopMonopolyCurse(CurseScriptable data) : base(data)
    {

    }

    public override void Activate()
    {
        Debug.Log("독점시장 실행");
    }

    public override void Deactivate()
    {
        Debug.Log("독점시장 끝");
    }
}
