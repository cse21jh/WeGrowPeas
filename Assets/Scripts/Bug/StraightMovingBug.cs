using System.Collections;

public class StraightMovingBug : Bug
{
    protected override BugMove Move => BugMove.Straight;

    protected override IEnumerator HitBug()
    {
        yield return StartCoroutine(base.HitBug());
        yield return StartCoroutine(KillBug());
    }
}
