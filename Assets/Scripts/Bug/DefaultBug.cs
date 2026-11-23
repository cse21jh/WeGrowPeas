using System.Collections;

public class DefaultBug : Bug
{
    protected override BugMove Move => BugMove.Chase;

    protected override IEnumerator HitBug()
    {
        yield return StartCoroutine(base.HitBug());
        yield return StartCoroutine(KillBug());
    }
}
