using UnityEngine;

public abstract class CurseInstance
{
    public CurseScriptable Data { get; }

    protected CurseInstance(CurseScriptable data)
    {
        Data = data;
    }

    public abstract void Activate();

    public abstract void Deactivate();
}
