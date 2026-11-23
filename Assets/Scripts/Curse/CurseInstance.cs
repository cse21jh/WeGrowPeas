using UnityEngine;

public abstract class CurseInstance
{
    public CurseScriptable Data { get; }

    /// <summary>이번 런의 저주 레벨(1~3). 새벽 저주 레벨에서 주입됨.</summary>
    protected int Level { get; }

    /// <summary>현재 레벨에 해당하는 수치. 데이터/레벨이 없으면 null.</summary>
    protected CurseLevel Lv => Data != null ? Data.GetLevel(Level) : null;

    /// <summary>이 저주가 걸린 판.</summary>
    protected CurseBoard Board { get; }
    protected ICurseWorld World => Board.World;

    protected CurseInstance(CurseScriptable data, int level, CurseBoard board)
    {
        Board = board;
        Data = data;
        Level = Mathf.Max(1, level);
    }

    public abstract void Activate();

    public abstract void Deactivate();
}
