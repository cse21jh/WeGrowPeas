using System.Collections;

public class RandomMovingBug : Bug
{
    protected override BugMove Move => BugMove.Wander;

    protected override IEnumerator HitBug()
    {
        yield return StartCoroutine(base.HitBug());
        yield return StartCoroutine(KillBug());
    }
}
